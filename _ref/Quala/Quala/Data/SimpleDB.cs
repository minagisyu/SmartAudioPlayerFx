using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Quala.Data
{
	/// <summary>
	/// SQLiteラッパー。
	/// プロパティとフィールドをカラムとして扱う。
	/// トランザクション処理はしないでください。
	/// </summary>
	public class SimpleDB : IDisposable
	{
		// thread... sqlite
		// dispose -> close
		// taskable, manual commit, xml import/export,
		#region ctor

		SQLiteConnection connection;
		BlockingCollection<Action> tasks;
		CancellationTokenSource taskCTS;
		Task taskRunner;

		public SimpleDB(string filepath)
		{
			taskCTS = new CancellationTokenSource();
			var ct = taskCTS.Token;
			tasks = new BlockingCollection<Action>();
			taskRunner = Task.Factory.StartNew(()=>
			{
				var csb = new SQLiteConnectionStringBuilder();
				csb.DataSource = filepath;
				connection = new SQLiteConnection(csb.ToString());
				connection.Open();
				__Execute("BEGIN");
				// DBファイルが消されないように開っぱなしにする
				using (var dbLock = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					while (taskCTS.IsCancellationRequested == false)
					{
						try
						{
							var action = tasks.Take(ct);
							if (action != null)
							{
								try { action(); }
								catch (SQLiteException e)
								{
									LogService.AddErrorLog("SimpleDB", "SQLiteException", e);
								}
							}
						}
						catch (OperationCanceledException) { }
					}
				}
			}, TaskCreationOptions.LongRunning);
		}

		#endregion
		#region IDisposable

		~SimpleDB() { Dispose(false); }
		public void Dispose() { Dispose(true); }

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (taskCTS != null)
				{
					taskCTS.Dispose();
					taskCTS = null;
				}
				if (taskRunner != null)
				{
					taskRunner.Wait();
					taskRunner.Dispose();
					taskRunner = null;
				}
				if (tasks != null)
				{
					tasks.Dispose();
					tasks = null;
				}
				if (connection != null)
				{
					connection.Dispose();
					connection = null;
				}
				GC.SuppressFinalize(this);
			}
		}

		#endregion

		public void Rollback()
		{
			AddTask(() => { __Execute("ROLLBACK"); __Execute("BEGIN"); });
		}
		public void Commit()
		{
			AddTask(() => { __Execute("COMMIT"); __Execute("BEGIN"); });
		}
		public void Vacuum()
		{
			AddTask(() => { __Execute("COMMIT; VACUUM; BEGIN;"); });
		}

		/// <summary>
		/// 無効なオブジェクトを削除してDBリソースをリサイクルする。
		/// </summary>
		/// <typeparam name="T">テーブルオブジェクト</typeparam>
		/// <param name="invalid_condition">無効なオブジェクトを計測するselect文のwhere条件</param>
		/// <param name="delete_condition">削除するオブジェクトのdelete文のwhere条件</param>
		/// <param name="delete_run_condition">削除を実行する条件(全体カウント, invalid_conditionに合致するカウント)</param>
		public void Recycle<T>(string invalid_condition, string delete_condition, Func<long, long, bool> delete_run_condition)
		{
			AddTask(() =>
			{
				var mapping = GetMapping(typeof(T));
				var all_items_count = (long)__ExecuteScaler("select count(*) from " + mapping.TableName);
				var invalid_items_count = (long)__ExecuteScaler("select count(*) from " + mapping.TableName + " where " + invalid_condition);
				if (delete_run_condition(all_items_count, invalid_items_count))
					__Execute("delete from " + mapping.TableName + " where " + delete_condition);
			});
		}

		/// <summary>
		/// テーブル、インデックスの作成、不足しているカラムの追加を行います。
		/// カラムの追加、インデックスの生成が行われる際にはコミット、バキュームが行われます。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public void CreateTable<T>()
		{
			AddTaskWithWait(()=>
			{
				bool vacuum_require = false;
				// テーブル作成
				var type = typeof(T);
				var map = GetMapping(type);
				var query = "create table if not exists [" + map.TableName + "](" +
					string.Join(",", map.Columns.Select(c => c.SqlDecl).ToArray()) + ")";
				__Execute(query, connection);
				// 不足しているカラムの作成
				var mapColumns = map.Columns.ToDictionary(c => c.Name, StringComparer.InvariantCultureIgnoreCase);
				__ExecuteReader("pragma table_info(" + map.TableName + ")")
					.Run(r=> mapColumns.Remove(r.GetString(1)));
				if (mapColumns.Any())
				{
					mapColumns.Run(kv => __Execute("alter table [" + map.TableName + "] add column " + kv.Value.SqlDecl));
					vacuum_require = true;
				}
				// インデックス作成 --- PrimaryKeyとUniqueには自動でインデックスが付くはず。
				var index_count = (long)__ExecuteScaler("select COUNT(*) from sqlite_master where type='index'");
				map.Columns
					.Where(c => c.IsIndexed && !c.IsPrimaryKey)
					.Run(c => __Execute("create index if not exists [" + map.TableName + "_" + c.Name + "] on [" + map.TableName + "]([" + c.Name + "])"));
				index_count -= (long)__ExecuteScaler("select COUNT(*) from sqlite_master where type='index'");
				if (index_count != 0)
					vacuum_require = true;
				// 最適化の必要アリ？
				if (vacuum_require)
					__Execute("COMMIT; VACUUM; BEGIN;");
			});
		}

		#region Query / Get

		public int ExecuteNonQuery(string query, params object[] args)
		{
			int ret = 0;
			AddTaskWithWait(() => ret = __Execute(query, args));
			return ret;
		}
		public object ExecuteScaler(string query, params object[] args)
		{
			object ret = null;
			AddTaskWithWait(() => ret = __ExecuteScaler(query, args));
			return ret;
		}
		public IEnumerable<T> Query<T>(string query, params object[] args)
			where T : new()
		{
			return Query<T>(GetMapping(typeof(T)), query, args);
		}
		IEnumerable<T> Query<T>(TableMapping map, string query, params object[] args)
			where T : new()
		{
			IEnumerable<T> result = null;
			AddTaskWithWait(() =>
			{
				TableMapping.Column[] targetColumns = null;
				result = __ExecuteReader(query, args)
					.Select(r =>
					{
						if (targetColumns == null)
						{
							var map_columns = map.Columns.ToDictionary(c => c.Name);
							targetColumns = Enumerable.Range(0, r.FieldCount)
								.Select(i =>
								{
									TableMapping.Column tmp;
									return map_columns.TryGetValue(r.GetName(i), out tmp) ? tmp : null;
								})
								.Where(i => i != null)
								.ToArray();
						}

						var ret = new T();
						Enumerable.Range(0, r.FieldCount)
							.Select(i => new { Index = i, Value = r.GetValue(i) })
							.Where(v => v.Value != DBNull.Value)
							.Run(v =>
							{
								var target = targetColumns[v.Index];
								if (target.ColumnType == typeof(bool))	// boolも数値で格納しちゃうので
									target.SetValue(ret, (long)v.Value != 0);
								else
									target.SetValue(ret, v.Value);
							});
						return ret;
					});
			});
			return result;
		}

		// 全ブロパティ; select * from T
		public IEnumerable<T> GetAll<T>() where T : new()
		{
			var map = GetMapping(typeof(T));
			var query = "select * from [" + map.TableName + "]";
			return Query<T>(map, query);
		}

		// 全ブロパティ; select * from T where PRIMARY_KEY = primaryKeyValue
		public T Get<T>(object primaryKeyValue) where T : new()
		{
			var map = GetMapping(typeof(T));
			if (map.PrimaryKey == null) throw new NotSupportedException();

			var query = "select * from [" + map.TableName + "] where [" + map.PrimaryKey.Name + "] = ? limit 1";
			return Query<T>(map, query, primaryKeyValue).FirstOrDefault();
		}
		// 全ブロパティ; select * from T where key = keyValue
		public T Get<T>(Expression<Func<T, object>> key, object keyValue) where T : new()
		{
			var keyname = GetExpressionMemberName(key);
			if (keyname == null)
				throw new ArgumentException("key");

			var map = GetMapping(typeof(T));
			var keyColumn = map.Columns
				.Where(c => c.Name == keyname)
				.FirstOrDefault();
			if (keyColumn == null)
				throw new NotSupportedException();

			var query = "select * from [" + map.TableName + "] where [" + keyColumn.Name + "] = ? limit 1";
			return Query<T>(map, query, keyValue).FirstOrDefault();
		}

		// 一部ブロパティ; select targetProperties from T where PRIMARY_KEY = primaryKeyValue
		public T Get<T>(object primaryKeyValue, params Expression<Func<T, object>>[] targetProperties) where T : new()
		{
			var map = GetMapping(typeof(T));
			if (map.PrimaryKey == null)
				throw new NotSupportedException();

			var columnNames = targetProperties
				.Select(c => GetExpressionMemberName(c))
				.Where(name => name != null)
				.ToList();
			var columns = map.Columns
				.Where(c => columnNames.Contains(c.Name))
				.Select(c => c.Name)
				.ToArray();

			var targetColumns = "(" + string.Join(",", columns) + ")";
			var query = "select " + targetColumns + " from [" + map.TableName + "] where [" + map.PrimaryKey.Name + "] = ? limit 1";
			return Query<T>(map, query, primaryKeyValue).FirstOrDefault();
		}
		// 一部ブロパティ; select targetProperties from T where key = keyValue
		public T Get<T>(Expression<Func<T, object>> key, object keyValue, params Expression<Func<T, object>>[] targetProperties) where T : new()
		{
			var keyname = GetExpressionMemberName(key);
			if (keyname == null)
				throw new ArgumentException("key");

			var map = GetMapping(typeof(T));
			var keyColumn = map.Columns
				.Where(c => c.Name == keyname)
				.FirstOrDefault();
			if (keyColumn == null)
				throw new NotSupportedException();

			var columnNames = targetProperties
				.Select(c => GetExpressionMemberName(c))
				.Where(c => c != null)
				.ToList();
			var columns = map.Columns
				.Where(c => columnNames.Contains(c.Name))
				.Select(c => c.Name)
				.ToArray();

			var targetColumns = "(" + string.Join(",", columns) + ")";
			var query = "select " + targetColumns + " from [" + map.TableName + "] where [" + keyColumn.Name + "] = ? limit 1";
			return Query<T>(map, query, keyValue).FirstOrDefault();
		}

		#endregion
		#region Insert

		// objを挿入(全プロパティ) --- INTEGER PRIMARY KEYなプロパティはIDが更新されます
		public IEnumerable<T> Insert<T>(params T[] objs)
		{
			return Insert<T>((IEnumerable<T>)objs);
		}
		public IEnumerable<T> Insert<T>(IEnumerable<T> objs)
		{
			return Insert_AllProperies<T>("insert into", objs);
		}
		public IEnumerable<T> InsertOrIgnore<T>(params T[] objs)
		{
			return InsertOrIgnore<T>((IEnumerable<T>)objs);
		}
		public IEnumerable<T> InsertOrIgnore<T>(IEnumerable<T> objs)
		{
			return Insert_AllProperies<T>("insert or ignore into", objs);
		}
		IEnumerable<T> Insert_AllProperies<T>(string command, IEnumerable<T> objs)
		{
			var map = GetMapping(typeof(T));
			var columns = map.Columns.Where(c => !c.IsAutoInclement).ToArray();
			return InsertInternal<T>(command, map, columns, objs);
		}

		// objを挿入(指定+NotNullのみ) --- INTEGER PRIMARY KEYなプロパティはIDが更新されます
		public IEnumerable<T> Insert<T>(T obj, params Expression<Func<T, object>>[] targetProperties)
		{
			return Insert<T>(new T[] { obj, }, targetProperties);
		}
		public IEnumerable<T> Insert<T>(IEnumerable<T> objs, params Expression<Func<T, object>>[] targetProperties)
		{
			return Insert_SpecifiedProperty<T>("insert into", objs, targetProperties);
		}
		public IEnumerable<T> InsertOrIgnore<T>(T obj, params Expression<Func<T, object>>[] targetProperties)
		{
			return InsertOrIgnore<T>(new T[] { obj, }, targetProperties);
		}
		public IEnumerable<T> InsertOrIgnore<T>(IEnumerable<T> objs, params Expression<Func<T, object>>[] targetProperties)
		{
			return (targetProperties == null) ?
				Insert_AllProperies<T>("insert or ignore", objs) :
				Insert_SpecifiedProperty<T>("insert or ignore into", objs, targetProperties);
		}
		IEnumerable<T> Insert_SpecifiedProperty<T>(string command, IEnumerable<T> objs, params Expression<Func<T, object>>[] targetProperties)
		{
			if (targetProperties == null || targetProperties.Length == 0)
				return Insert_AllProperies<T>(command, objs);

			var map = GetMapping(typeof(T));
			var targetNames = targetProperties
				.Select(ex => GetExpressionMemberName(ex))
				.Where(name => name != null)
				.ToList();
			var columns = map.Columns
				.Where(c => !c.IsAutoInclement)
				.Where(c => c.IsNotNull || targetNames.Contains(c.Name))
				.ToArray();
			return InsertInternal<T>(command, map, columns, objs);
		}
		IEnumerable<T> InsertInternal<T>(string command, TableMapping map, TableMapping.Column[] columns, IEnumerable<T> objs)
		{
			IEnumerable<T> result = null;
			AddTaskWithWait(() =>
			{
				var base_sql = command + " [" + map.TableName + "] (" +
					string.Join(",", columns.Select(c => "[" + c.Name + "]").ToArray()) + ") values(";

				string update_sql = null;
				var postProcess = (map.AutoInclement == null) ?
					new Action<T>(delegate { }) :
					new Action<T>(t =>
					{
						// AutoInclementなプロパティを更新
						if (update_sql == null)
							update_sql = "select " + map.AutoInclement.Name +
								" from [" + map.TableName + "] where _ROWID_ = last_insert_rowid()";
						var id = __ExecuteScaler(update_sql);
						if (id != null)
							map.AutoInclement.SetValue(t, Convert.ChangeType(id, map.AutoInclement.ColumnType));
					});

				result = objs.Select(obj =>
				{
					var query = base_sql + string.Join(",", columns.Select(c => "?").ToArray()) + ")";
					var values = columns.Select(c => c.GetValue(obj)).ToArray();
					if (__Execute(query, values) > 0)
					{
						postProcess(obj);
						return obj;
					}
					return default(T);
				})
				.Where(obj => obj != null)
				.ToArray();
			});
			return result;
		}

		#endregion
		#region Delete

		// objを削除 --- PRIMARY KEYで条件つけます
		public int Delete<T>(T obj)
		{
			var map = GetMapping(typeof(T));
			if (map.PrimaryKey == null)
				throw new NotSupportedException();
			var where_string = "[" + map.PrimaryKey.Name + "] = ?";
			return DeleteInternal<T>(map, obj, where_string, map.PrimaryKey.GetValue(obj));
		}
		// objを削除 --- 指定プロパティをキーとして条件つけます
		public int Delete<T>(T obj, Expression<Func<T, object>> key)
		{
			var keyname = GetExpressionMemberName(key);
			if (keyname == null)
				throw new ArgumentException("key");

			var map = GetMapping(typeof(T));
			var keyColumn = map.Columns
				.Where(c => keyname == c.Name)
				.FirstOrDefault();
			if (keyColumn == null)
				throw new NotSupportedException();
			var where_string = "[" + keyColumn.Name + "] = ?";
			return DeleteInternal<T>(map, obj, where_string, keyColumn.GetValue(obj));
		}
		// objを削除 --- where以降のSQL文を指定できます ('COLUMN_NAME' = ?)
		public int Delete<T>(T obj, string where_string, params object[] args)
		{
			var map = GetMapping(typeof(T));
			return DeleteInternal<T>(map, obj, where_string, args);
		}
		int DeleteInternal<T>(TableMapping map, T obj, string where_string, params object[] args)
		{
			int result = 0;
			AddTaskWithWait(() =>
			{
				var query = "delete from [" + map.TableName + "] where " + where_string;
				result = __Execute(query, args);
			});
			return result;
		}

		#endregion
		#region UpdateTo

		// objを更新(全プロパティ) --- PRIMARY KEYで条件つけます
		public int UpdateTo<T>(T obj)
		{
			var map = GetMapping(typeof(T));
			if (map.PrimaryKey == null)
				throw new NotSupportedException();
			var where_string = "[" + map.PrimaryKey.Name + "] = ?";
			return UpdateToInternal_AllProperties<T>("update", map, obj, where_string, map.PrimaryKey.GetValue(obj));
		}
		// objを更新(全プロパティ) --- 指定プロパティをキーとして条件つけます
		public int UpdateTo<T>(T obj, Expression<Func<T, object>> key)
		{
			var keyname = GetExpressionMemberName(key);
			if (keyname == null)
				throw new ArgumentException("key");

			var map = GetMapping(typeof(T));
			var keyColumn = map.Columns
				.Where(c => keyname == c.Name)
				.FirstOrDefault();
			if (keyColumn == null)
				throw new NotSupportedException();
			var where_string = "[" + keyColumn.Name + "] = ?";
			return UpdateToInternal_AllProperties<T>("update", map, obj, where_string, keyColumn.GetValue(obj));
		}
		// objを更新(全プロパティ) --- where以降のSQL文を指定できます ('COLUMN_NAME' = ?)
		public int UpdateTo<T>(T obj, string where_string, params object[] args)
		{
			var map = GetMapping(typeof(T));
			return UpdateToInternal_AllProperties<T>("update", map, obj, where_string, args);
		}
		// objを更新(全プロパティ) --- PRIMARY KEYで条件つけます
		public int UpdateOrIgnoreTo<T>(T obj)
		{
			var map = GetMapping(typeof(T));
			if (map.PrimaryKey == null)
				throw new NotSupportedException();
			var where_string = "[" + map.PrimaryKey.Name + "] = ?";
			return UpdateToInternal_AllProperties<T>("update or ignore", map, obj, where_string, map.PrimaryKey.GetValue(obj));
		}
		// objを更新(全プロパティ) --- 指定プロパティをキーとして条件つけます
		public int UpdateOrIgnoreTo<T>(T obj, Expression<Func<T, object>> key)
		{
			var keyname = GetExpressionMemberName(key);
			if (keyname == null)
				throw new ArgumentException("key");

			var map = GetMapping(typeof(T));
			var keyColumn = map.Columns
				.Where(c => keyname == c.Name)
				.FirstOrDefault();
			if (keyColumn == null)
				throw new NotSupportedException();
			var where_string = "[" + keyColumn.Name + "] = ?";
			return UpdateToInternal_AllProperties<T>("update or ignore", map, obj, where_string, keyColumn.GetValue(obj));
		}
		// objを更新(全プロパティ) --- where以降のSQL文を指定できます ('COLUMN_NAME' = ?)
		public int UpdateOrIgnoreTo<T>(T obj, string where_string, params object[] args)
		{
			var map = GetMapping(typeof(T));
			return UpdateToInternal_AllProperties<T>("update or ignore", map, obj, where_string, args);
		}
		int UpdateToInternal_AllProperties<T>(string command, TableMapping map, T obj, string where_string, params object[] args)
		{
			int result = 0;
			AddTaskWithWait(() =>
			{
				var columns = map.Columns.Where(c => !c.IsAutoInclement).ToArray();
				var values = columns.Select(c => c.GetValue(obj)).ToArray();
				var query = "update [" + map.TableName + "] set " +
					string.Join(",", columns.Select(c => "[" + c.Name + "]=?").ToArray()) +
					"where " + where_string;
				result = __Execute(query, Enumerable.Concat(values, args).ToArray());
			});
			return result;
		}

		// objを更新(一部プロパティ) --- PRIMARY KEYで条件つけます
		public int UpdateTo<T>(T obj, params Expression<Func<T, object>>[] targetProperties)
		{
			var map = GetMapping(typeof(T));
			if (map.PrimaryKey == null)
				throw new NotSupportedException();
			var where_string = "[" + map.PrimaryKey.Name + "] = ?";
			return UpdateToInternal_SpecifiedProperties<T>("update", map, obj, where_string, new[] { map.PrimaryKey.GetValue(obj) }, targetProperties);
		}
		// objを更新(一部プロパティ) --- 指定プロパティをキーとして条件つけます
		public int UpdateTo<T>(Expression<Func<T, object>> key, T obj, params Expression<Func<T, object>>[] targetProperties)
		{
			var keyname = GetExpressionMemberName(key);
			if (keyname == null)
				throw new ArgumentException("key");

			var map = GetMapping(typeof(T));
			var keyColumn = map.Columns
				.Where(c => keyname == c.Name)
				.FirstOrDefault();
			if (keyColumn == null)
				throw new NotSupportedException();
			var where_string = "[" + keyColumn.Name + "] = ?";
			return UpdateToInternal_SpecifiedProperties<T>("update", map, obj, where_string, new[] { keyColumn.GetValue(obj) }, targetProperties);
		}
		// objを更新(一部プロパティ) --- where以降のSQL文を指定できます ('COLUMN_NAME' = ?)
		public int UpdateTo<T>(T obj, string where_string, object[] args, params Expression<Func<T, object>>[] targetProperties)
		{
			var map = GetMapping(typeof(T));
			return UpdateToInternal_SpecifiedProperties<T>("update", map, obj, where_string, args, targetProperties);
		}
		// objを更新(一部プロパティ) --- PRIMARY KEYで条件つけます
		public int UpdateOrIgnoreTo<T>(T obj, params Expression<Func<T, object>>[] targetProperties)
		{
			var map = GetMapping(typeof(T));
			if (map.PrimaryKey == null)
				throw new NotSupportedException();
			var where_string = "[" + map.PrimaryKey.Name + "] = ?";
			return UpdateToInternal_SpecifiedProperties<T>("update or ignore", map, obj, where_string, new[] { map.PrimaryKey.GetValue(obj) }, targetProperties);
		}
		// objを更新(一部プロパティ) --- 指定プロパティをキーとして条件つけます
		public int UpdateOrIgnoreTo<T>(T obj, Expression<Func<T, object>> key, params Expression<Func<T, object>>[] targetProperties)
		{
			var keyname = GetExpressionMemberName(key);
			if (keyname == null)
				throw new ArgumentException("key");

			var map = GetMapping(typeof(T));
			var keyColumn = map.Columns
				.Where(c => keyname == c.Name)
				.FirstOrDefault();
			if (keyColumn == null)
				throw new NotSupportedException();
			var where_string = "[" + keyColumn.Name + "] = ?";
			return UpdateToInternal_SpecifiedProperties<T>("update or ignore", map, obj, where_string, new[] { keyColumn.GetValue(obj) }, targetProperties);
		}
		// objを更新(一部プロパティ) --- where以降のSQL文を指定できます ('COLUMN_NAME' = ?)
		public int UpdateOrIgnoreTo<T>(T obj, string where_string, object[] args, params Expression<Func<T, object>>[] targetProperties)
		{
			var map = GetMapping(typeof(T));
			return UpdateToInternal_SpecifiedProperties<T>("update or ignore", map, obj, where_string, args, targetProperties);
		}
		int UpdateToInternal_SpecifiedProperties<T>(string command, TableMapping map, T obj, string where_string, object[] args, params Expression<Func<T, object>>[] targetProperties)
		{
			int result = 0;
			AddTaskWithWait(() =>
			{
				var targetNames = targetProperties
					.Select(ex => GetExpressionMemberName(ex))
					.Where(name => name != null)
					.ToList();
				var columns = map.Columns
					.Where(c => !c.IsAutoInclement)
					.Where(c => targetNames.Contains(c.Name))
					.ToArray();
				var values = columns.Select(c => c.GetValue(obj)).ToArray();
				var query = command + " [" + map.TableName + "] set " +
					string.Join(",", columns.Select(c => "[" + c.Name + "]=?").ToArray()) +
					"where " + where_string;
				result = __Execute(query, Enumerable.Concat(values, args).ToArray());
			});
			return result;
		}

		#endregion

		#region 利便性メソッド

		// 特定タスクを実行キューに登録する
		void AddTask(Action action)
		{
			tasks.Add(action);
		}

		// 特定タスクを実行キューに登録し、実行され、処理が終わるまで待つ
		void AddTaskWithWait(Action action)
		{
			using (var ev = new ManualResetEventSlim(false))
			{
				tasks.Add(()=>
				{
					try { action(); }
					finally { ev.Set(); }
				});
				ev.Wait();
			}
		}

		// CreateCommand
		SQLiteCommand CreateCommand(string commandText, params object[] args)
		{
			var cmd = connection.CreateCommand();
			cmd.CommandText = commandText;
			cmd.CommandType = System.Data.CommandType.Text;
			cmd.Parameters.AddRange(args.Select(arg => new SQLiteParameter(null, arg)).ToArray());
			return cmd;
		}

		// Execute
		int __Execute(string commandText, params object[] args)
		{
			using (var cmd = CreateCommand(commandText, args))
			{
				return cmd.ExecuteNonQuery();
			}
		}

		// ExecuteScaler
		object __ExecuteScaler(string commandText, params object[] args)
		{
			using (var cmd = CreateCommand(commandText, args))
			{
				return cmd.ExecuteScalar();
			}
		}

		// ExecuteReader
		IEnumerable<SQLiteDataReader> __ExecuteReader(string commandText, params object[] args)
		{
			using (var cmd = CreateCommand(commandText, args))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					yield return reader;
				}
			}
		}

		#endregion
		#region マッピングキャッシュ / TableMapping

		static Dictionary<Type, TableMapping> _mappings;	// マッピングキャッシュ

		static TableMapping GetMapping(Type type)
		{
			if (_mappings == null)
				_mappings = new Dictionary<Type, TableMapping>();

			TableMapping map;
			if (!_mappings.TryGetValue(type, out map))
				_mappings.Add(type, map = new TableMapping(type));

			return map;
		}

		sealed class TableMapping
		{
			public readonly Type MappedType;
			public readonly string TableName;
			public readonly Column[] Columns;
			public readonly Column PrimaryKey;
			public readonly Column AutoInclement;

			public TableMapping(Type type)
			{
				MappedType = type;
				TableName = type
					.GetCustomAttributes(typeof(TableNameAttribute), false)
					.Select(a => ((TableNameAttribute)a).TableName)
					.FirstOrDefault()
					?? type.Name;
				Columns = type
					.GetFields()
					.Where(f => !f.IsInitOnly)		// readonlyを除外
					.Select(f => new Column(f))
					.Concat(type.GetProperties()	// ↓ specialとインデックスプロパティは除外
						.Where(p => !p.IsSpecialName && p.GetIndexParameters().Length == 0)
						.Select(p => new Column(p)))
					.Where(c => !c.IsIgnore)		// 無視設定されてたら除外
					.ToArray();
				PrimaryKey = Columns.Where(c => c.IsPrimaryKey).FirstOrDefault();
				AutoInclement = Columns.Where(c => c.IsAutoInclement).FirstOrDefault();
			}

			public sealed class Column
			{
				FieldInfo _fi;
				PropertyInfo _pi;
				public readonly string Name;
				public readonly Type ColumnType;
				public readonly string SqlTypeName;
				public readonly bool IsPrimaryKey;
				public readonly bool IsIndexed;
				public readonly bool IsNotNull;
				public readonly bool IsUnique;
				public readonly string Collate;
				public readonly bool IsAutoInclement;
				public readonly bool IsIgnore;
				public readonly string SqlDecl;	// SQL定義 --- 'Name' integer primary key not null

				public Column(FieldInfo fi) : this(fi.FieldType, fi) { _fi = fi; }
				public Column(PropertyInfo pi) : this(pi.PropertyType, pi) { _pi = pi; }
				Column(Type columnType, MemberInfo mi)
				{
					ColumnType = columnType;
					Name = mi.Name;
					SqlTypeName = mi
						.GetCustomAttributes(typeof(SqlTypeAttribute), true)
						.Select(t => ((SqlTypeAttribute)t).TypeName)
						.FirstOrDefault()
						?? GetDefaultSqlTypeName(ColumnType);
					Collate = mi
						.GetCustomAttributes(typeof(CollateAttribute), true)
						.Select(c => ((CollateAttribute)c).Collation)
						.FirstOrDefault();
					IsPrimaryKey = mi
						.GetCustomAttributes(typeof(PrimaryKeyAttribute), true)
						.Any();
					IsIndexed = mi
						.GetCustomAttributes(typeof(IndexedAttribute), true)
						.Any();
					IsNotNull = IsPrimaryKey || mi
						.GetCustomAttributes(typeof(NotNullAttribute), true)
						.Any();
					IsUnique = mi
						.GetCustomAttributes(typeof(UniqueAttribute), true)
						.Any();
					IsIgnore = mi
						.GetCustomAttributes(typeof(IgnoreAttribute), true)
						.Any();
					IsAutoInclement = IsPrimaryKey && SqlTypeName.Contains("integer");
					SqlDecl = string.Format("[{0}] {1} {2}{3}{4}{5}",
						Name, SqlTypeName,
						(IsPrimaryKey) ? "primary key " : string.Empty,
						(IsNotNull) ? "not null " : string.Empty,
						(IsUnique) ? "unique " : string.Empty,
						(Collate != null) ? "collate " + Collate + " " : string.Empty);
				}

				public object GetValue(object obj)
				{
					return (_fi != null) ? _fi.GetValue(obj) : _pi.GetValue(obj, null);
				}

				public void SetValue(object obj, object value)
				{
					if (_fi != null)
						_fi.SetValue(obj, value);
					else
						_pi.SetValue(obj, value, null);
				}

				string GetDefaultSqlTypeName(Type type)
				{
					// nullable対応
					type = Nullable.GetUnderlyingType(type) ?? type;
					// Bool/Byte/IntXX/Decimal -> integer
					// Single/Double -> real
					// String -> text
					// DateTime -> datetime
					// byte[] -> blob
					// *other -> ""
					if (type == typeof(Boolean) || type == typeof(Byte) || type == typeof(SByte) ||
						type == typeof(UInt16) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(UInt32) ||
						type == typeof(Int64) || type == typeof(Decimal))
					{
						return "integer";
					}
					else if (type == typeof(Single) || type == typeof(Double))
					{
						return "real";
					}
					else if (type == typeof(String))
					{
						return "text";
					}
					else if (type == typeof(DateTime))
					{
						return "datetime";
					}
					else if (type == typeof(byte[]))
					{
						return "blob";
					}

					return "";
				}
			}
		}

		#region Helper & Attributes

		static string GetExpressionMemberName<TObject, TProperty>(Expression<Func<TObject, TProperty>> expression)
		{
			// 通常ならMemberExpressionが出来る
			var exp = expression.Body as MemberExpression;
			// 値型からobjectにボクシングされるとUnaryExpressionになる(ex)TProperty=object/real=int)
			// TPropertyが正常に設定されていればMemberAccessExpressionになるはずだが
			if (exp == null && expression.Body is UnaryExpression)
				exp = ((UnaryExpression)expression.Body).Operand as MemberExpression;
			// メンバーアクセスじゃない？
			if (exp == null)
				throw new NotSupportedException();
			return exp.Member.Name;
		}

		// カラムはプライマリキー
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class PrimaryKeyAttribute : Attribute { }

		// カラムはインデックス化される
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class IndexedAttribute : Attribute { }

		// カラムはユニークされる
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class UniqueAttribute : Attribute { }

		// カラムをnullに出来ない
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class NotNullAttribute : Attribute { }

		// カラムをDB要素から除外する
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class IgnoreAttribute : Attribute { }

		// COLLATEの設定(BINARY(default), NOCASE, RTRIM)
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class CollateAttribute : Attribute
		{
			public readonly string Collation;
			public CollateAttribute(string collation) { this.Collation = collation; }
		}

		// カラムのSQL型を手動設定 --- デフォルトは Bool/Byte/IntXX/Decimal->integer,
		//   Single/Double->real, String->text, DateTime->datetime, byte[]->blob, 
		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
		public sealed class SqlTypeAttribute : Attribute
		{
			public readonly string TypeName;
			public SqlTypeAttribute(string type) { TypeName = type; }
		}

		// テーブルの名前を手動設定 --- デフォルトはクラス/構造体名
		[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
		public sealed class TableNameAttribute : Attribute
		{
			public readonly string TableName;
			public TableNameAttribute(string name) { TableName = name; }
		}

		#endregion
		#endregion
	}
}

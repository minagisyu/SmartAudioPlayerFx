using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Codeplex.Data;

namespace SmartAudioPlayer
{
	// media.db操作用
	// SQLiteの非同期は1Connection=1スレッドなのでタスクを使うとダメ.
	// 基本シングルにして非同期は外側に任せる
	[Standalone]
	public sealed class MediaDB : IDisposable
	{
		#region ctor / Dispose

		readonly FileStream _dbFileLock;
		readonly ThreadLocal<DbExecutor> _dbconn;

		public MediaDB(string db_filename)
		{
			// ファイルが削除出来ないように開きっぱなしにする
			_dbFileLock = File.Open(db_filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			_dbconn = new ThreadLocal<DbExecutor>(() => Open(), true);
			CreateTableAndWriteMetadata();
		}
		public void Dispose()
		{
			_dbFileLock.Dispose();
			lock (_dbconn)
			{
				foreach (var x in _dbconn.Values)
				{
					x.Dispose();
				}
				_dbconn.Dispose();
			}
		}

		void CreateTableAndWriteMetadata()
		{
			CreateTable(_dbconn.Value);
			WriteMetadata(_dbconn.Value);
		}
		void CreateTable(DbExecutor executor)
		{
			executor.ExecuteNonQuery(
				"create table if not exists media (" +
				"ID integer primary key not null," +
				"FilePath text unique collate nocase," +
				"Title text, Artist text, Album text, Comment text, SearchHint text," +
				"CreatedDate integer, LastWrite integer, LastUpdate integer, LastPlay integer," +
				"PlayCount integer, SelectCount integer, SkipCount integer, IsFavorite integer, IsNotExist integer)");
			executor.ExecuteNonQuery(@"create index if not exists media_FilePath on media (FilePath)");
			executor.ExecuteNonQuery(@"create table if not exists meta (Key text primary key, Value text)");
		}
		void WriteMetadata(DbExecutor executor)
		{
			// Memo:
			// SAPFx 3.2.0.1以前の実装ミス(新しいバージョンで例外)によりチェックは無意味なので廃止
			// 互換性のためにメタ情報の書き込みだけやる

			// リフレクション経由で呼ばれるとGetEntryAssembly()==nullの場合があるので他も試す
			var asmName = (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly()).GetName();

			var insert_sql = @"insert or ignore into meta(Key,Value) values(@Key,@Value)";
			executor.ExecuteNonQuery(insert_sql, new { Key = "Version", Value = "3" });	// CURRENT_DB_VERSION
			executor.ExecuteNonQuery(insert_sql, new { Key = "CreatedDate", Value = DateTime.Now.ToString() });
			executor.ExecuteNonQuery(insert_sql, new { Key = "CreatedBy", Value = (asmName.Name + ", Version=" + asmName.Version) });
		}

		#endregion

		DbExecutor Open()
		{
			var builder = new SQLiteConnectionStringBuilder() { DataSource = _dbFileLock.Name, };
			return new DbExecutor(new SQLiteConnection(builder.ConnectionString));
		}
		public DBWriteAction BeginTransaction()
		{
			return new DBWriteAction(Open());
		}

		/// <summary>
		/// 指定ファイルパスのデータを取得します。
		/// DBにアイテムが無いときはnullが返ります
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public MediaItem Get(string filepath)
		{
			if (string.IsNullOrWhiteSpace(filepath)) throw new ArgumentException("filepath");
			if (!Path.IsPathRooted(filepath)) throw new ArgumentException("filepath is not rooted", "filepath");

			return _dbconn.Value.ExecuteReaderDynamic("select * from media where FilePath=@filepath",
				new { filepath })
				.Select(x => CreateMediaItemFromDynamic(x))
				.FirstOrDefault();
		}
		MediaItem CreateMediaItemFromDynamic(dynamic obj)
		{
			return new MediaItem()
			{
				ID = obj.ID,
				FilePath = obj.FilePath,

				Title = obj.Title,
				Artist = obj.Artist,
				Album = obj.Album,
				Comment = obj.Comment,
				SearchHint = obj.SearchHint,

				CreatedDate = obj.CreatedDate,
				LastWrite = obj.LastWrite,
				LastUpdate = obj.LastUpdate,
				LastPlay = obj.LastPlay,
				PlayCount = obj.PlayCount,
				SelectCount = obj.SelectCount,
				SkipCount = obj.SkipCount,

				IsFavorite = (obj.IsFavorite == 1),
				IsNotExist = (obj.IsNotExist == 1),
			};
		}

		/// <summary>
		/// ファイルパス前方一致で読み出します。
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public long GetCountFromFilePath(string path)
		{
			return _dbconn.Value.ExecuteScalar<long>(
				"select count(id) from media where FilePath like @path",
				new { path = path + "%" });
		}

		/// <summary>
		/// ファイルパス前方一致で読み出します。
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public IEnumerable<MediaItem> GetFromFilePath(string path)
		{
			var select_sql = "select * from media where FilePath like @path";
			foreach (var x in _dbconn.Value
				.ExecuteReaderDynamic(select_sql, new { path = path + "%" })
				.Select(x => CreateMediaItemFromDynamic(x)))
			{
				yield return x;
			}
		}

		/// <summary>
		/// ファイルパス前方一致+IsNotExist=0バージョン。
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public IEnumerable<MediaItem> GetFromFilePath_ExistsOnly(string path)
		{
			var select_sql = "select * from media where IsNotExist=0 AND FilePath like @path";
			foreach (var x in _dbconn.Value
				.ExecuteReaderDynamic(select_sql, new { path = path + "%" })
				.Select(x => CreateMediaItemFromDynamic(x)))
			{
				yield return x;
			}
		}

		/// <summary>
		/// ファイルパス前方一致+範囲指定可能バージョン。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		private IEnumerable<MediaItem> GetFromFilePath_Ranged(string path, int index, int count)
		{
			var select_sql = "select * from media where FilePath like @path limit @count offset @index";
			foreach (var x in _dbconn.Value
				.ExecuteReaderDynamic(select_sql, new { path = path + "%", count = count, index = index, })
				.Select(x => CreateMediaItemFromDynamic(x)))
			{
				yield return x;
			}
		}

		/// <summary>
		/// ファイルパス前方一致+範囲指定可能+IsNotExist=0バージョン。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public IEnumerable<MediaItem> GetFromFilePath_ExistsOnly_Ranged(string path, int index, int count = -1)
		{
			var select_sql =
				"select * from media where IsNotExist=0 AND FilePath like @path limit @count offset @index";
			foreach (var x in _dbconn.Value
				.ExecuteReaderDynamic(select_sql, new { path = path + "%", count = count, index = index, })
				.Select(x => CreateMediaItemFromDynamic(x)))
			{
				yield return x;
			}
		}

		/// <summary>
		/// 指定アイテムより前に再生されたアイテムを取得します。
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public string PreviousPlayItem(MediaItem item)
		{
			var select_sql =
				"select FilePath from media " +
				"where FilePath != @FilePath " +
				"and LastPlay < @LastPlay " +
				"and LastPlay > 0 " +
				"order by LastPlay desc limit 1";
			return _dbconn.Value.ExecuteScalar<string>(select_sql, new { item.FilePath, item.LastPlay, });
		}

		/// <summary>
		/// 最近再生された(MediaItem.LastPlayが新しい順)項目のファイルパスを指定数まで取得します。
		/// 未再生(MediaItem.LastPlay == 0)は対象から外れます。
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public IEnumerable<string> RecentPlayItemsPath(int limit)
		{
			var select_sql =
				"select FilePath from media where LastPlay > 0 order by LastPlay desc limit @limit";
			foreach (var x in _dbconn.Value.ExecuteReaderDynamic(select_sql, new { limit }))
			{
				yield return x.FilePath;
			}
		}

		/// <summary>
		/// DBリサイクル処理。
		/// fullCollect=true時はIsNotExist=1が対象。
		/// fullCollect=false時はIsNotExist=1 かつ LastUpdate が2日以上前の対象が全体の30%以上なら実行。
		/// fullCollect=true時vacuum処理するのでトランザクションを無効にしてください。
		/// </summary>
		/// <param name="fullCollect">最適化してDBファイルサイズを縮小する場合はtrue</param>
		public void Recycle(bool fullCollect)
		{
			// MEMO: Vacuumする可能性がある都合でトランザクションは使えない
			if (fullCollect)
			{
				_dbconn.Value.ExecuteNonQuery("delete from media where IsNotExist=1");
				_dbconn.Value.ExecuteNonQuery("vacuum");
			}
			else
			{
				var all_items_count = _dbconn.Value.ExecuteScalar<long>("select count(id) from media");
				var invalid_items_count = _dbconn.Value.ExecuteScalar<long>("select count(id) from media where IsNotExist=1");
				// しきい値を超えたらリサイクル処理
				if (((all_items_count / 100) * 30) < invalid_items_count)
				{
					var date = DateTime.UtcNow.AddDays(-2).Ticks;
					_dbconn.Value.ExecuteNonQuery(
						"delete from media where IsNotExist=1 and LastUpdate < @date",
						new { date = date });
				}
			}
		}

		/// <summary>
		/// MediaDBへの操作
		/// </summary>
		public class DBWriteAction : IDisposable
		{
			readonly DbExecutor _executor;

			internal DBWriteAction(DbExecutor executor)
			{
				this._executor = executor;

				while (true)
				{
					try
					{
						// MEMO: BEGIN TRANSACTIONと同じだけど、明示的にDEFERREDとしておく。
						_executor.ExecuteNonQuery("begin deferred;");
						break;
					}
					catch (SQLiteException) { Thread.Sleep(100); }
				}
			}
			public void Dispose()
			{
				_executor.Dispose();
			}
			public void Commit(bool restartTransaction = false)
			{
				while (true)
				{
					try
					{
						// MEMO: DbExecutorのIsorationLevelを指定する方法では例外が出るので手動で。
						_executor.ExecuteNonQuery("commit;");
						break;
					}
					catch (SQLiteException e)
					{
						Logger.AddDebugLog("DBWriter Commit Exception: {0}", e);
						Thread.Sleep(100);
					}
				}
				if (restartTransaction)
				{
					while (true)
					{
						try
						{
							// MEMO: BEGIN TRANSACTIONと同じだけど、明示的にDEFERREDとしておく。
							_executor.ExecuteNonQuery("begin deferred;");
							break;
						}
						catch (SQLiteException) { Thread.Sleep(100); }
					}
				}
			}

			/// <summary>
			/// 指定アイテムをDBに追加します。
			/// 同じファイルパスのアイテムがあった場合は無視され、追加されたファイルだけが返ります。
			/// 追加後はIDが書き込まれます。
			/// </summary>
			/// <param name="items"></param>
			/// <returns></returns>
			public IEnumerable<MediaItem> Insert(IEnumerable<MediaItem> items)
			{
				// IDは自動で割り振られるので指定しない
				var insert_sql = "insert or ignore into media(" +
					"FilePath,Title,Artist,Album,Comment," +
					"SearchHint,CreatedDate,LastWrite,LastUpdate,LastPlay," +
					"PlayCount,SelectCount,SkipCount,IsFavorite,IsNotExist) " +
					"values(" +
					"@FilePath,@Title,@Artist,@Album,@Comment," +
					"@SearchHint,@CreatedDate,@LastWrite,@LastUpdate,@LastPlay," +
					"@PlayCount,@SelectCount,@SkipCount,@IsFavorite,@IsNotExist)";
				var rowid_sql =
					"select ID from media where _ROWID_=last_insert_rowid()";
				foreach (var x in items)
				{
					var ret = _executor.ExecuteNonQuery(insert_sql, x);
					if (ret > 0)
					{
						// 新しく割り当てられたIDを取得しオブジェクトにフィードバック
						var new_id = _executor.ExecuteScalar<long>(rowid_sql);
						x.ID = new_id;
						yield return x;
					}
				}
			}

			/// <summary>
			/// 指定アイテムのIDを使用して、指定カラムのデータを更新します。
			/// 更新するカラムを指定することもできます。
			/// ID == 0のアイテムは更新できません。
			/// </summary>
			/// <param name="item"></param>
			/// <param name="columns"></param>
			public IEnumerable<MediaItem> Update(IEnumerable<MediaItem> items, params Expression<Func<MediaItem, object>>[] columns)
			{
				if (columns == null) throw new ArgumentNullException("columns");
				if (items.Any(i => i.ID == 0)) throw new ArgumentException("item.ID == 0", "item");

				var targetNames = columns
					.Select(ex => GetExpressionMemberName(ex))
					.Where(name => name != null)
					.ToArray();
				var columns_sql = string.Join(",",
					targetNames.Select(x => x + "=@" + x).ToArray());
				var update_sql =
					"update media set " + columns_sql + " where ID=@ID";

				foreach (var x in items)
				{
					x.LastUpdate = DateTime.UtcNow.Ticks;
					var ret = _executor.ExecuteNonQuery(update_sql, x);
					if (ret > 0)
					{
						yield return x;
					}
				}
			}

			// Expression<Func<MediaItem, object>> hoge = _=>_.MemberName な感じに指定された時、"MemberName"を返す.
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
		}

	}
}

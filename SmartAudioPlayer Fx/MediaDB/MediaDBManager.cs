using Dapper;
using Quala;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;

namespace SmartAudioPlayerFx.MediaDB
{
	// media.db操作用
	sealed class MediaDBManager : IDisposable
	{
		#region ctor / Dispose

		CompositeDisposable _disposables = new CompositeDisposable();
		SQLiteConnectionStringBuilder connStrBuilder;

		public MediaDBManager()
		{
			var db_filename = App.Models.Get<Storage>().AppDataRoaming.CreateFilePath("data", "media.db");

			// ファイルが削除出来ないように開きっぱなしにする
			Directory.CreateDirectory(Path.GetDirectoryName(db_filename));
			File.Open(db_filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)
				.AddTo(_disposables);
			connStrBuilder = new SQLiteConnectionStringBuilder()
			{
				DataSource = db_filename,
				Pooling = true,
			};
			CreateTableAndCheckDBVersion();
		}
		public void Dispose()
		{
			_disposables.Dispose();
		}

		#endregion

		SQLiteConnection Open()
		{
			if (_disposables.IsDisposed)
				throw new ObjectDisposedException(nameof(MediaDBManager));
			return new SQLiteConnection(connStrBuilder.ConnectionString)
				.OpenAndReturn();
		}
		public void UseTransaction(Action<DBWriteAction> action)
		{
			using (var conn = Open())
			{
				var dbaction = new DBWriteAction(conn);
				action(dbaction);
				dbaction.Commit();
			}
		}

		void CreateTableAndCheckDBVersion()
		{
			using (var conn = Open())
			{
				// create table
				conn.Execute(
					"create table if not exists media (" +
					"ID integer primary key not null," +
					"FilePath text unique collate nocase," +
					"Title text, Artist text, Album text, Comment text, SearchHint text," +
					"CreatedDate integer, LastWrite integer, LastUpdate integer, LastPlay integer," +
					"PlayCount integer, SelectCount integer, SkipCount integer, IsFavorite integer, IsNotExist integer)");
				conn.Execute(@"create index if not exists media_FilePath on media (FilePath)");
				conn.Execute(@"create table if not exists meta (Key text primary key, Value text)");

				// check db version
				const int CURRENT_DB_VERSION = 3;

				// Memo: SAPFx 3.2.0.1以前の実装ミス(新しいバージョンで例外)により
				//       バージョンあげると動かなくなる(例外発生)のでチェックはほぼ意味を成さない...
				var meta = conn.Query("select * from meta")
					.Select(x => new { Key = x.Key, Value = x.Value, })
					.ToDictionary(i => i.Key, i => i.Value);
				if (meta.Any())
				{
					// バージョン番号修正
					dynamic db_version;
					if (!meta.TryGetValue("Version", out db_version) || // Versionが無い
						string.IsNullOrWhiteSpace(db_version) ||        // 取得したら空文字だった
						!char.IsDigit(db_version, 0))                   // 数字以外の文字だった
						meta["Version"] = CURRENT_DB_VERSION.ToString();
				}
				else
				{
					// メタ情報書き込み
					// リフレクション経由で呼ばれるとGetEntryAssembly()==nullの場合があるので...
					var asmName = (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly() ?? Assembly.GetExecutingAssembly()).GetName();
					meta["Version"] = CURRENT_DB_VERSION.ToString();
					meta["CreatedDate"] = DateTime.Now.ToString();
					meta["CreatedBy"] = asmName.Name + ", Version=" + asmName.Version;
					meta
						.Select(v => new { Key = v.Key, Value = v.Value })
						.ToList()
						.ForEach(x => conn.Execute("insert into meta values(@Key, @Value);", x));
				}
				//
				int version = int.Parse(meta["Version"]);
				if (version < CURRENT_DB_VERSION)
				{
					// 古いバージョンなのでxmlエクスポートして修正とか？
					App.Models.Get<LogManager>().AddErrorLog($" - media db version {meta["Version"]} not supported.");
					throw new ApplicationException("db version not supported.");
				}
				else if (version > CURRENT_DB_VERSION)
				{
					// DBバージョンが新しい？
					App.Models.Get<LogManager>().AddErrorLog($" - media db version {meta["Version"]} futured version?");
					throw new ApplicationException("db version futured?");
				}
			}
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
			if (Path.IsPathRooted(filepath) == false) throw new ArgumentException("filepath is not rooted", "filepath");

			using (var conn = Open())
			{
				return conn.Query("select * from media where FilePath=@filepath",
					new { filepath })
					.Select(x => CreateMediaItemFromDynamic(x))
					.FirstOrDefault();
			}
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
			using (var conn = Open())
			{
				return conn.ExecuteScalar<long>(
					"select count(id) from media where FilePath like @path",
					new { path = path + "%" });
			}
		}

		/// <summary>
		/// ファイルパス前方一致で読み出します。
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public IEnumerable<MediaItem> GetFromFilePath(string path)
		{
			using (var conn = Open())
			{
				var select_sql = "select * from media where FilePath like @path";
				foreach (var x in conn
					.Query(select_sql, new { path = path + "%" })
					.Select(x => CreateMediaItemFromDynamic(x)))
				{
					yield return x;
				}
			}
		}

		/// <summary>
		/// ファイルパス前方一致+IsNotExist=0バージョン。
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public IEnumerable<MediaItem> GetFromFilePath_ExistsOnly(string path)
		{
			using (var conn = Open())
			{
				var select_sql = "select * from media where IsNotExist=0 AND FilePath like @path";
				foreach (var x in conn
					.Query(select_sql, new { path = path + "%" })
					.Select(x => CreateMediaItemFromDynamic(x)))
				{
					yield return x;
				}
			}
		}

		/// <summary>
		/// ファイルパス前方一致+範囲指定可能バージョン。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public IEnumerable<MediaItem> GetFromFilePath_Ranged(string path, int index, int count)
		{
			using (var conn = Open())
			{
				var select_sql = "select * from media where FilePath like @path limit @count offset @index";
				foreach (var x in conn
					.Query(select_sql, new { path = path + "%", count = count, index = index, })
					.Select(x => CreateMediaItemFromDynamic(x)))
				{
					yield return x;
				}
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
			using (var conn = Open())
			{
				var select_sql =
					"select * from media where IsNotExist=0 AND FilePath like @path limit @count offset @index";
				foreach (var x in conn
					.Query(select_sql, new { path = path + "%", count = count, index = index, })
					.Select(x => CreateMediaItemFromDynamic(x)))
				{
					yield return x;
				}
			}
		}

		/// <summary>
		/// 指定アイテムより前に再生されたアイテムを取得します。
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public string PreviousPlayItem(MediaItem item)
		{
			using (var conn = Open())
			{
				var select_sql =
					"select FilePath from media " +
					"where FilePath != @FilePath " +
					"and LastPlay < @LastPlay " +
					"and LastPlay > 0 " +
					"order by LastPlay desc limit 1";
				return conn.ExecuteScalar<string>(select_sql, new { item.FilePath, item.LastPlay, });
			}
		}

		/// <summary>
		/// 最近再生された(MediaItem.LastPlayが新しい順)項目のファイルパスを指定数まで取得します。
		/// 未再生(MediaItem.LastPlay == 0)は対象から外れます。
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public IEnumerable<string> RecentPlayItemsPath(int limit)
		{
			using (var conn = Open())
			{
				var select_sql =
					"select FilePath from media where LastPlay > 0 order by LastPlay desc limit @limit";
				foreach (var x in conn.Query(select_sql, new { limit }))
				{
					yield return x.FilePath;
				}
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
			using (var conn = Open())
			{
				if (fullCollect)
				{
					conn.Execute("delete from media where IsNotExist=1");
					conn.Execute("vacuum");
				}
				else
				{
					var all_items_count = conn.ExecuteScalar<long>("select count(id) from media");
					var invalid_items_count = conn.ExecuteScalar<long>("select count(id) from media where IsNotExist=1");
					// しきい値を超えたらリサイクル処理
					if (((all_items_count / 100) * 30) < invalid_items_count)
					{
						var date = DateTime.UtcNow.AddDays(-2).Ticks;
						conn.Execute(
							"delete from media where IsNotExist=1 and LastUpdate < @date",
							new { date = date });
					}
				}
			}
		}

		/// <summary>
		/// MediaDBへの操作
		/// </summary>
		public class DBWriteAction
		{
			readonly SQLiteConnection conn;

			internal DBWriteAction(SQLiteConnection conn)
			{
				this.conn = conn;
				while (true)
				{
					try
					{
						// MEMO: BEGIN TRANSACTIONと同じだけど、明示的にDEFERREDとしておく。
						conn.Execute("begin deferred;");
						break;
					}
					catch (SQLiteException) { Thread.Sleep(100); }
				}
			}
			internal void Commit()
			{
				while (true)
				{
					try
					{
						// MEMO: DbExecutorのIsorationLevelを指定する方法では例外が出るので手動で。
						var result = conn.Execute("commit;");
						break;
					}
					catch (SQLiteException e)
					{
						App.Models.Get<LogManager>().AddDebugLog($"DBWriter Commit Exception: {e}");
						Thread.Sleep(100);
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
					var ret = conn.Execute(insert_sql, x);
					if (ret > 0)
					{
						// 新しく割り当てられたIDを取得しオブジェクトにフィードバック
						var new_id = conn.ExecuteScalar<long>(rowid_sql);
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
					var ret = conn.Execute(update_sql, x);
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

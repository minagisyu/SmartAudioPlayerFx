using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Quala;
using System.IO;

namespace SmartAudioPlayerFx.Data
{
	/// <summary>
	/// データベース(media.db)を操作します
	/// SimpleDBラッパー
	/// </summary>
	sealed class MediaDB : IDisposable
	{
		const int CURRENT_DB_VERSION = 3;
		Lazy<SimpleDB> mediaDB;

		public MediaDB(string db_filename)
		{
			// DB初期化はコストがかかるので遅延処理
			mediaDB = new Lazy<SimpleDB>(() =>
			{
				LogService.AddDebugLog("MediaDB(SimpleDB) initializing...");
				var db = new SimpleDB(db_filename);

				// テーブル作成、メタデータチェック、コミット
				db.CreateTable<MediaItem>();
				db.CreateTable<MetaData>();
				CheckDBVersion(db);
				db.Commit();
				return db;
			}, true);
		}

		static void CheckDBVersion(SimpleDB db)
		{
			// Memo: SAPFx 3.2.0.1以前の実装ミス(新しいバージョンで例外)により
			//       バージョンあげると動かなくなる(例外発生)のでチェックはほぼ意味を成さない...
			var meta = db
				.GetAll<MetaData>()
				.ToDictionary(i => i.Key, i => i.Value);
			if (meta.Any())
			{
				// バージョン番号修正
				string db_version;
				if (!meta.TryGetValue("Version", out db_version) ||	// Versionが無い
					string.IsNullOrWhiteSpace(db_version) ||		// 取得したら空文字だった
					!char.IsDigit(db_version, 0))					// 数字以外の文字だった
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
				var entries = meta.Select(v => new MetaData() { Key = v.Key, Value = v.Value }).ToArray();
				db.Insert(entries);
			}
			//
			int version = int.Parse(meta["Version"]);
			if (version < CURRENT_DB_VERSION)
			{
				// 古いバージョンなのでxmlエクスポートして修正とか？
				LogService.AddErrorLog(" - media db version " + meta["Version"] + ", not supported.");
				throw new ApplicationException("db version not supported.");
			}
			else if (version > CURRENT_DB_VERSION)
			{
				// DBバージョンが新しい？
				LogService.AddErrorLog(" - media db version " + meta["Version"] + ", futured version?");
				throw new ApplicationException("db version futured?");
			}
		}

		/// <summary>
		/// 未保存のデータを保存します
		/// </summary>
		public void SaveChanges()
		{
			LogService.AddDebugLog("Call SaveChanges");
			mediaDB.Value.Commit();
		}

		#region IDisposable

		~MediaDB() { Dispose(false); }
		public void Dispose() { Dispose(true); }
		void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (mediaDB.IsValueCreated)
					mediaDB.Value.Dispose();
				GC.SuppressFinalize(this);
			}
		}

		#endregion

		/// <summary>
		/// 指定アイテムをDBに追加します。
		/// 同じファイルパスのアイテムがあった場合は無視され、追加されたファイルだけが返ります。
		/// 追加後はIDが書き込まれます。
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public MediaItem[] Insert(IEnumerable<MediaItem> items)
		{
		//	LogService.AddDebugLog("Call Insert");
			return mediaDB.Value.InsertOrIgnore(items);
		}

		/// <summary>
		/// 指定アイテムのIDを使用して、指定カラムのデータを更新します。
		/// 更新するカラムを指定することもできます。
		/// ID == 0のアイテムは更新できません。
		/// </summary>
		/// <param name="item"></param>
		/// <param name="columns"></param>
		public MediaItem[] Update(IEnumerable<MediaItem> items, params Expression<Func<MediaItem, object>>[] columns)
		{
			//	LogService.AddDebugLog("Call Update: item.ID={0}, item.FilePath={1}", item.ID, item.FilePath);
			if (columns == null) throw new ArgumentNullException("columns");
			if (items.Any(i => i.ID == 0)) throw new ArgumentException("item.ID == 0", "item");

			// update
			return items
				.Do(i => i.LastUpdate = DateTime.UtcNow.Ticks)
				.Where(i => mediaDB.Value.UpdateTo(_ => _.ID, i, columns) > 0)
				.ToArray();
		}

		/// <summary>
		/// 最近再生された(MediaItem.LastPlayが新しい順)項目のファイルパスを指定数まで取得します。
		/// 未再生(MediaItem.LastPlay == 0)は対象から外れます。
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public string[] RecentPlayItemsPath(int limit)
		{
			LogService.AddDebugLog("Call RecentPlayItems: limit={0}", limit);
			return mediaDB.Value
				.Query<MediaItem>("select FilePath from media where LastPlay > 0 order by LastPlay desc limit ?", limit)
				.Select(i => i.FilePath)
				.ToArray();
		}

		/// <summary>
		/// 指定アイテムより前に再生されたアイテムを取得します。
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public string PreviousPlayItem(MediaItem item)
		{
			LogService.AddDebugLog("Call PreviousPlayItem: item.FilePath={0}", item.FilePath);
			return mediaDB.Value
				.Query<MediaItem>("select FilePath from media where FilePath != ? and LastPlay < ? and LastPlay > 0 order by LastPlay desc limit 1", item.FilePath, item.LastPlay)
				.Select(i => i.FilePath)
				.FirstOrDefault();
		}

		/// <summary>
		/// 指定ファイルパスのデータを取得します。
		/// DBにアイテムが無いときはnullが返ります
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public MediaItem Get(string filepath)
		{
			LogService.AddDebugLog("Call Get: filepath={0}", filepath);
			if (string.IsNullOrWhiteSpace(filepath)) throw new ArgumentException("filepath");
			if (Path.IsPathRooted(filepath) == false) throw new ArgumentException("filepath is not rooted", "filepath");
			return mediaDB.Value.Get<MediaItem>(_ => _.FilePath, filepath);
		}

		/// <summary>
		/// GetItems()の範囲指定可能バージョン。
		/// ID順でソートされてから読みだします。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public MediaItem[] GetFromFilePath_Ranged(string path, int index, int count)
		{
		//	LogService.AddDebugLog("Call GetFromFilePath_Ranged: path={0}, index={1}, count={2}", path, index, count);
			return mediaDB.Value.Query<MediaItem>("select * from media where FilePath like ? order by ID limit " + count + " offset " + index, path + "%");
		}

		/// <summary>
		/// GetItems()の範囲指定可能+IsNotExist=0バージョン。
		/// ID順でソートされてから読みだします。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public MediaItem[] GetFromFilePath_ExistsOnly_Ranged(string path, int index, int count)
		{
		//	LogService.AddDebugLog("Call GetFromFilePath_ExistsOnly_Ranged: path={0}, index={1}, count={2}", path, index, count);
			return mediaDB.Value.Query<MediaItem>("select * from media where IsNotExist=0 AND FilePath like ? order by ID limit " + count + " offset " + index, path + "%");
		}

		/// <summary>
		/// DBリサイクル処理。
		/// IsNotExist=1 で LastUpdate が2日前以上が対象。
		/// </summary>
		/// <param name="run_vacuum">最適化してDBファイルサイズを縮小する場合はtrue</param>
		public void Recycle(bool run_vacuum)
		{
			LogService.AddDebugLog("Call Recycle");
			var date = DateTime.UtcNow.AddDays(-2).Ticks.ToString();
			mediaDB.Value.Recycle<MediaItem>(
				"IsNotExist=1",
				"IsNotExist=1 AND LastUpdate < " + date,
				(all, invalid) => ((all / 100) * 30) < invalid);
			if (run_vacuum)
				mediaDB.Value.Vacuum();
		}
		
		#region Definition

		/// <summary>
		/// DBメタ情報
		/// </summary>
		[SimpleDB.TableName("meta")]
		public sealed class MetaData
		{
			[SimpleDB.PrimaryKey]
			public string Key;
			public string Value;
		}

		#endregion

	}
}

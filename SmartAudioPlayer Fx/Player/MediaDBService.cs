using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Quala;
using Quala.Data;

namespace SmartAudioPlayerFx.Player
{
	/// <summary>
	/// データベース(media.db)を操作します
	/// </summary>
	static class MediaDBService
	{
		const int CURRENT_DB_VERSION = 3;
		static SimpleDB mediaDB;

		static MediaDBService()
		{
			LogService.AddDebugLog("MediaDBService", "Call ctor.");
			var filename = PreferenceService.CreateFullPath("data", "media.db");
			mediaDB = new SimpleDB(filename);

			// テーブル作成 (VACUUMする可能性があるのでトランザクションの外におくこと)
			mediaDB.CreateTable<MediaItem>();
			mediaDB.CreateTable<MetaData>();

			// メタデータチェック
			var meta = mediaDB.GetAll<MetaData>().ToDictionary(i => i.Key, i => i.Value);
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
				var asmName = Assembly.GetEntryAssembly().GetName();
				meta["Version"] = CURRENT_DB_VERSION.ToString();
				meta["CreatedDate"] = DateTime.Now.ToString();
				meta["CreatedBy"] = asmName.Name + ", Version=" + asmName.Version;
				var entries = meta.Select(v => new MetaData() { Key = v.Key, Value = v.Value }).ToArray();
				mediaDB.Insert(entries);
			}
			int version = int.Parse(meta["Version"]);
			if (version < CURRENT_DB_VERSION)
			{
				// 古いバージョンなのでxmlエクスポートして修正とか？
				LogService.AddErrorLog("MediaDBService", " - media db version " + meta["Version"] + ", not supported.");
				throw new ApplicationException("db version not supported.");
			}
			else if (version > CURRENT_DB_VERSION)
			{
				// DBバージョンが新しい？
				LogService.AddErrorLog("MediaDBService", " - media db version " + meta["Version"] + ", futured version?");
				throw new ApplicationException("db version futured?");
			}
			mediaDB.Commit();
		}

		/// <summary>
		/// 未保存のデータを保存します
		/// </summary>
		public static void SaveChanges()
		{
			LogService.AddDebugLog("MediaDBService", "Call SaveChanges");
			if (mediaDB == null) throw new InvalidOperationException("call first PrepareService()");
			mediaDB.Commit();
		}

		/// <summary>
		/// 指定アイテムをDBに追加します。
		/// 同じファイルパスのアイテムがあった場合は無視され、追加されたファイルだけが返ります。
		/// 追加後はIDが書き込まれます。
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public static IEnumerable<MediaItem> Insert(IEnumerable<MediaItem> items)
		{
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			var inserted_items = mediaDB.InsertOrIgnore(items);
			inserted_items
				.GroupBy(i => Path.GetDirectoryName(i.FilePath), StringComparer.CurrentCultureIgnoreCase)
				.Run(g => OnMediaItemChanged(false, g.Key, g.ToArray()));
			return inserted_items;
		}

		/// <summary>
		/// 指定アイテムのIDを使用して、指定カラムのデータを更新します。
		/// 更新するカラムを指定することもできます。
		/// ID == 0のアイテムは更新できません。
		/// </summary>
		/// <param name="item"></param>
		/// <param name="columns"></param>
		public static int Update(MediaItem item, params Expression<Func<MediaItem, object>>[] columns)
		{
			LogService.AddDebugLog("MediaDBService", "Call Update: item.ID={0}, item.FilePath={1}", item.ID, item.FilePath);
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			if (columns == null) throw new ArgumentNullException("columns");
			if (item.ID == 0) throw new ArgumentException("item.ID == 0", "item");

			// lastupdateを追加
			var list = new List<Expression<Func<MediaItem, object>>>(columns);
			list.Add(_ => _.LastUpdate);

			item.LastUpdate = DateTime.UtcNow.Ticks;
			var ret = mediaDB.UpdateTo(_ => _.ID, item, list.ToArray());
			OnMediaItemChanged(true, Path.GetDirectoryName(item.FilePath), item);
			return ret;
		}

		/// <summary>
		/// 指定ファイルパスのデータを取得します。
		/// DBにアイテムが無いときは新規に作成されます。
		/// さらに、バックグラウンドでタグ情報を取得、更新されます。
		/// ファイルが存在しない時はnullが返ります
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public static MediaItem GetOrCreate(string filepath)
		{
			LogService.AddDebugLog("MediaDBService", "Call GetOrCreate: filepath={0}", filepath);
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			if (string.IsNullOrWhiteSpace(filepath)) throw new ArgumentException("filepath");

			var exists = File.Exists(filepath);
			var item = mediaDB.Get<MediaItem>(_=>_.FilePath, filepath);
			if (item == null)
			{
				if (exists == false) return null;
				item = MediaItem.CreateDefault(filepath);
				var inserted_items = mediaDB.Insert(item);
				OnMediaItemChanged(false, Path.GetDirectoryName(filepath), inserted_items.ToArray());
			}
			else if (exists == false)
			{
				// ファイルが消されたっぽい
				item.IsNotExist = true;
				Update(item, _ => _.IsNotExist);
			}

			// アイテム情報バックグラウンド更新
			Task.Factory.StartNew(() =>
			{
				var finfo = new FileInfo(item.FilePath);
				var tag = MediaTagService.Get(item.FilePath);
				item.FilePath = finfo.FullName;
				item.Title = tag.Title;
				item.Artist = tag.Artist;
				item.Album = tag.Album;
				item.Comment = tag.Comment;
				item.UpdateSearchHint();
				item.CreatedDate = finfo.Exists ? finfo.CreationTimeUtc.Ticks : DateTime.MinValue.Ticks;
				item.LastWrite = finfo.Exists ? finfo.LastWriteTimeUtc.Ticks : DateTime.MinValue.Ticks;
				item.LastUpdate = DateTime.UtcNow.Ticks;
				item.IsNotExist = !finfo.Exists;
				item.GetFilePathDir(true);
				if (mediaDB.UpdateTo(
					_ => _.ID,
					item,
					_ => _.FilePath,
					_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
					_ => _.CreatedDate, _ => _.LastWrite, _ => _.LastUpdate, _ => _.IsNotExist) > 0)
					OnMediaItemChanged(true, finfo.DirectoryName, item);
			});
			return item;
		}

		/// <summary>
		/// 最近再生された(MediaItem.LastPlayが新しい順)項目のファイルパスを指定数まで取得します。
		/// 未再生(MediaItem.LastPlay == 0)は対象から外れます。
		/// </summary>
		/// <param name="limit"></param>
		/// <returns></returns>
		public static string[] RecentPlayItemsPath(int limit)
		{
			LogService.AddDebugLog("MediaDBService", "Call RecentPlayItems: limit={0}", limit);
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			return mediaDB
				.Query<MediaItem>("select FilePath from media where LastPlay > 0 order by LastPlay desc limit ?", limit)
				.Select(i => i.FilePath)
				.ToArray();
		}

		/// <summary>
		/// 指定アイテムより前に再生されたアイテムを取得します。
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static string PreviousPlayItem(MediaItem item)
		{
			LogService.AddDebugLog("MediaDBService", "Call PreviousPlayItem: item.FilePath={0}", item.FilePath);
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			return mediaDB
				.Query<MediaItem>("select FilePath from media where FilePath != ? and LastPlay < ? and LastPlay > 0 order by LastPlay desc limit 1", item.FilePath, item.LastPlay)
				.Select(i => i.FilePath)
				.FirstOrDefault();
		}

		/// <summary>
		/// GetItems()の範囲指定可能バージョン。
		/// ID順でソートされてから読みだします。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static MediaItem[] GetFromFilePath_Ranged(string path, int index, int count)
		{
		//	LogService.AddDebugLog("MediaDBService", "Call GetFromFilePath_Ranged: path={0}, index={1}, count={2}", path, index, count);
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			return mediaDB
				.Query<MediaItem>("select * from media where FilePath like ? order by ID limit " + count + " offset " + index, path + "%")
				.ToArray();
		}

		/// <summary>
		/// GetItems()の範囲指定可能+IsNotExist=0バージョン。
		/// ID順でソートされてから読みだします。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public static MediaItem[] GetFromFilePath_ExistsOnly_Ranged(string path, int index, int count)
		{
		//	LogService.AddDebugLog("MediaDBService", "Call GetFromFilePath_ExistsOnly_Ranged: path={0}, index={1}, count={2}", path, index, count);
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			return mediaDB
				.Query<MediaItem>("select * from media where IsNotExist=0 AND FilePath like ? order by ID limit " + count + " offset " + index, path + "%")
				.ToArray();
		}

		/// <summary>
		/// DBリサイクル処理。
		/// IsNotExist=1 で LastUpdate が2日前以上が対象。
		/// </summary>
		/// <param name="run_vacuum">最適化してDBファイルサイズを縮小する場合はtrue</param>
		public static void Recycle(bool run_vacuum)
		{
			LogService.AddDebugLog("MediaDBService", "Call Recycle");
			if (mediaDB == null) throw new InvalidOperationException("call first Start()");
			var date = DateTime.UtcNow.AddDays(-2).Ticks.ToString();
			mediaDB.Recycle<MediaItem>(
				"IsNotExist=1",
				"IsNotExist=1 AND LastUpdate < " + date,
				(all, invalid) => ((all / 100) * 30) < invalid);
			if (run_vacuum)
				mediaDB.Vacuum();
		}
		
		#region Event / Definition

		/// <summary>
		/// アイテム情報が更新されたら呼び出されます。
		/// </summary>
		public static event Action<MediaItemChangedEventArgs> MediaItemChanged;

		static void OnMediaItemChanged(bool is_fullinfo, string directory_path, params MediaItem[] items)
		{
			if (MediaItemChanged != null)
				MediaItemChanged(new MediaItemChangedEventArgs(is_fullinfo, directory_path, items));
		}

		/// <summary>
		/// アイテム情報更新通知用
		/// </summary>
		public class MediaItemChangedEventArgs : EventArgs
		{
			/// <summary>
			/// タグ情報が取得済みの場合true。
			/// falseの場合、IDとFilePath以外信用できません。
			/// </summary>
			public bool IsFullInfo { get; private set; }

			/// <summary>
			/// 今回のイベントでターゲットとなるディレクトリパス。
			/// このパス以外のファイルは来ないはず。
			/// </summary>
			public string DirectoryPath { get; private set; }

			/// <summary>
			/// アイテム
			/// </summary>
			public MediaItem[] Items { get; private set; }

			public MediaItemChangedEventArgs(bool is_fullinfo, string directory_path, MediaItem[] items)
			{
				this.IsFullInfo = is_fullinfo;
				this.DirectoryPath = directory_path;
				this.Items = items;
			}
		}

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

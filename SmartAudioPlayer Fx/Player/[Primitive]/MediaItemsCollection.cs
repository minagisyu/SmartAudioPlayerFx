using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Quala;

namespace SmartAudioPlayerFx.Player
{
	// 特定パスを基準にMeediaItemのインスタンスを収集する
	//  - MediaDBServiceからの更新情報を監視して追随更新する
	sealed class MediaItemsCollection
	{
		public MediaItemFilteringManager MediaItemFilteringManager { get; private set; }

		public MediaItemsCollection()
		{
			MediaItemFilteringManager = new MediaItemFilteringManager();
			MediaDBService.MediaItemChanged += e =>
				OnMediaChanged(e.IsFullInfo, e.DirectoryPath, e.Items);
		}

		#region フォルダオープン履歴(SetFocusPath()で設定される)

		public const int FOLDER_RECENTS_ITEMS_MAX = 20;
		List<string> opened_folder_recents;
		public string[] GetFolderRecents()
		{
			return
				(opened_folder_recents != null) ? opened_folder_recents.ToArray() :
				new string[0];
		}
		public void AddFolderRecents(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;
			if (opened_folder_recents == null)
				opened_folder_recents = new List<string>();
			//
			opened_folder_recents
				.RemoveAll(i => i.Equals(path, StringComparison.CurrentCultureIgnoreCase));
			opened_folder_recents
				.Insert(0, path);
			if (opened_folder_recents.Count >= FOLDER_RECENTS_ITEMS_MAX)
				opened_folder_recents.RemoveRange(
					FOLDER_RECENTS_ITEMS_MAX,
					opened_folder_recents.Count - (FOLDER_RECENTS_ITEMS_MAX + 1));
		}
		public void ClearFolderRecents()
		{
			if (opened_folder_recents != null)
				opened_folder_recents.Clear();
		}

		#endregion
		#region FocusPath

		public event Action FocusPathChanged;
		public string FocusPath { get; private set; }
		public void SetFocusPath(string path, bool isCollectNow, Action onReloaded = null)
		{
			LogService.AddDebugLog("MediaItemsCollection", "Call SetFocusPath: path={0}, isCollectNow={1}", path, isCollectNow);
			MediaItemExtension.ClearAllCache();
			FocusPath = path;
			AddFolderRecents(path);
			if (FocusPathChanged != null)
				FocusPathChanged();
			ReloadItems(isCollectNow, onReloaded);
		}

		#endregion

		// Itemsプロパティ自体が更新された(Resetted)
		public event Action ItemsChanged;
		// Itemsにアイテムが追加/削除/更新された
		public event Action<MediaDBService.MediaItemChangedEventArgs> ItemsAdded;
		public event Action<MediaDBService.MediaItemChangedEventArgs> ItemsRemoved;
		public event Action<MediaDBService.MediaItemChangedEventArgs> ItemsUpdated;
		// キャッシュ済みアイテム<ID, Item>
		public ConcurrentDictionary<long, MediaItem> Items { get; private set; }
		// FocusPathをもとにファイル検索をする
		FilesSource files_source = new DirectoryFileSource();

		CancellationTokenSource ReloadItems_CTS = null;
		// アイテム情報をリセットしてDBから再読み込み、場合によってファイル収集
		void ReloadItems(bool isCollectNow, Action onReloaded = null)
		{
			LogService.AddDebugLog("MediaItemsCollection", "Call ReloadItems: isCollectNow={0}", isCollectNow);
			// リセット
			Items = new ConcurrentDictionary<long, MediaItem>();
			if (ItemsChanged != null)
				ItemsChanged();
			// パスが空なら何もしない
			var focus_path = FocusPath;
			if (string.IsNullOrEmpty(focus_path))
				return;
			// 以前の検索をキャンセル
			if (ReloadItems_CTS != null)
				ReloadItems_CTS.Cancel();
			ReloadItems_CTS = new CancellationTokenSource();
			var ct = ReloadItems_CTS.Token;
			// TODO:
			// ファイルパスならプレイリスト解析、フォルダならDB読み込んでからファイル検索
			// とりあえずフォルダ固定
			Task.Factory.StartNew(() =>
			{
				var predicater = new Predicate<MediaItem>(MediaItemFilteringManager.Validate);
				var sw = Stopwatch.StartNew();
				for (var index = 0; ct.IsCancellationRequested == false; index += 2000)
				{
					var items = MediaDBService
						.GetFromFilePath_ExistsOnly_Ranged(focus_path, index, 2000)
						.Where(i => predicater(i))
						.GroupBy(i => i.GetFilePathDir())
						.ToArray();
					foreach (var g in items)
					{
						if (ct.IsCancellationRequested) break;
						OnMediaChanged(true, g.Key, g.ToArray());
					}
					if (items.Length == 0) break;
				}
				sw.Stop();
				LogService.AddDebugLog("MediaItemsCollection", " **ReloadItems: {0}ms", sw.ElapsedMilliseconds);
				if (onReloaded != null)
					onReloaded();
				if (isCollectNow && files_source != null)
					files_source.CollectStart(focus_path, ct);
			});
		}

		// ItemFilterServiceが更新されたとき用、除外ファイルは削除扱いで
		public void ReValidate_Items()
		{
			LogService.AddDebugLog("MediaItemsCollection", "Call ReValidate_Items");

			var focus_path = FocusPath;
			if (string.IsNullOrEmpty(focus_path)) return;

			Task.Factory.StartNew(() =>
			{
				var sw = Stopwatch.StartNew();
				// Remove
				var predicater = new Predicate<MediaItem>(MediaItemFilteringManager.Validate);
				// 合わないファイルは削除扱い
				var removed_list = Items
					.Select(i=>i.Value)
					.Where(item => predicater(item) == false)
					.ToList();
				MediaItem tmp;
				removed_list
					.Select(item => item.ID)
					.Run(i => Items.TryRemove(i, out tmp));
				if (ItemsRemoved != null && removed_list.Count != 0)
				{
					removed_list
						.GroupBy(i => i.GetFilePathDir())
						.Run(g => ItemsRemoved(new MediaDBService.MediaItemChangedEventArgs(false, g.Key, g.ToArray())));
				}
				// Add
				for (var index = 0; ; index += 2000)
				{
					var items = MediaDBService.GetFromFilePath_ExistsOnly_Ranged(focus_path, index, 2000);
					items
						.Where(i => predicater(i))
						.GroupBy(i => i.GetFilePathDir())
						.Run(g => OnMediaChanged(true, g.Key, g.ToArray()));
					if (items.Length == 0) break;
				}
				sw.Stop();
				LogService.AddDebugLog("MediaItemsCollection", " **ReValidate_Items: {0}ms", sw.ElapsedMilliseconds);
			});
		}

		/// <summary>
		/// CollectItems()処理中に呼び出されます。
		/// 状態テキストは「[1] フォルダパス」という形式です。
		/// [1] - ファイル検索中
		/// [2] - ファイル生存確認中
		/// [3] - タグ情報取得中
		/// [4] - フォルダ監視中のファイル検索中
		/// [5] - フォルダ監視中のファイル生存確認中
		/// </summary>
		public event Action<string> ItemsCollecting;
		/// <summary>
		/// CollectItems()処理中に、ファイル検索とファイル存在確認がすんだ時点で呼び出されます。
		/// タグ情報の取得は時間がかかるので、その救済措置。
		/// </summary>
		public event Action ItemCollect_CoreScanFinished;

		void OnMediaChanged(bool is_fullinfo, string dir_path, MediaItem[] changedItems)
		{
			var focus_path = FocusPath;
			if (string.IsNullOrEmpty(focus_path) ||
				!dir_path.StartsWith(focus_path, StringComparison.CurrentCultureIgnoreCase))
				return;

			var predicater = new Predicate<MediaItem>(MediaItemFilteringManager.Validate);
			var added_list = new List<MediaItem>();
			var remoed_list = new List<MediaItem>();
			var updated_list = new List<MediaItem>();
			changedItems
				.Where(item => item.ID != 0 && predicater(item)) // ID == 0はないという仮定で
				.Run(item =>
				{
					MediaItem has_item;
					if (Items.TryGetValue(item.ID, out has_item) == false)
					{
						// アイテムがリスト内に存在しないので追加
						Items.TryAdd(item.ID, item);
						added_list.Add(item);
					}
					else if (item.IsNotExist)
					{
						// ファイルが削除されたのでリスト内から削除
						MediaItem tmp;
						Items.TryRemove(item.ID, out tmp);
						remoed_list.Add(item);
					}
					else
					{
						// アイテム情報が更新されたのでデータ書き換え
						has_item.ID = item.ID;
						has_item.FilePath = item.FilePath;
						has_item.GetFilePathDir(true);
						if (is_fullinfo)
						{
							item.CopyTo(has_item);
						}
						updated_list.Add(item);
					}
				});
			if (ItemsAdded != null && added_list.Count != 0)
				ItemsAdded(new MediaDBService.MediaItemChangedEventArgs(is_fullinfo, dir_path, added_list.ToArray()));
			if (ItemsRemoved != null && remoed_list.Count != 0)
				ItemsRemoved(new MediaDBService.MediaItemChangedEventArgs(is_fullinfo, dir_path, remoed_list.ToArray()));
			if (ItemsUpdated != null && updated_list.Count != 0)
				ItemsUpdated(new MediaDBService.MediaItemChangedEventArgs(is_fullinfo, dir_path, updated_list.ToArray()));
		}

		#region FilesSource

		abstract class FilesSource
		{
			// 時間の掛からない最低限のスキャン
			protected abstract object CollectItems_CoreScan(string path, CancellationToken token);
			// 時間のかかる、CoreScan後の詳細スキャン
			protected abstract void CollectItems_PostScan(string path, CancellationToken token, object corescan_result);

			public void CollectStart(string path, CancellationToken token)
			{
				LogService.AddDebugLog("FilesSource", "Call CollectStart: path={0}", path);
				if (string.IsNullOrEmpty(path)) return;
				Task.Factory.StartNew(() =>
				{
					LogService.AddDebugLog("FilesSource", " - CollectItems start.");
					try
					{
						var focus_path = JukeboxService.AllItems.FocusPath;
						var core_ret = CollectItems_CoreScan(focus_path, token);
						if (JukeboxService.AllItems.ItemCollect_CoreScanFinished != null)
							JukeboxService.AllItems.ItemCollect_CoreScanFinished();
						MediaDBService.Recycle(false);
						CollectItems_PostScan(focus_path, token, core_ret);
					}
					finally
					{
						LogService.AddDebugLog("FilesSource", " - CollectItems finished.");
					}
				});
			}
		}

		sealed class DirectoryFileSource : FilesSource
		{
			/// <summary>
			/// 指定パスからファイルを列挙し、DBに追加します。
			/// また、DBに格納されている指定パス以下のファイルに対して存在確認を行います。
			/// 列挙されるファイルはItemFilterServiceによるフィルタ処理が行われます。
			/// </summary>
			protected override object CollectItems_CoreScan(string path, CancellationToken token)
			{
				LogService.AddDebugLog("DirectoryFileSource", "Call CollectItems_CoreScan: path={0}", path);
				if (Directory.Exists(path) == false) return null;

				// フォルダ監視を有効に
				var fsw = new FileSystemWatcher(path);
				fsw.IncludeSubdirectories = true;
				fsw.InternalBufferSize = 0x10000;
				fsw.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite;
				fsw.EnableRaisingEvents = true;

				// phase.1: new item add...
				var predicater = new Predicate<MediaItem>(JukeboxService.AllItems.MediaItemFilteringManager.Validate);
				var sw_phase = Stopwatch.StartNew();
				var files = EnumerateAllFiles(path)
					.Select(file =>
					{
						var item = MediaItem.CreateDefault(file);
						item.LastWrite = DateTime.MinValue.Ticks;	// 後で更新するため
						return item;
					})
					.Where(i => predicater(i))
					.TakeWithSplit(1000, token);
				foreach (var mass in files)
				{
					if (token.IsCancellationRequested) break;
					foreach (var g in mass.GroupBy(i => i.GetFilePathDir()))
					{
						if (token.IsCancellationRequested) break;
						if (JukeboxService.AllItems.ItemsCollecting != null)
							JukeboxService.AllItems.ItemsCollecting("[1] " + g.Key);
						MediaDBService.Insert(g.AsEnumerable());
					}
				}
				sw_phase.Stop();
				LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase1(file collect) finish. >>>" + sw_phase.ElapsedMilliseconds + "ms.");

				// phase.2: exists check...
				sw_phase.Restart();
				for (var offset = 0; token.IsCancellationRequested == false; offset += 1000)
				{
					var items = MediaDBService.GetFromFilePath_Ranged(path, offset, 1000);
					if (items.Length == 0) break;
					var q_phase2 = items
						.Select(i => new { Item = i, Exists = File.Exists(i.FilePath) })
						.Where(i => i.Item.IsNotExist != !i.Exists)
						.Select(i =>
						{
							i.Item.LastUpdate = DateTime.UtcNow.Ticks;
							i.Item.IsNotExist = !i.Exists;
							return i.Item;
						})
						.GroupBy(i => i.GetFilePathDir());
					foreach (var g in q_phase2)
					{
						if (token.IsCancellationRequested) break;
						if (JukeboxService.AllItems.ItemsCollecting != null)
							JukeboxService.AllItems.ItemsCollecting("[2] " + g.Key);
						// 状態が変化してたら更新＆通知する
						g.Run(item => MediaDBService.Update(item, _ => _.LastUpdate, _ => _.IsNotExist));
					}
				}
				MediaDBService.SaveChanges();
				sw_phase.Stop();
				LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase2(exist check) finish. >>>" + sw_phase.ElapsedMilliseconds + "ms.");

				return fsw;
			}

			/// <summary>
			/// タグ情報の更新も行います。
			/// また、CollectItems_CoreScan()直後から追加削除されたファイル情報を収集し処理します。
			/// </summary>
			protected override void CollectItems_PostScan(string path, CancellationToken token, object corescan_result)
			{
				LogService.AddDebugLog("DirectoryFileSource", "Call CollectItems_PostScan: path={0}", path);
				if (corescan_result == null) return;

				// phase.3: taginfo recheck...
				var sw_phase = Stopwatch.StartNew();
				for (var offset = 0; token.IsCancellationRequested == false; offset += 50)
				{
					var items = MediaDBService.GetFromFilePath_ExistsOnly_Ranged(path, offset, 50);
					if (items.Length == 0) break;
					// ファイルが存在していて、最終更新日が新しいもの or dateTime.MinValueに限定
					var q_phase3 = items
						.Where(i => File.Exists(i.FilePath))
						.Where(i => (i.LastWrite == DateTime.MinValue.Ticks) || (i.LastWrite < File.GetLastWriteTimeUtc(i.FilePath).Ticks))
						.Select(i => new { Item = i, Tag = MediaTagService.Get(i.FilePath), })
						.GroupBy(i => i.Item.GetFilePathDir());
					foreach (var g in q_phase3)
					{
						if (token.IsCancellationRequested) break;
						if (JukeboxService.AllItems.ItemsCollecting != null)
							JukeboxService.AllItems.ItemsCollecting("[3] " + g.Key);
						g.Select(i =>
						{
							i.Item.FilePath = i.Tag.FilePath;
							i.Item.Title = i.Tag.Title;
							i.Item.Artist = i.Tag.Artist;
							i.Item.Album = i.Tag.Album;
							i.Item.Comment = i.Tag.Comment;
							i.Item.UpdateSearchHint();
							i.Item.CreatedDate = File.GetCreationTimeUtc(i.Tag.FilePath).Ticks;
							i.Item.LastWrite = File.GetLastWriteTimeUtc(i.Tag.FilePath).Ticks;
							i.Item.IsNotExist = false;
							i.Item.GetFilePathDir(true);
							return i.Item;
						})
						.Run(item =>
						{
							MediaDBService.Update(item,
								_ => _.FilePath,
								_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
								_ => _.CreatedDate, _ => _.LastWrite, _ => _.IsNotExist);
						});
					}
				}
				MediaDBService.SaveChanges();
				sw_phase.Stop();
				LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase3(tag collect) finish. >>>" + sw_phase.ElapsedMilliseconds + "ms.");

				// phase.4: append check...
				var predicater = new Predicate<MediaItem>(JukeboxService.AllItems.MediaItemFilteringManager.Validate);
				var fsw = (FileSystemWatcher)corescan_result;
				var exists_scan_require = new ConcurrentQueue<string>();
				Task idle_task = null;
				while (token.IsCancellationRequested == false)
				{
					var ret = fsw.WaitForChanged(WatcherChangeTypes.All, 1000);
					if (ret.TimedOut)
					{
						if (idle_task == null || idle_task.Status == TaskStatus.RanToCompletion || idle_task.Status == TaskStatus.Faulted)
						{
							idle_task = Task.Factory.StartNew(() =>
							{
								string target_item;
								while (exists_scan_require.TryDequeue(out target_item))
								{
									LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase4.5(directory rescan): path={0}", target_item);
									if (JukeboxService.AllItems.ItemsCollecting != null)
										JukeboxService.AllItems.ItemsCollecting("[5] " + target_item);
									// phase.4.5: exists check...
									for (var offset = 0; token.IsCancellationRequested == false; offset += 1000)
									{
										var items = MediaDBService.GetFromFilePath_Ranged(target_item, offset, 1000);
										if (items.Length == 0) break;
										var q_phase4_5 = items
											.Select(i => new { Item = i, Exists = File.Exists(i.FilePath) })
											.Where(i => i.Item.IsNotExist != !i.Exists)
											.Select(i =>
											{
												i.Item.IsNotExist = !i.Exists;
												return i.Item;
											})
											.GroupBy(i => i.GetFilePathDir());
										foreach (var g in q_phase4_5)
										{
											if (token.IsCancellationRequested) break;
											// 状態が変化してたら更新＆通知する
											g.Run(item => MediaDBService.Update(item, _ => _.IsNotExist));
										}
									}
								}
							});
						}
						continue;
					}
					// フォルダに変更があった
					var fullname = Path.Combine(path, ret.Name);
					if (ret.ChangeType == WatcherChangeTypes.Created || ret.ChangeType == WatcherChangeTypes.Renamed)
					{
						if (Directory.Exists(fullname))
						{
							LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase4(directory watch): path={0}, (dir)", fullname);
							Task.Factory.StartNew(() =>
							{
								var files = EnumerateAllFiles(fullname)
									.Select(file => MediaItem.CreateDefault(file))
									.Where(i => predicater(i))
									.TakeWithSplit(1000, token);
								foreach (var mass in files)
								{
									if (token.IsCancellationRequested) break;
									foreach (var g in mass.GroupBy(i => i.GetFilePathDir()))
									{
										if (JukeboxService.AllItems.ItemsCollecting != null)
											JukeboxService.AllItems.ItemsCollecting("[4] " + g.Key);
										g.Run(i => MediaDBService.GetOrCreate(i.FilePath));
									}
								}
							});
						}
						else if (File.Exists(fullname))
						{
							LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase4(directory watch): path={0}, (file)", fullname);
							Task.Factory.StartNew(() =>
							{
								if (JukeboxService.AllItems.ItemsCollecting != null)
									JukeboxService.AllItems.ItemsCollecting("[4] " + Path.GetDirectoryName(fullname));
								MediaDBService.GetOrCreate(fullname);
							});
						}
					}
					if (ret.ChangeType == WatcherChangeTypes.Renamed)
					{
						var target = Path.Combine(path, ret.OldName);
						if (Directory.Exists(fullname))
						{
							LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase4(directory watch): path={0}, old={1}, (rename-dir)", fullname, target);
							exists_scan_require.Enqueue(target);
						}
						else if (File.Exists(fullname))
						{
							target = Path.GetDirectoryName(target);
							LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase4(directory watch): path={0}, old={1}, (rename-file)", fullname, target);
							exists_scan_require.Enqueue(target);
						}
					}
					if (ret.ChangeType == WatcherChangeTypes.Deleted)
					{
						var target = Path.GetDirectoryName(fullname);
						LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase4(directory watch): path={0}, target={1}, (delete)", fullname, target);
						exists_scan_require.Enqueue(target);
					}
					if (ret.ChangeType == WatcherChangeTypes.Changed)
					{
						LogService.AddDebugLog("DirectoryFileSource", " - CollectItems phase4(directory watch): path={0}, (changed)", fullname);
						if (Directory.Exists(fullname))
							exists_scan_require.Enqueue(fullname);
					}
				}
			}

			// 指定パスを例外処理しながら再帰検索
			static IEnumerable<string> EnumerateAllFiles(string path)
			{
				IEnumerable<string> files = null;
				IEnumerable<string> dirs = null;
				try
				{
					files = Directory.EnumerateFiles(path);
					dirs = Directory.EnumerateDirectories(path);
				}
				catch (UnauthorizedAccessException) { }
				catch (DirectoryNotFoundException) { }
				catch (IOException) { }

				var subdir = (dirs ?? Enumerable.Empty<string>())
					.SelectMany(d => EnumerateAllFiles(d));
				return (files ?? Enumerable.Empty<string>())
					.Concat(subdir);
			}
		}

		#endregion
	}
}

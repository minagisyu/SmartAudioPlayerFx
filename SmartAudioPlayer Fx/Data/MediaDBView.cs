using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Quala;
using SmartAudioPlayerFx.Windows;

namespace SmartAudioPlayerFx.Data
{
	// MediaDBView
	// ◆FocusPathを設定するとDBから対象だけを読み込んでインスタンス化(Items)
	//   さらにファイルを検索して情報を更新、DBに反映、ファイルシステムへの変更を監視する
	//------------------------------------------------------------------------
	// ・指定ファイルパスをバックエンドから読み込んでオブジェクト化(DB-View)
	// ・指定ファイルパスを検索して追加、アイテム情報の更新をDBに反映(DB-Controller)
	// ・アイテム情報変更のイベント通知
	sealed class MediaDBView : IDisposable
	{
		MediaDB mediaDB;
		public MediaItemFilter MediaItemFilter { get; private set; }

		public MediaDBView(string db_filename)
		{
			mediaDB = new MediaDB(db_filename);
			MediaItemFilter = new MediaItemFilter();
		}

		#region IDisposable

		~MediaDBView() { Dispose(false); }
		public void Dispose() { Dispose(true); }
		void Dispose(bool disposing)
		{
			if (disposing)
			{
				mediaDB.Dispose();
				GC.SuppressFinalize(this);
			}
		}

		#endregion


		public event Action FocusPathChanged;
		public string FocusPath { get; private set; }

		/// <summary>
		/// FocusPathを変更します
		/// </summary>
		/// <param name="path">検索対象のディレクトリパス、null可(バックグラウンドで動いている検索処理も停止)</param>
		/// <param name="isCollectNow">バックグラウンドでファイル検索処理を行うか</param>
		/// <param name="onReloading">DBからアイテムの読み込みを行う前に呼び出されます</param>
		/// <param name="onReloaded">DBからのアイテム読み込み処理が終了した時点で呼び出されます(ファイル検索はまだ)</param>
		public void SetFocusPath(string path, bool isCollectNow, Action onReloading = null,  Action onReloaded = null)
		{
			LogService.AddDebugLog("Call SetFocusPath: path={0}, isCollectNow={1}", path, isCollectNow);
			if (string.IsNullOrEmpty(path) == false && Path.IsPathRooted(path) == false)
				throw new ArgumentException("フォルダパスがルートから始まっていない", "path");
			MediaItemExtension.ClearAllCache();	// ファイルパスが違うとアテにならなくなるのでキャッシュクリア
			FocusPath = path;
			if (FocusPathChanged != null)
				FocusPathChanged();
			var itemsReset = (path != null);
			ReloadItems(isCollectNow, itemsReset, onReloading, onReloaded);
		}

		
		public event Action<MediaItemChangedEventArgs> ItemChanged;					// Itemsにアイテムが追加/削除/更新された
		public event Action ItemsPropertyChanged;									// Itemsプロパティ自体が更新された(Resetted)
		public ConcurrentDictionary<long, MediaItem> Items { get; private set; }	// キャッシュ済みアイテム<ID, Item>
		CancellationTokenSource ReloadItems_CTS = null;								// ReloadItems()呼び出しによって、先に行われたファイル検索処理をキャンセルするのに使う

		// アイテム情報をリセットしてDBから再読み込み、場合によってファイル収集
		void ReloadItems(bool isCollectNow, bool itemsReset = true, Action onReloading = null, Action onReloaded = null)
		{
			LogService.AddDebugLog("Call ReloadItems: isCollectNow={0}", isCollectNow);

			// 以前の検索をキャンセル
			if (ReloadItems_CTS != null)
			{
				ReloadItems_CTS.Cancel();
				ReloadItems_CTS.Dispose();
			}
			ReloadItems_CTS = new CancellationTokenSource();
			var ct = ReloadItems_CTS.Token;

			// リセット
			if (itemsReset)
			{
				Items = new ConcurrentDictionary<long, MediaItem>();
				if (ItemsPropertyChanged != null)
					ItemsPropertyChanged();
			}

			// パスが空なら何もしない (検索処理中止の為にFocusPath=nullが許容されるため)
			var focus_path = FocusPath;
			if (string.IsNullOrEmpty(focus_path))
				return;

			// DB読み込んでからファイル検索
			if (onReloading != null)
				onReloading();
			var sw = Stopwatch.StartNew();
			for (var index = 0; ct.IsCancellationRequested == false; index += 2000)
			{
				var items = mediaDB.GetFromFilePath_ExistsOnly_Ranged(focus_path, index, 2000);
				if (items.Length == 0) break;
				var q_phase0 = items.GroupBy(i => i.GetFilePathDir());
				foreach (var g in q_phase0)
				{
					if (ct.IsCancellationRequested) break;
					var collecting = ItemsCollecting;
					if (collecting != null)
						collecting("[0] " + g.Key);
					OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Add, g.Key, g.ToArray()));
				}
			}
			sw.Stop();
			LogService.AddDebugLog(" **ReloadItems: {0}ms", sw.ElapsedMilliseconds);
			if (onReloaded != null)
				onReloaded();
			if (isCollectNow)
				CollectStart(focus_path, ct);
		}

		CancellationTokenSource ReValidateItems_CTS = null;
		// ItemFilterServiceが更新されたとき用、除外ファイルは削除扱いで
		public void ReValidate_Items()
		{
			LogService.AddDebugLog("Call ReValidate_Items");

			var focus_path = FocusPath;
			if (string.IsNullOrEmpty(focus_path)) return;

			if (ReValidateItems_CTS != null)
			{
				ReValidateItems_CTS.Cancel();
				ReValidateItems_CTS.Dispose();
			}
			ReValidateItems_CTS = new CancellationTokenSource();
			var ct = ReValidateItems_CTS.Token;

			var sw = Stopwatch.StartNew();

			// MediaItemFilteringManagerが更新されたとき用。
			// this.Itemsに存在していて、Validate()を通らないものを削除
			Items
				.Select(i => i.Value)
				.Where(item => MediaItemFilter.Validate(item) == false)
				.TakeWithSplit(2000, ct)
				.Run(s=>
				{
					s.GroupBy(i => i.GetFilePathDir())
						.Run(g => OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Remove, g.Key, g.ToArray())));
				});

			// Add
			for (var index = 0; ct.IsCancellationRequested == false; index += 2000)
			{
				var items = mediaDB.GetFromFilePath_ExistsOnly_Ranged(focus_path, index, 2000);
				if (items.Length == 0) break;
				items
					.GroupBy(i => i.GetFilePathDir())
					.Run(g => OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Add, g.Key, g.ToArray())));
			}
			sw.Stop();
			LogService.AddDebugLog(" **ReValidate_Items: {0}ms", sw.ElapsedMilliseconds);
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

		void OnMediaChanged(MediaItemChangedEventArgs e)
		{
			var list = Items;
			if (list == null) return;

			var focus_path = FocusPath;
			var items = e.Items.Where(i => i.ID != 0).ToArray();	// ID == 0はないという仮定で
			if (string.IsNullOrWhiteSpace(focus_path) ||
				e.DirectoryName.StartsWith(focus_path) == false ||
				items.Any() == false)
			{
				return;
			}
			//
			var itemchanged = ItemChanged;
			if (e.ChangedType == MediaItemChangedType.Add)
			{
				// Add: アイテムがリスト内に存在しないので追加
				var items2 =
					(from i in items
					 where list.ContainsKey(i.ID) == false
					 where MediaItemFilter.Validate(i)
					 where list.TryAdd(i.ID, i)
					 select i).ToArray();
				if (items2.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaItemChangedEventArgs(MediaItemChangedType.Add, e.DirectoryName, items2)); }
					catch { }
				}
			}
			else if (e.ChangedType == MediaItemChangedType.Remove)
			{
				// Remove: ファイルが削除されたのでリスト内から削除
				MediaItem tmp;
				var items2 =
					(from i in items
					 where list.TryRemove(i.ID, out tmp)
					 select i).ToArray();
				if (items2.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaItemChangedEventArgs(MediaItemChangedType.Remove, e.DirectoryName, items2)); }
					catch { }
				}
			}
			else if (e.ChangedType == MediaItemChangedType.Update)
			{
				MediaItem tmp = null;
				// アイテム情報がないなら追加
				var items0 =
					(from i in items
					 where i.IsNotExist == false
					 where list.ContainsKey(i.ID) == false
					 select i).ToArray();
				if (items0.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaItemChangedEventArgs(MediaItemChangedType.Add, e.DirectoryName, items0)); }
					catch { }
				}
				// Update: アイテム情報が更新されたのでデータ書き換え
				var items2 =
					(from i in items
					where list.TryGetValue(i.ID, out tmp)
					let tmp2 = tmp
					select new { i, tmp2, })
					.Do(l=>
					{
						l.i.CopyTo(l.tmp2);
						l.tmp2.GetFilePathDir(true);	// とりあえず再生成を指示
					})
					.Select(l => l.i)
					.ToArray();
				if (items2.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaItemChangedEventArgs(MediaItemChangedType.Update, e.DirectoryName, items2)); }
					catch { }
				}
				// ファイルが存在しないならリストから削除 (IsNotExistが更新された場合の処理)
				var items3 =
					(from i in items2
					 where i.IsNotExist
					 where list.TryRemove(i.ID, out tmp)
					 select i).ToArray();
				if (items3.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaItemChangedEventArgs(MediaItemChangedType.Remove, e.DirectoryName, items3)); }
					catch { }
				}
			}
		}

		/// <summary>
		/// 指定ファイルパスのデータを取得します。
		/// DBにアイテムが無いときは新規に作成され、DBに存在するがファイルがない場合は情報が更新されます。
		/// さらに、バックグラウンドでタグ情報を取得、更新されます。
		/// ファイルが存在しない時はnullが返ります
		/// </summary>
		/// <param name="filepath"></param>
		/// <returns></returns>
		public MediaItem GetOrCreate(string filepath)
		{
			LogService.AddDebugLog("Call GetOrCreate: filepath={0}", filepath);
			if (string.IsNullOrWhiteSpace(filepath))
				return null;

			var exists = File.Exists(filepath);
			var item = mediaDB.Get(filepath);
			if (item == null)
			{
				if (exists == false) return null;
				item = MediaItem.CreateDefault(filepath);
				mediaDB.Insert(new[] { item, })
					.GroupBy(i => i.GetFilePathDir())
					.Run(g => OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Add, g.Key, g.ToArray())));
			}
			else if (exists == false)
			{
				// ファイルが消されたっぽい
				item.IsNotExist = true;
				item.LastUpdate = DateTime.UtcNow.Ticks;
				RaiseDBUpdate(item, _ => _.IsNotExist, _ => _.LastUpdate);
			}
			return item;
		}

		/// <summary>
		/// itemの指定プロパティの情報をDBへ書き込みます
		/// </summary>
		/// <param name="item"></param>
		/// <param name="columns"></param>
		public void RaiseDBUpdate(MediaItem item, params Expression<Func<MediaItem, object>>[] columns)
		{
			mediaDB.Update(new[] { item }, columns)
				.GroupBy(i => i.GetFilePathDir())
				.Run(g => OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Update, g.Key, g.ToArray())));
		}

		/// <summary>
		/// DBへ書き込みを確定します
		/// </summary>
		public void RaiseDBCommit()
		{
			mediaDB.SaveChanges();
		}

		/// <summary>
		/// 最近再生された(MediaItem.LastPlayが新しい順)項目のファイルパスを指定数まで取得します。
		/// 未再生(MediaItem.LastPlay == 0)は対象から外れます。
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		public string[] RecentPlayItemsPath(int count)
		{
			return mediaDB.RecentPlayItemsPath(count);
		}

		/// <summary>
		/// 指定アイテムより前に再生されたアイテムを取得します。
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public string PreviousPlayItem(MediaItem item)
		{
			return mediaDB.PreviousPlayItem(item);
		}

		/// <summary>
		/// DBリサイクル処理。
		/// IsNotExist=1 で LastUpdate が2日前以上が対象。
		/// </summary>
		public void Recycle()
		{
			mediaDB.Recycle(true);
		}

		#region FilesSource

		/// <summary>
		/// 指定パスを例外処理しながら再帰検索。
		/// ディレクトリパスとそのディレクトリのファイル群を一纏めにして返します。
		/// </summary>
		/// <param name="path"></param>
		/// <param name="token"></param>
		/// <returns>(ディレクトリパス, IEnumerable(ファイルパス))のTuple[]</returns>
		static IEnumerable<Tuple<string, IEnumerable<string>>> EnumerateAllFiles2(string path, CancellationToken token)
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

			if (token.IsCancellationRequested) yield break;
			yield return Tuple.Create(path, (files ?? Enumerable.Empty<string>()));

			var subdir = (dirs ?? Enumerable.Empty<string>())
				.Where(_=>token.IsCancellationRequested == false)
				.SelectMany(d => EnumerateAllFiles2(d, token));
			foreach (var i in subdir)
				yield return i;
		}

		void CollectStart(string path, CancellationToken token)
		{
			LogService.AddDebugLog("Call CollectStart: path={0}", path);
			if (string.IsNullOrEmpty(path)) return;

			LogService.AddDebugLog(" - CollectItems start.");
			try { OnCollectItems(path, token); }
			catch (ObjectDisposedException) { }	// SimpleDBがDisposeされたあとに処理すると発生
			finally { LogService.AddDebugLog(" - CollectItems finished."); }
		}

		void OnCollectItems(string path, CancellationToken token)
		{
			LogService.AddDebugLog("Call OnCollectItems: path={0}", path);
			if (Directory.Exists(path) == false) return;

			// フォルダ監視を有効に
			var fsw = new FileSystemWatcher(path);
			fsw.IncludeSubdirectories = true;
			fsw.InternalBufferSize = 0x10000;
			fsw.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite;
			fsw.EnableRaisingEvents = true;

			// phase.1: new item add...
			var sw_phase = Stopwatch.StartNew();
			EnumerateAllFiles2(path, token)
				.Select(dir =>
				{
					var files = dir.Item2.Select(file =>
					{
						var item = MediaItem.CreateDefault(file);
						item.LastWrite = DateTime.MinValue.Ticks;	// 後(phase3)で更新するため
						return item;
					})
					.Where(MediaItemFilter.Validate);
					return new { Path = dir.Item1, Files = files, };
				})
				.Run(target =>
				{
					var collecting = ItemsCollecting;
					if (collecting != null)
						collecting("[1] " + target.Path);
					var result = mediaDB.Insert(target.Files);
					OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Add, target.Path, result));
				});
			RaiseDBCommit();
			sw_phase.Stop();
			LogService.AddDebugLog(" - CollectItems phase1(file collect) finish. >>>" + sw_phase.ElapsedMilliseconds + "ms.");

			// phase.2: exists check...
			sw_phase.Restart();
			for (var offset = 0; token.IsCancellationRequested == false; offset += 1000)
			{
				var items = mediaDB.GetFromFilePath_Ranged(path, offset, 1000);
				if (items.Length == 0) break;
				var q_phase2 = items.GroupBy(i => i.GetFilePathDir());
				foreach (var g in q_phase2)
				{
					if (token.IsCancellationRequested) break;
					var collecting = ItemsCollecting;
					if (collecting != null)
						collecting("[2] " + g.Key);
					//
					var q = g
						.Select(i => new { Item = i, Exists = File.Exists(i.FilePath) })
						.Where(i => i.Item.IsNotExist != !i.Exists)
						.Select(i =>
						{
							i.Item.IsNotExist = !i.Exists;
							return i.Item;
						});
					// 状態が変化してたら更新＆通知する
					var result = mediaDB.Update(q.ToArray(), _ => _.LastUpdate, _ => _.IsNotExist);
					OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Update, g.Key, result));
				}
			}
			RaiseDBCommit();
			sw_phase.Stop();
			LogService.AddDebugLog(" - CollectItems phase2(exist check) finish. >>>" + sw_phase.ElapsedMilliseconds + "ms.");

			// corescan finish
			var scan_finished = ItemCollect_CoreScanFinished;
			if (scan_finished != null)
				scan_finished();
			mediaDB.Recycle(false);

			// phase.3: taginfo recheck...
			sw_phase.Restart();
			for (var offset = 0; token.IsCancellationRequested == false; offset += 50)
			{
				var items = mediaDB.GetFromFilePath_ExistsOnly_Ranged(path, offset, 50);
				if (items.Length == 0) break;
				// ファイルが存在していて、最終更新日が新しいもの or dateTime.MinValueに限定
				var q_phase3 = items.GroupBy(i => i.GetFilePathDir());
				foreach (var g in q_phase3)
				{
					if (token.IsCancellationRequested) break;
					var collecting = ItemsCollecting;
					if (collecting != null)
						collecting("[3] " + g.Key);
					//
					var q = g
						.Where(i => File.Exists(i.FilePath))
						.Where(i => (i.LastWrite == DateTime.MinValue.Ticks) || (i.LastWrite < File.GetLastWriteTimeUtc(i.FilePath).Ticks))
						.Select(i => new { Item = i, Tag = MediaTagService.Get(i.FilePath), })
						.Where(i => i.Tag.IsTagLoaded)
						.Select(i =>
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
						}).ToArray();
					var result = mediaDB.Update(q,
						_ => _.FilePath,
						_ => _.Title, _ => _.Artist, _ => _.Album, _ => _.Comment, _ => _.SearchHint,
						_ => _.CreatedDate, _ => _.LastUpdate, _ => _.LastWrite, _ => _.IsNotExist);
					OnMediaChanged(new MediaItemChangedEventArgs(MediaItemChangedType.Update, g.Key, result));
				}
			}
			RaiseDBCommit();
			sw_phase.Stop();
			LogService.AddDebugLog(" - CollectItems phase3(tag collect) finish. >>>" + sw_phase.ElapsedMilliseconds + "ms.");

			// phase.4: append check...
			// FileSystemWatcherがあまりに挙動不審なので、最後の通知から2秒後にフルスキャンを行う
			Observable.Start(() =>
			{
				IDisposable rescan_timer = null;
				while (token.IsCancellationRequested == false)
				{
					var ret = fsw.WaitForChanged(WatcherChangeTypes.All, 1000);
					if (ret.TimedOut) continue;
					//
					LogService.AddDebugLog("FSW-Notify, Rescan waiting...");
					if (rescan_timer != null) rescan_timer.Dispose();
					rescan_timer = Observable.Timer(TimeSpan.FromSeconds(2))
						.Subscribe(_ =>
						{
							// rescan
							LogService.AddDebugLog("FSW-Notify, RaiseRescan.");
							this.ReloadItems(true, false, null, null);
						});
				}
			});
		}

		#endregion

		#region Definition

		public enum MediaItemChangedType { Add, Remove, Update, }

		public class MediaItemChangedEventArgs : EventArgs
		{
			public MediaItemChangedType ChangedType { get; set; }
			public string DirectoryName { get; private set; }
			public MediaItem[] Items { get; private set; }

			public MediaItemChangedEventArgs(MediaItemChangedType type, string dirName, MediaItem[] items)
			{
				this.ChangedType = type;
				this.DirectoryName = dirName;
				this.Items = items;
			}
		}

		#endregion
	
	}
}

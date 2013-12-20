using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Quala;

namespace SmartAudioPlayerFx.Data
{
	// 特定パスを基準にItemCollectionServiceの内容をフィルタリングする
	//  - 基本的にItemCollectionServiceの内容を追随する
	//  - ItemCollectionService.FocusPathChangedと同期して基準パスを変更する
	sealed class MediaItemsViewFocus
	{
		MediaDBView all_items;

		public MediaItemsViewFocus(MediaDBView items)
		{
			if (items == null)
				throw new ArgumentNullException("items");
			all_items = items;
			all_items.FocusPathChanged += () => SetViewFocusPathInternal(all_items.FocusPath, false);
			all_items.ItemChanged += e => OnMediaChanged(e);
			all_items.ItemsPropertyChanged += () => ResetViewItems();
			SetViewFocusPath(null);
		}

		// 再生・選択表示対象フォルダの設定
		// DB項目を絞り込んで、これをベースに操作する
		public event Action ViewFocusPathChanged;
		public string ViewFocusPath { get; private set; }
		// パスの設定。無効なパスの場合は ItemsCollectionService.FocusPath が設定される
		public void SetViewFocusPath(string path)
		{
			SetViewFocusPathInternal(path, true);
		}
		void SetViewFocusPathInternal(string path, bool reloadItems)
		{
			LogService.AddDebugLog("Call SetViewFocusPathInternal: path={0}, reloadItems={1}", path, reloadItems);
			ViewFocusPath =
				(string.IsNullOrEmpty(path) || path.StartsWith(all_items.FocusPath) == false) ?
				all_items.FocusPath :
				path;
			if (ViewFocusPathChanged != null)
				ViewFocusPathChanged();
			if(reloadItems)
				ResetViewItems();
		}

		// ViewItemsプロパティ自体が更新された(reset/item==0)
		public event Action ViewItemsChanged;
		// アイテムのリセット処理が完了した(reseted/item!=0)
		public event Action ViewItemsResetted;
		// ViewItemsにアイテムが追加/削除/更新された
		public event Action<MediaDBView.MediaItemChangedEventArgs> ViewItemChanged;
		// キャッシュ済みアイテム<ID, Item>
		public ConcurrentDictionary<long, MediaItem> ViewItems { get; private set; }

		CancellationTokenSource ResetViewItems_CTS = null;
		// アイテム情報をリセットして、ItemsCollectionServiceから再読み込み
		void ResetViewItems()
		{
			LogService.AddDebugLog("Call ResetViewItems");

			// 以前の検索をキャンセル
			if (ResetViewItems_CTS != null)
			{
				ResetViewItems_CTS.Cancel();
				ResetViewItems_CTS.Dispose();
			}
			ResetViewItems_CTS = new CancellationTokenSource();

			// リセット
			ViewItems = new ConcurrentDictionary<long, MediaItem>();
			if (ViewItemsChanged != null)
				ViewItemsChanged();

			// パスが空なら何もしない
			if (string.IsNullOrEmpty(ViewFocusPath))
				return;

			// OnMediaChangedに丸投げ
			var sw = Stopwatch.StartNew();
			var items = all_items.Items;
			if (items != null && items.Count > 0)
			{
				items.Values
					.TakeWithSplit(2000, ResetViewItems_CTS.Token)
					.Run(its =>
					{
						its
							.GroupBy(i => i.GetFilePathDir())
							.Run(g => OnMediaChanged(new MediaDBView.MediaItemChangedEventArgs(MediaDBView.MediaItemChangedType.Add, g.Key, g.ToArray())));
					});
			}
			sw.Stop();
			LogService.AddDebugLog(" **ResetViewItems: {0}ms", sw.ElapsedMilliseconds);
			if (ViewItemsResetted != null)
				ViewItemsResetted();
		}

		void OnMediaChanged(MediaDBView.MediaItemChangedEventArgs e)
		{
			var list = ViewItems;
			if (list == null) return;

			// ID==0とValidateItemのチェックはMediaItemCollectionで済んでるのでやらない
			var view_focus_path = ViewFocusPath;
			if (string.IsNullOrWhiteSpace(view_focus_path) ||
				e.DirectoryName.StartsWith(view_focus_path) == false ||
				e.Items.Any() == false)
			{
				return;
			}
			//
			var itemchanged = ViewItemChanged;
			if (e.ChangedType == MediaDBView.MediaItemChangedType.Add)
			{
				// Add: アイテムがリスト内に存在しないので追加
				var items2 =
					(from i in e.Items
					 where list.TryAdd(i.ID, i)
					 select i).ToArray();
				if (items2.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaDBView.MediaItemChangedEventArgs(MediaDBView.MediaItemChangedType.Add, e.DirectoryName, items2)); }
					catch { }
				}
			}
			else if (e.ChangedType == MediaDBView.MediaItemChangedType.Remove)
			{
				// Remove: ファイルが削除されたのでリスト内から削除
				MediaItem tmp;
				var items2 =
					(from i in e.Items
					 where list.TryRemove(i.ID, out tmp)
					 select i).ToArray();
				if (items2.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaDBView.MediaItemChangedEventArgs(MediaDBView.MediaItemChangedType.Remove, e.DirectoryName, items2)); }
					catch { }
				}
			}
			else if (e.ChangedType == MediaDBView.MediaItemChangedType.Update)
			{
				// TODO: (IsNotExistが更新された場合の処理)はもしかしたら要らないのかもしれない...
				// アイテム情報がないなら追加 (IsNotExistが更新された場合の処理)
				var items0 =
					(from i in e.Items
					 where i.IsNotExist == false
					 where list.ContainsKey(i.ID) == false
					 select i).ToArray();
				if (items0.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaDBView.MediaItemChangedEventArgs(MediaDBView.MediaItemChangedType.Add, e.DirectoryName, items0)); }
					catch { }
				}
				// Update: アイテム情報が更新されたけどインスタンス一緒だから何もしなくていいはず？
				var items2 =
					(from i in e.Items
					 where list.ContainsKey(i.ID)
					 select i).ToArray();
				if (items2.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaDBView.MediaItemChangedEventArgs(MediaDBView.MediaItemChangedType.Update, e.DirectoryName, items2)); }
					catch { }
				}
				// ファイルが存在しないならリストから削除 (IsNotExistが更新された場合の処理)
				MediaItem tmp;
				var items3 = items2
					.Where(i => i.IsNotExist)
					.Where(i => list.TryRemove(i.ID, out tmp))
					.ToArray();
				if (items3.Any() && itemchanged != null)
				{
					try { itemchanged(new MediaDBView.MediaItemChangedEventArgs(MediaDBView.MediaItemChangedType.Remove, e.DirectoryName, items3)); }
					catch { }
				}
			}
		}

	}
}

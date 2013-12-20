using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Quala;

namespace SmartAudioPlayerFx.Player
{
	// 特定パスを基準にItemCollectionServiceの内容をフィルタリングする
	//  - 基本的にItemCollectionServiceの内容を追随する
	//  - ItemCollectionService.FocusPathChangedと同期して基準パスを変更する
	sealed class MediaItemsCollectionViewFocus
	{
		MediaItemsCollection all_items;

		public MediaItemsCollectionViewFocus(MediaItemsCollection items)
		{
			if (items == null)
				throw new ArgumentNullException("items");
			all_items = items;
			all_items.FocusPathChanged += () => SetViewFocusPath(all_items.FocusPath);
			all_items.ItemsAdded += e => OnMediaChanged(e.IsFullInfo, e.DirectoryPath, e.Items);
			all_items.ItemsRemoved += e => OnMediaChanged(e.IsFullInfo, e.DirectoryPath, e.Items, forceOnRemove: true);
			all_items.ItemsUpdated += e => OnMediaChanged(e.IsFullInfo, e.DirectoryPath, e.Items);
			all_items.ItemsChanged += () => ResetViewItems();
		}

		// 再生・選択表示対象フォルダの設定
		// DB項目を絞り込んで、これをベースに操作する
		public event Action ViewFocusPathChanged;
		public string ViewFocusPath { get; private set; }
		// パスの設定。無効なパスの場合は ItemsCollectionService.FocusPath が設定される
		public void SetViewFocusPath(string path)
		{
			LogService.AddDebugLog("MediaItemsCollectionViewFocus", "Call SetViewFocusPath: path={0}", path);
			ViewFocusPath =
				(string.IsNullOrEmpty(path) || path.StartsWith(all_items.FocusPath) == false) ?
				all_items.FocusPath :
				path;
			if (ViewFocusPathChanged != null)
				ViewFocusPathChanged();
			ResetViewItems();
		}

		// ViewItemsプロパティ自体が更新された(reset/item==0)
		public event Action ViewItemsChanged;
		// アイテムのリセット処理が完了した(reseted/item!=0)
		public event Action ViewItemsResetted;
		// ViewItemsにアイテムが追加/削除/更新された
		public event Action<MediaDBService.MediaItemChangedEventArgs> ViewItemsAdded;
		public event Action<MediaDBService.MediaItemChangedEventArgs> ViewItemsRemoved;
		public event Action<MediaDBService.MediaItemChangedEventArgs> ViewItemsUpdated;
		// キャッシュ済みアイテム<ID, Item>
		public ConcurrentDictionary<long, MediaItem> ViewItems { get; private set; }

		CancellationTokenSource ResetViewItems_CTS = null;
		// アイテム情報をリセットして、ItemsCollectionServiceから再読み込み
		void ResetViewItems()
		{
			LogService.AddDebugLog("MediaItemsCollectionViewFocus", "Call ResetViewItems");
			// リセット
			ViewItems = new ConcurrentDictionary<long, MediaItem>();
			if (ViewItemsChanged != null)
				ViewItemsChanged();
			// パスが空なら何もしない
			if (string.IsNullOrEmpty(ViewFocusPath))
				return;
			// 以前の検索をキャンセル
			if (ResetViewItems_CTS != null)
				ResetViewItems_CTS.Cancel();
			ResetViewItems_CTS = new CancellationTokenSource();
			// OnMediaChangedに丸投げ
			Task.Factory.StartNew(() =>
			{
				var sw = Stopwatch.StartNew();
				all_items.Items
					.TakeWithSplit(2000, ResetViewItems_CTS.Token)
					.SelectMany(its => its.Select(i => i.Value).GroupBy(i => i.GetFilePathDir()))
					.Run(g => OnMediaChanged(true, g.Key, g.ToArray()));
				sw.Stop();
				LogService.AddDebugLog("MediaItemsCollectionViewFocus", " **ResetViewItems: {0}ms", sw.ElapsedMilliseconds);
				if (ViewItemsResetted != null)
					ViewItemsResetted();
			});
		}

		// forceOnXXXは強制的にその処理を行うモード
		void OnMediaChanged(bool is_fullinfo, string dir_path, MediaItem[] items, bool forceOnRemove = false)
		{
			var view_focus_path = ViewFocusPath;
			if (string.IsNullOrEmpty(view_focus_path) ||
				!dir_path.StartsWith(view_focus_path, StringComparison.CurrentCultureIgnoreCase))
				return;

			var added_list = new List<MediaItem>();
			var remoed_list = new List<MediaItem>();
			var updated_list = new List<MediaItem>();
			items
				.Where(item => item.ID != 0)	// e.MediaItem.ID == 0はないという仮定で
				.Run(item =>
				{
					MediaItem has_item;
					if (!ViewItems.TryGetValue(item.ID, out has_item))
					{
						// アイテムがリスト内に存在しないので追加
						ViewItems.TryAdd(item.ID, item);
						added_list.Add(item);
					}
					else if (item.IsNotExist || forceOnRemove)
					{
						// ファイルが削除されたのでリスト内から削除
						MediaItem tmp;
						ViewItems.TryRemove(item.ID, out tmp);
						remoed_list.Add(item);
					}
					else
					{
						// アイテム情報が更新されたけどインスタンス一緒だから何もしなくていいはず？
						updated_list.Add(item);
					}
				});
			if (ViewItemsAdded != null && added_list.Count != 0)
				ViewItemsAdded(new MediaDBService.MediaItemChangedEventArgs(is_fullinfo, dir_path, added_list.ToArray()));
			if (ViewItemsRemoved != null && remoed_list.Count != 0)
				ViewItemsRemoved(new MediaDBService.MediaItemChangedEventArgs(is_fullinfo, dir_path, remoed_list.ToArray()));
			if (ViewItemsUpdated != null && updated_list.Count != 0)
				ViewItemsUpdated(new MediaDBService.MediaItemChangedEventArgs(is_fullinfo, dir_path, updated_list.ToArray()));
		}

	}
}

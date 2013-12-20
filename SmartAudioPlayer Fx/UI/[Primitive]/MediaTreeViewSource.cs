using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Quala;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI
{
	/// <summary>
	/// ItemCollectionViewFocusServiceのアイテムをツリーに変換。
	/// 特殊ツリーも。
	/// </summary>
	sealed class MediaTreeViewSource
	{
		ItemsTreeViewModel media_tree_root;

		/// <summary>
		/// ツリーアイテム
		/// </summary>
		public ObservableCollection<ITreeEntry> TreeItems { get; private set; }

		public MediaTreeViewSource()
		{
			TreeItems = new ObservableCollection<ITreeEntry>();
			TreeItems.Add(new NonPlayedItemsTreeViewModel());
			TreeItems.Add(new LatestAddItemsTreeViewModel());
			TreeItems.Add(new FavoriteItemsTreeViewModel());
			ResetMediaTreeItems();

			// ViewItemsが変化したらツリーリセット
			JukeboxService.ViewFocus
				.ViewItemsChanged += () => { UIService.UIThreadInvoke(() => ResetMediaTreeItems()); };
			// ViewItems更新処理追随
			JukeboxService.ViewFocus
				.ViewItemsAdded += e => { if (media_tree_root != null) { UIService.UIThreadInvoke(() => media_tree_root.ApplyByDirectoryPath(e.DirectoryPath)); } };
			JukeboxService.ViewFocus
				.ViewItemsRemoved += e => { if (media_tree_root != null) { UIService.UIThreadInvoke(() => media_tree_root.RemoveByDirectoryPath(e.DirectoryPath)); } };
		}

		// リセット
		// ItemCollectionViewFocusService.ViewFocusPathでツリールートを作成、
		void ResetMediaTreeItems()
		{
			LogService.AddDebugLog("MediaTreeViewSourceService", "Call ResetMediaTreeItems");
			if (media_tree_root != null)
			{
				TreeItems.Remove(media_tree_root);
				media_tree_root = null;
			}
			if (string.IsNullOrWhiteSpace(JukeboxService.ViewFocus.ViewFocusPath) == false)
			{
				var sw = Stopwatch.StartNew();
				media_tree_root = new ItemsTreeViewModel(JukeboxService.ViewFocus.ViewFocusPath, 0);
				TreeItems.Add(media_tree_root);
				media_tree_root.IsExpanded.Value = true;
				media_tree_root.IsSelected.Value = true;
				JukeboxService.ViewFocus.ViewItems
					.Select(i => i.Value.GetFilePathDir())
					.Distinct(StringComparer.CurrentCultureIgnoreCase)
					.Run(i => media_tree_root.ApplyByDirectoryPath(i));
				sw.Stop();
				LogService.AddDebugLog("MediaTreeViewSourceService", " - tree creation: {0}ms", sw.ElapsedMilliseconds);
			}
		}

		/// <summary>
		/// 指定パスまでのViewModelを取得
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		public ItemsTreeViewModel[] GetTreeItems(string dir)
		{
			return (media_tree_root != null) ?
				media_tree_root.FindItemRoad(dir) : null;
		}

	}
}

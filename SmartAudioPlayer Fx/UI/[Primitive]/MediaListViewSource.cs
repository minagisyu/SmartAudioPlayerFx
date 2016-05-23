using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI
{
	/// <summary>
	/// ItemCollectionViewFocusServiceのアイテムを
	/// ListFocusによって絞り込んでViewModelに変換して保持します。
	/// 
	/// TODO:
	/// ItemCollectionViewFocusServiceではなく、
	/// MediaTreeViewSourceServiceの選択要素にあわせて表示変更する？
	/// アイテム収集にはItemCollectionViewFocusServiceが必要だけど。
	/// </summary>
	sealed class MediaListViewSource
	{
		/// <summary>
		/// ListFocusが変更された
		/// </summary>
		public event Action ListFocusChanged;
		/// <summary>
		/// ItemCollectionViewFocusServiceのアイテムを絞り込むコンディションクラスインスタンス
		/// </summary>
		public ListFocusCondition ListFocus { get; private set; }
		public void SetListFocus(ListFocusCondition condition)
		{
			ListFocus = condition;
			if (ListFocusChanged != null)
				ListFocusChanged();
			if (condition != null)
				condition.ResetListItems();
			if (ListItemsChanged != null)
				ListItemsChanged();
		}

		/// <summary>
		/// ListItemsが変更された
		/// </summary>
		public event Action ListItemsChanged;
		/// <summary>
		/// 現在のListFocus条件に合致するアイテム群
		/// </summary>
		public ObservableCollection<IListEntry> ListItems
		{
			get { return (ListFocus != null) ? ListFocus.Items : null; }
		}

		public MediaListViewSource()
		{
			// ViewItemsが変化したらリストクリア (暫定処理)
			// イベントの順番によっては設定されてからnull設定になる場合がある (TODO)
			JukeboxService.ViewFocus
				.ViewItemsChanged += () => { ListFocus = null; };
			// ViewItem更新処理追随
			JukeboxService.ViewFocus
				.ViewItemsAdded += e => { if (ListFocus != null) { ListFocus.ViewItemAdded_Handle(e); } };
			JukeboxService.ViewFocus
				.ViewItemsRemoved += e => { if (ListFocus != null) { ListFocus.ViewItemRemoved_Handle(e); } };
			JukeboxService.ViewFocus
				.ViewItemsUpdated += e => { if (ListFocus != null) { ListFocus.ViewItemUpdated_Handle(e); } };
			// PlayErrorも処理 (ViewModel.HasErrorを処理するため)
			JukeboxService
				.PlayError += e => { if (ListFocus != null) { ListFocus.PlayError_Handle(e); } };
		}
	}

	#region ListFocusCondition

	/// <summary>
	/// リストフォーカスの基底実装
	/// </summary>
	abstract class ListFocusCondition
	{
		// 上：ディレクトリパスからMediaListDirectoryDifinitionの逆引きを高速化するキャッシュ
		// 下：アイテムIDからMediaItemViewModelの逆引きを高速化するキャシュ
		Dictionary<string, MediaListDirectoryDifinition> items_header_cache; // <Path, ViewModel>
		Dictionary<long, MediaItemViewModel> items_item_cache; // <ID, ViewModel>

		/// <summary>
		/// ルートパス
		/// </summary>
		protected string RootPath { get; private set; }

		/// <summary>
		/// 対象アイテム群
		/// </summary>
		public ObservableCollection<IListEntry> Items { get; private set; }

		public ListFocusCondition(string root_path)
		{
			this.RootPath = root_path;
		}

		/// <summary>
		/// このアイテムは条件をクリアしてますか？
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		protected abstract bool ValidateItem(MediaItem item);

		/// <summary>
		/// リセット。
		/// ItemCollectionViewFocusService.ViewItemsのアイテムを
		/// RootPathで始まるファイルパスで絞り込んでViewModelに変換
		/// </summary>
		public void ResetListItems()
		{
			if (string.IsNullOrWhiteSpace(RootPath)) return;

			// ファイルパスがRootPathから始まるもので絞り込む
			// 高速化のために、rootpath_hashが含まれているか確認
			var group = JukeboxService.ViewFocus.ViewItems
				.Select(i => i.Value)
				.Where(i => i.ContainsDirPath(RootPath))
				.Where(ValidateItem)
				.OrderBy(i => i.FilePath)
				.GroupBy(i => i.GetFilePathDir())
				.SelectMany(g =>
					Enumerable.Concat<IListEntry>(
						new[] { new MediaListDirectoryDifinition(g.Key, RootPath) },
						g.Select(i => new MediaItemViewModel(i))
					));
			this.Items = new ObservableCollection<IListEntry>(group);
			this.items_header_cache = this.Items
				.OfType<MediaListDirectoryDifinition>()
				.ToDictionary(i => i.FilePath, StringComparer.CurrentCultureIgnoreCase);
			this.items_item_cache = this.Items
				.OfType<MediaItemViewModel>()
				.ToDictionary(i => i.Item.ID);
		}

		// アイテムを追加、必要ならヘッダも追加する
		void AppendListItems(MediaDBService.MediaItemChangedEventArgs e)
		{
			// アイテムを検証
			var items = this.Items;
			var add_items = e.Items.Where(ValidateItem).ToArray();
			if (items == null || add_items.Length == 0) return;
			lock (items)
			{
				// ヘッダを探してインスタンスとインデックスを得る
				MediaListDirectoryDifinition headerItem;
				if (!items_header_cache.TryGetValue(e.DirectoryPath, out headerItem))
				{
					// ないので追加
					headerItem = new MediaListDirectoryDifinition(e.DirectoryPath, RootPath);
					var headerIndex = items.Count;
					foreach (var i in items_header_cache.Values)
					{
						// ソート
						if (i.FilePath.CompareTo(e.DirectoryPath) > 0)
						{
							headerIndex = items.IndexOf(i);
							break;
						}
					}
					items.Insert(headerIndex, headerItem);
					items_header_cache[e.DirectoryPath] = headerItem;
				}
				// ヘッダ下に項目追加
				var itemIndex = items.IndexOf(headerItem) + 1;
				add_items.Run(item =>
				{
					int i = itemIndex;
					for (; i < items.Count; i++)
					{
						var t = items[i];
						if (t is MediaListDirectoryDifinition || t.FilePath.CompareTo(item.FilePath) > 0)
							break;
					}
					var item_vm = new MediaItemViewModel(item);
					items.Insert(i, item_vm);
					items_item_cache[item_vm.Item.ID] = item_vm;
				});
			}
		}

		// アイテムを削除、必要ならヘッダも削除する
		void RemoveListItems(MediaDBService.MediaItemChangedEventArgs e)
		{
			// アイテムを検証
			var items = this.Items;
			var remove_items = e.Items.Where(ValidateItem).ToArray();
			if (items == null || remove_items.Length == 0) return;
			lock (items)
			{
				// アイテムを探す
				remove_items.Run(item =>
				{
					MediaItemViewModel vm;
					if (items_item_cache.TryGetValue(item.ID, out vm))
					{
						items.Remove(vm);	// 削除されたはず
						items_item_cache.Remove(vm.Item.ID);
					}
				});
				// ヘッダを探す
				MediaListDirectoryDifinition headerItem;
				if (!items_header_cache.TryGetValue(e.DirectoryPath, out headerItem))
					return;	// ヘッダが無いはずは無いのだが・・・

				// ヘッダ下を捜索
				var headerIndex = items.IndexOf(headerItem);
				var headerItemsIndex = headerIndex + 1;
				if (items.Count <= headerItemsIndex ||
					items[headerItemsIndex] is MediaListDirectoryDifinition)
				{
					// ヘッダが不要になったので削除する
					items.RemoveAt(headerIndex);
					items_header_cache.Remove(e.DirectoryPath);
				}
			}
		}

		// アイテムを更新
		void UpdateListItems(MediaDBService.MediaItemChangedEventArgs e)
		{
			// アイテムを検証
			var items = this.Items;
			var update_items = e.Items.Where(ValidateItem).ToArray();
			if (items == null || update_items.Length == 0) return;
			lock (items)
			{
				// アイテムを探す
				update_items.Run(item =>
				{
					MediaItemViewModel vm;
					if (items_item_cache.TryGetValue(item.ID, out vm))
						vm.Item = item;
				});
			}
		}

		public void ViewItemAdded_Handle(MediaDBService.MediaItemChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(RootPath)) return;
			if (MediaItemExtension.ContainsDirPath(e.DirectoryPath, RootPath))
				UIService.UIThreadInvoke(() => AppendListItems(e));
		}
		public void ViewItemRemoved_Handle(MediaDBService.MediaItemChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(RootPath)) return;
			if (MediaItemExtension.ContainsDirPath(e.DirectoryPath, RootPath))
				UIService.UIThreadInvoke(() => RemoveListItems(e));
		}
		public void ViewItemUpdated_Handle(MediaDBService.MediaItemChangedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(RootPath)) return;
			if (MediaItemExtension.ContainsDirPath(e.DirectoryPath, RootPath))
				UIService.UIThreadInvoke(() => UpdateListItems(e));
		}
		public void PlayError_Handle(MediaItem item)
		{
			// アイテムを検証
			var items = this.Items;
			if (items == null) return;
			lock (items)
			{
				// アイテムを探す
				MediaItemViewModel vm;
				if (items_item_cache.TryGetValue(item.ID, out vm))
					vm.HasError = (item._play_error_reason != null);
			}
		}
	}

	/// <summary>
	/// ファイルパスで絞り込み
	/// </summary>
	sealed class ListFocusCondition_Path : ListFocusCondition
	{
		public ListFocusCondition_Path(string root_path) : base(root_path) { }

		protected override bool ValidateItem(MediaItem item)
		{
			return true;
		}
	}

	/// <summary>
	/// 検索ワードによる絞り込み
	/// </summary>
	sealed class ListFocusCondition_SearchWord : ListFocusCondition
	{
		string[] _words;

		public ListFocusCondition_SearchWord(string word)
			: base(JukeboxService.AllItems.FocusPath)
		{
			this._words = (string.IsNullOrEmpty(word)) ? new string[0] :
				MediaItem.StrConv_LowerHankakuKana(word).Split(' ');
		}

		protected override bool ValidateItem(MediaItem item)
		{
			return _words.All(w => item.SearchHint.Contains(w));
		}
	}

	/// <summary>
	/// お気に入りによる絞り込み
	/// </summary>
	sealed class ListFocusCondition_FavoriteOnly : ListFocusCondition
	{
		public ListFocusCondition_FavoriteOnly()
			: base(JukeboxService.AllItems.FocusPath)
		{
		}

		protected override bool ValidateItem(MediaItem item)
		{
			return item.IsFavorite;
		}
	}

	/// <summary>
	/// 未再生による絞り込み
	/// </summary>
	sealed class ListFocusCondition_NonPlayedOnly : ListFocusCondition
	{
		public ListFocusCondition_NonPlayedOnly()
			: base(JukeboxService.AllItems.FocusPath)
		{
		}

		protected override bool ValidateItem(MediaItem item)
		{
			return item.PlayCount == 0;
		}
	}

	/// <summary>
	/// 追加日(今から14日前まで)による絞り込み
	/// </summary>
	sealed class ListFocusCondition_LatestAddOnly : ListFocusCondition
	{
		public ListFocusCondition_LatestAddOnly()
			: base(JukeboxService.AllItems.FocusPath)
		{
		}

		protected override bool ValidateItem(MediaItem item)
		{
			// とりあえず2週間
			var days = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - item.CreatedDate).Days;
			return days < 14;
		}
	}

	#endregion
}

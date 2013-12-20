using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.ViewModels
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
		public IListFocusCondition ListFocus { get; private set; }
		public void SetListFocus(IListFocusCondition condition)
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
			JukeboxService.ViewFocus.ViewItemChanged += e =>
			{
				var focus = ListFocus;
				if(focus != null)
					focus.ViewItemChanged_Handle(e);

			};
			// PlayErrorも処理 (ViewModel.HasErrorを処理するため)
			JukeboxService
				.PlayError += e => { if (ListFocus != null) { ListFocus.PlayError_Handle(e); } };
		}
	}

	#region ListFocusCondition

	interface IListFocusCondition
	{
		ObservableCollection<IListEntry> Items { get; }
		void ResetListItems();
		void ViewItemChanged_Handle(MediaDBView.MediaItemChangedEventArgs e);
		void PlayError_Handle(MediaItem item);
	}

	/// <summary>
	/// リストフォーカスの基底実装
	/// </summary>
	abstract class ListFocusCondition : IListFocusCondition
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
		void AppendListItems(string dirName, MediaItem[] items)
		{
			// アイテムを検証
			var list = this.Items;
			if (list == null) return;
			lock (list)
			{
				// ヘッダを探してインスタンスとインデックスを得る
				MediaListDirectoryDifinition headerItem;
				if (!items_header_cache.TryGetValue(dirName, out headerItem))
				{
					// ないので追加
					headerItem = new MediaListDirectoryDifinition(dirName, RootPath);
					var headerIndex = list.Count;
					foreach (var i in items_header_cache.Values)
					{
						// ソート
						if (i.FilePath.CompareTo(dirName) > 0)
						{
							headerIndex = list.IndexOf(i);
							break;
						}
					}
					list.Insert(headerIndex, headerItem);
					items_header_cache[dirName] = headerItem;
				}
				// ヘッダ下に項目追加
				items.Run(item =>
				{
					var itemIndex = list.IndexOf(headerItem) + 1;
					int i = itemIndex;
					for (; i < list.Count; i++)
					{
						var t = list[i];
						if (t is MediaListDirectoryDifinition || t.FilePath.CompareTo(item.FilePath) > 0)
							break;
					}
					var item_vm = new MediaItemViewModel(item);
					list.Insert(i, item_vm);
					items_item_cache[item_vm.Item.ID] = item_vm;
				});
			}
		}

		// アイテムを削除、必要ならヘッダも削除する
		void RemoveListItems(string dirName, MediaItem[] items)
		{
			// アイテムを検証
			var list = this.Items;
			if (list == null) return;
			lock (list)
			{
				items.Run(item =>
				{
					// アイテムを探す
					MediaItemViewModel vm;
					if (items_item_cache.TryGetValue(item.ID, out vm))
					{
						list.Remove(vm);	// 削除されたはず
						items_item_cache.Remove(vm.Item.ID);
					}
					// ヘッダを探す
					MediaListDirectoryDifinition headerItem;
					if (!items_header_cache.TryGetValue(dirName, out headerItem))
						return;	// ヘッダが無いはずは無いのだが・・・

					// ヘッダ下を捜索
					var headerIndex = list.IndexOf(headerItem);
					var headerItemsIndex = headerIndex + 1;
					if (list.Count <= headerItemsIndex ||
						list[headerItemsIndex] is MediaListDirectoryDifinition)
					{
						// ヘッダが不要になったので削除する
						list.RemoveAt(headerIndex);
						items_header_cache.Remove(dirName);
					}
				});
			}
		}

		// アイテムを更新
		void UpdateListItems(string dirName, MediaItem[] items)
		{
			// アイテムを検証
			var list = this.Items;
			if (list == null) return;
			lock (list)
			{
				items.Run(item =>
				{
					// アイテムを探す
					MediaItemViewModel vm;
					if (items_item_cache.TryGetValue(item.ID, out vm))
						vm.Item = item;
				});
			}
		}

		public void ViewItemChanged_Handle(MediaDBView.MediaItemChangedEventArgs e)
		{
			var root = RootPath;
			if (string.IsNullOrWhiteSpace(root)) return;
			if (e.DirectoryName.StartsWith(root) == false) return;
			//
			UIService.UIThreadInvoke(() =>
			{
				switch (e.ChangedType)
				{
					case MediaDBView.MediaItemChangedType.Add:
						AppendListItems(e.DirectoryName, e.Items);
						break;
					case MediaDBView.MediaItemChangedType.Remove:
						RemoveListItems(e.DirectoryName, e.Items);
						break;
					case MediaDBView.MediaItemChangedType.Update:
						UpdateListItems(e.DirectoryName, e.Items);
						break;
				}
			});
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

	/// <summary>
	/// 追加日が新しい物TOP100。リスト追加による自動更新なし
	/// </summary>
	sealed class ListFocusCondition_LatestAdd100 : IListFocusCondition
	{
		// 上：ディレクトリパスからMediaListDirectoryDifinitionの逆引きを高速化するキャッシュ
		// 下：アイテムIDからMediaItemViewModelの逆引きを高速化するキャシュ
		Dictionary<string, MediaListDirectoryDifinition> items_header_cache; // <Path, ViewModel>
		Dictionary<long, MediaItemViewModel> items_item_cache; // <ID, ViewModel>

		/// <summary>
		/// ルートパス
		/// </summary>
		string RootPath { get; set; }

		/// <summary>
		/// 対象アイテム群
		/// </summary>
		public ObservableCollection<IListEntry> Items { get; private set; }

		public ListFocusCondition_LatestAdd100()
		{
			this.RootPath = JukeboxService.AllItems.FocusPath;
		}

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
				.OrderByDescending(i => i.CreatedDate)
				.Take(100)
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

		public void ViewItemChanged_Handle(MediaDBView.MediaItemChangedEventArgs e)
		{
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

	#endregion
}

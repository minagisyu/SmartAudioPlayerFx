using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Quala.Windows.Mvvm;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI
{
	// ツリーエントリー要素
	interface ITreeEntry
	{
		// ツリーの名前
		string Name { get; }
		// ツリー開いてる？
		ViewModelProperty<bool> IsExpanded { get; }
		// ツリー選択されてる？
		ViewModelProperty<bool> IsSelected { get; }
		// 子要素
		ObservableCollection<ITreeEntry> SubItems { get; }
		// ツリーの階層
		int Depth { get; }
		// このツリー項目用のListFocusCondition
		ListFocusCondition CreateListFocusCondition();
	}

	// 特殊検索条件ツリー用のマーキング用
	interface ISpecialTreeEntry
	{
	}

	// お気に入りツリー
	[DebuggerDisplay("{Name}")]
	sealed class FavoriteItemsTreeViewModel : ViewModel, ITreeEntry, ISpecialTreeEntry
	{
		public string Name { get { return "◇お気に入り"; } }
		public ViewModelProperty<bool> IsExpanded { get { return null; } }
		public ViewModelProperty<bool> IsSelected { get { return null; } }
		public ObservableCollection<ITreeEntry> SubItems { get { return null; } }
		public int Depth { get { return 0; } }
		public ListFocusCondition CreateListFocusCondition()
		{
			return new ListFocusCondition_FavoriteOnly();
		}
	}

	// 未再生ツリー
	[DebuggerDisplay("{Name}")]
	sealed class NonPlayedItemsTreeViewModel : ViewModel, ITreeEntry, ISpecialTreeEntry
	{
		public string Name { get { return "◇未再生"; } }
		public ViewModelProperty<bool> IsExpanded { get { return null; } }
		public ViewModelProperty<bool> IsSelected { get { return null; } }
		public ObservableCollection<ITreeEntry> SubItems { get { return null; } }
		public int Depth { get { return 0; } }
		public ListFocusCondition CreateListFocusCondition()
		{
			return new ListFocusCondition_NonPlayedOnly();
		}
	}

	// 最近追加ツリー
	[DebuggerDisplay("{Name}")]
	sealed class LatestAddItemsTreeViewModel : ViewModel, ITreeEntry, ISpecialTreeEntry
	{
		public string Name { get { return "◇最近追加"; } }
		public ViewModelProperty<bool> IsExpanded { get { return null; } }
		public ViewModelProperty<bool> IsSelected { get { return null; } }
		public ObservableCollection<ITreeEntry> SubItems { get { return null; } }
		public int Depth { get { return 0; } }
		public ListFocusCondition CreateListFocusCondition()
		{
			return new ListFocusCondition_LatestAddOnly();
		}
	}

	// 通常ツリー
	[DebuggerDisplay("{Name}")]
	sealed class ItemsTreeViewModel : ViewModel, ITreeEntry
	{
		public ViewModelProperty<bool> IsExpanded { get; private set; }
		public ViewModelProperty<bool> IsSelected { get; private set; }
		public string Name { get; private set; }
		public int Depth { get; private set; }
		public ObservableCollection<ITreeEntry> SubItems { get; private set; }
		Dictionary<string, ItemsTreeViewModel> sub_items_cache = new Dictionary<string, ItemsTreeViewModel>(StringComparer.CurrentCultureIgnoreCase);
		public ListFocusCondition CreateListFocusCondition()
		{
			return new ListFocusCondition_Path(BasePath);
		}

		/// <summary>
		/// 基準パス。AddItem()でこの基準以下を追加する
		/// </summary>
		public string BasePath { get; private set; }

		public ItemsTreeViewModel(string base_path, int depth)
		{
			if (string.IsNullOrWhiteSpace(base_path))
				throw new ArgumentException("not accept null or whitespace", "base_path");
			IsExpanded = RegisterViewModelProperty(false);
			IsSelected = RegisterViewModelProperty(false);
			this.BasePath = base_path;
			var name = Path.GetFileName(base_path);
			this.Name = string.IsNullOrWhiteSpace(name) ? base_path : name;
			this.Depth = depth;
			SubItems = new ObservableCollection<ITreeEntry>();
		}

		/// <summary>
		/// 基準パスを素に子要素を作成＆追加
		/// </summary>
		/// <param name="path"></param>
		public void ApplyByDirectoryPath(string path)
		{
		//	LogService.AddDebugLog("ItemsTreeViewModel", "Call ApplyByDirectoryPath: path={0}", path);
			if (path.Length == BasePath.Length && path.Equals(BasePath, StringComparison.CurrentCultureIgnoreCase))
				return;	// 指定されたパスが基準パスと同一(自分自身は追加できないし)
			if (!MediaItemExtension.ContainsDirPath(path, BasePath))
				return;	// 指定されたパスが基準パスと異なる
			ApplyByDirectoryPath_Core(path);
		}
		void ApplyByDirectoryPath_Core(string path)
		{
			if (path.Length <= BasePath.Length + 1)
				return;	// 文字数が足りない？

			// BasePath\???\の???部分を取得
			var subdirIndexTmp = path.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, BasePath.Length + 1);
			var subdir = (subdirIndexTmp != -1) ? path.Substring(0, subdirIndexTmp) : path;

			ItemsTreeViewModel item;
			if (sub_items_cache.TryGetValue(subdir, out item) == false)
			{
				// 無いので追加
				item = new ItemsTreeViewModel(subdir, Depth+1);
				// 比較して適切な位置にインサートする
				var index = SubItems.Count;
				foreach (var item2 in SubItems.OfType<ItemsTreeViewModel>())
				{
					if (string.Compare(subdir, item2.BasePath, true) < 0)
					{
						index = SubItems.IndexOf(item2);
						break;
					}
				}
				SubItems.Insert(index, item);
				sub_items_cache.Add(subdir, item);
			}
			// subdirがpathとは違う場合、子アイテムに処理委託
			if (subdir.Length != path.Length)
			{
				// 処理丸投げ
				item.ApplyByDirectoryPath_Core(path);
			}
		}

		/// <summary>
		/// 基準パスを元に子要素を削除
		/// </summary>
		/// <param name="path"></param>
		public void RemoveByDirectoryPath(string path)
		{
		//	Quala.LogService.AddDebugLog("ItemsTreeViewModel", "Call RemoveByDirectoryPath: path={0}", path);
			if (path.Length == BasePath.Length && path.Equals(BasePath, StringComparison.CurrentCultureIgnoreCase))
				return;	// 指定されたパスが基準パスと同一(自分自身は追加できないし)
			if (!MediaItemExtension.ContainsDirPath(path, BasePath))
				return;	// 指定されたパスが基準パスと異なる
			RemoveByDirectoryPath_Core(path);
		}
		void RemoveByDirectoryPath_Core(string path)
		{
			if (path.Length <= BasePath.Length + 1)
				return;	// 文字数が足りない？

			// BasePath\???\の???部分を取得
			var subdirIndexTmp = path.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, BasePath.Length + 1);
			var subdir = (subdirIndexTmp != -1) ? path.Substring(0, subdirIndexTmp) : path;

			ItemsTreeViewModel item;
			if (sub_items_cache.TryGetValue(subdir, out item))
			{
				// subdirがpathとは違う場合、子アイテムに処理委託
				if (subdir.Length != path.Length)
					item.RemoveByDirectoryPath_Core(path);
				// subitemが空ならアイテム削除
				if (item.sub_items_cache.Count == 0)
				{
					SubItems.Remove(item);
					sub_items_cache.Remove(subdir);
				}
			}
		}

		/// <summary>
		/// BasePathが指定要素のアイテムを返す
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public ItemsTreeViewModel[] FindItemRoad(string path)
		{
		//	Quala.LogService.AddDebugLog("ItemsTreeViewModel", "Call FindItemRoad: path={0}", path);
			if (path.Length == BasePath.Length && path.Equals(BasePath, StringComparison.CurrentCultureIgnoreCase))
				return null;	// 指定されたパスが基準パスと同一(自分自身は追加できないし)
			if (!MediaItemExtension.ContainsDirPath(path, BasePath))
				return null;	// 指定されたパスが基準パスと異なる
			return FindItemRoad_Core(path);
		}
		ItemsTreeViewModel[] FindItemRoad_Core(string path)
		{
			if (path.Length <= BasePath.Length + 1)
				return null;	// 文字数が足りない？

			// BasePath\???\の???部分を取得
			var subdirIndexTmp = path.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, BasePath.Length + 1);
			var subdir = (subdirIndexTmp != -1) ? path.Substring(0, subdirIndexTmp) : path;

			ItemsTreeViewModel item;
			if (sub_items_cache.TryGetValue(subdir, out item) == false)
			{
				// 無い
				return null;
			}
			if(subdir.Length == path.Length)
			{
				return new[] { this, item, };
			}

			// 処理丸投げ
			var sub = item.FindItemRoad_Core(path);
			if (sub == null)
			{
				return new[] { this, item, };
			}
			return new[] { this, }.Concat(sub).ToArray();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using __Primitives__;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;

namespace SmartAudioPlayerFx.Views
{
	sealed class MediaListItemsSource : IDisposable
	{
		readonly object lockObj = new object();
		CompositeDisposable _disposables = new CompositeDisposable();
		SortedList<string, MediaItem> _items_cache;
		ManualResetEventSlim _reloadViewItems_wait;
		MediaDBViewFocus _viewFocus;
		public ObservableCollection<IListEntry> Items { get; private set; }

		public MediaListItemsSource(MediaDBViewFocus viewFocus)
		{
			if (viewFocus == null) throw new ArgumentNullException("viewFocus");

			_viewFocus = viewFocus;
			_reloadViewItems_wait = new ManualResetEventSlim(false);
			_disposables = new CompositeDisposable(_viewFocus, _reloadViewItems_wait);

			// Viewの変更を処理
			viewFocus.Items
				.GetNotifyObservable()
				.Subscribe(x =>
				{
					_reloadViewItems_wait.Wait();
					App.UIThreadBeginInvoke(() =>
					{
						if (x.Item != null)
						{
							if (x.Type == VersionedCollection<MediaItem>.NotifyType.Add)
								AppendListItems(x.Item);
							else if (x.Type == VersionedCollection<MediaItem>.NotifyType.Remove)
								RemoveListItems(x.Item);
							else if (x.Type == VersionedCollection<MediaItem>.NotifyType.Update)
								UpdateListItems(x.Item);
						}
						if (x.Type == VersionedCollection<MediaItem>.NotifyType.Clear)
						{
							ClearListItems();
						}
					});
				})
				.AddTo(_disposables);

			// PlayErrorも処理 (ViewModel.HasErrorを処理するため)
			Observable.FromEvent<MediaItem>(
				v => JukeboxManager.PlayError += v,
				v => JukeboxManager.PlayError -= v)
				.Subscribe(OnPlayError)
				.AddTo(_disposables);

			// 読み込み
			ReloadListItems();
			_reloadViewItems_wait.Set();
		}
		public void Dispose()
		{
			_disposables.Dispose();
		}

		void OnPlayError(MediaItem item)
		{
			if (item == null) return;
			_reloadViewItems_wait.Wait();
			MediaListItemViewModel vm = null;
			App.UIThreadBeginInvoke(() =>
			{
				lock (_items_cache)
				{
					var index = _items_cache.IndexOfKey(item.FilePath);
					vm = (index >= 0) ?
						(Items[index] as MediaListItemViewModel) :
						null;
				}
			});
			if (vm == null) return;	// ヘッダは処理しないので

			// アイテムを更新
			vm.HasError = (string.IsNullOrWhiteSpace(item._play_error_reason) == false);
		}

		private void ReloadListItems()
		{
			Logger.AddDebugLog("Call ReloadListItems", new object[0]);
			Stopwatch sw = Stopwatch.StartNew();
			this.ClearListItems();
			Dictionary<string, MediaItem> tmp = new Dictionary<string, MediaItem>();
			foreach (MediaItem x in this._viewFocus.Items.GetLatest())
			{
				string fileName_x = PathStringComparer.GetFileName(x.FilePath);
				string cacheKey = x.FilePath.Substring(0, (x.FilePath.Length - fileName_x.Length) - 1) + @"\";
				if (!tmp.ContainsKey(cacheKey))
				{
					tmp.Add(cacheKey, null);
				}
				if (!tmp.ContainsKey(x.FilePath))
				{
					tmp.Add(x.FilePath, x);
				}
			}
			this._items_cache = new SortedList<string, MediaItem>(tmp, PathStringComparer.Default);
			this.Items = new ObservableCollection<IListEntry>(this._items_cache.Select<KeyValuePair<string, MediaItem>, IListEntry>(delegate(KeyValuePair<string, MediaItem> x)
			{
				if (x.Value == null)
				{
					return new MediaListDirectoryDifinition(x.Key.Substring(0, x.Key.Length - 1), this._viewFocus.FocusPath);
				}
				MediaListItemViewModel vm = new MediaListItemViewModel(x.Value);
				this._disposables.Add(vm);
				return vm;
			}));
			sw.Stop();
			Logger.AddDebugLog(" **ReloadListItems({0}items): {1}ms", new object[] { this._items_cache.Count, sw.ElapsedMilliseconds });
		}

		// アイテムを追加、必要ならヘッダも追加する
		void AppendListItems(MediaItem item)
		{
			lock (this.lockObj)
			{
				string dirName = item.GetFilePathDir(false);
				string cacheKey = dirName + @"\";
				if (!this._items_cache.ContainsKey(cacheKey))
				{
					this._items_cache.Add(cacheKey, null);
					int index = this._items_cache.IndexOfKey(cacheKey);
					this.Items.Insert(index, new MediaListDirectoryDifinition(dirName, this._viewFocus.FocusPath));
				}
				if (!this._items_cache.ContainsKey(item.FilePath))
				{
					this._items_cache.Add(item.FilePath, item);
					int index = this._items_cache.IndexOfKey(item.FilePath);
					MediaListItemViewModel itemVM = new MediaListItemViewModel(item);
					this.Items.Insert(index, itemVM);
					this._disposables.Add(itemVM);
				}
			}
		}

		// アイテムを削除、必要ならヘッダも削除する
		void RemoveListItems(MediaItem item)
		{
			lock (this.lockObj)
			{
				string cacheKey = item.GetFilePathDir(false) + @"\";
				int index = this._items_cache.IndexOfKey(item.FilePath);
				if (index >= 0)
				{
					this._items_cache.Remove(item.FilePath);
					IDisposable itemVM = this.Items[index] as IDisposable;
					this.Items.RemoveAt(index);
					if (itemVM != null)
					{
						this._disposables.Remove(itemVM);
						itemVM.Dispose();
					}
				}
				index = this._items_cache.IndexOfKey(cacheKey);
				if ((index >= 0) && ((this._items_cache.Count >= (index + 1)) || (this._items_cache.Values[index + 1] == null)))
				{
					this._items_cache.RemoveAt(index);
					this.Items.RemoveAt(index);
				}
			}
		}

		// アイテムを更新
		void UpdateListItems(MediaItem item)
		{
			if (item != null)
			{
				lock (this.lockObj)
				{
					int index = this._items_cache.IndexOfKey(item.FilePath);
					if (index >= 0)
					{
						MediaListItemViewModel vm = this.Items[index] as MediaListItemViewModel;
						if (vm != null)
						{
							vm.Item = item;
						}
					}
				}
			}
		}

		void ClearListItems()
		{
			lock (this.lockObj)
			{
				if (this._items_cache != null)
				{
					this._items_cache.Clear();
				}
				if (this.Items != null)
				{
					this.Items.Clear();
				}
			}
		}

		// ディレクトリパスとファイルパスが混在したものをソート
		// ディレクトリパスは"\\"で終わっている必要があります
		sealed class PathStringComparer : IComparer<string>
		{
			public static readonly PathStringComparer Default = new PathStringComparer();

			public int Compare(string x, string y)
			{
				// MEMO:
				// StrCmpLogicalW APIがどうにも不安定...
				// 同じパラメータでも場合によって結果が異なる (a == b で 1が返るとか)
				// MEMO:
				// ディレクトリ名を得るのにPath.GetDirectoryName()を使うと重い...
				var cmp = StringComparer.CurrentCultureIgnoreCase;
				var fileName_x = GetFileName(x);
				var dirName_x = x.Substring(0, x.Length - fileName_x.Length - 1);
				var fileName_y = GetFileName(y);
				var dirName_y = y.Substring(0, y.Length - fileName_y.Length - 1);

				// まずはディレクトリ名で比較
				var cp = cmp.Compare(dirName_x, dirName_y);
				if (cp != 0) return cp;

				// 同じディレクトリ名なのでファイル名で比較
				cp = cmp.Compare(fileName_x, fileName_y);
				return cp;
			}

			// Path.GetFileName() 相当
			internal static string GetFileName(string path)
			{
				var index = path.LastIndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
				return path.Substring(index + 1, path.Length - index - 1);
			}
		}
	}
}

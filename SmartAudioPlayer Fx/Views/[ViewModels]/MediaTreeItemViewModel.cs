namespace SmartAudioPlayerFx.Views
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Reactive.Disposables;
	using System.Reactive.Linq;
	using System.Threading;
	using System.Windows;
	using WinAPIs;
	using Codeplex.Reactive.Extensions;
	using SmartAudioPlayerFx.Data;
	using SmartAudioPlayerFx.Managers;
	using SmartAudioPlayer;

	// 特殊検索条件ツリー用のマーキング用
	interface ISpecialMediaTreeItem
	{
	}

	[DebuggerDisplay("{Name}")]
	abstract class MediaTreeItemViewModel : NotificationObject
	{
		public MediaTreeItemViewModel()
		{
			Name = string.Empty;
			IsExpanded = false;
			IsSelected = false;
			TreeButtonVisibility = Visibility.Collapsed;
		}

		#region Properties

		string _name;
		public virtual string Name
		{
			get { return _name; }
			protected set
			{
				if (string.Equals(_name, value)) return;
				_name = value;
				RaisePropertyChanged("Name");
			}
		}

		bool _isExpanded;
		public virtual bool IsExpanded
		{
			get { return _isExpanded; }
			set
			{
				if (_isExpanded == value) return;
				_isExpanded = value;
				RaisePropertyChanged("IsExpanded");
			}
		}

		bool _isSelected;
		public virtual bool IsSelected
		{
			get { return _isSelected; }
			set
			{
				if (_isSelected == value) return;
				_isSelected = value;
				RaisePropertyChanged("IsSelected");
				
				// TreeButtonVisibility Change
				TreeButtonVisibility = value ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		Visibility _treeButtonVisibility;
		public virtual Visibility TreeButtonVisibility
		{
			get { return _treeButtonVisibility; }
			protected set
			{
				if (_treeButtonVisibility == value) return;
				_treeButtonVisibility = value;
				RaisePropertyChanged("TreeButtonVisibility");
			}
		}

		public abstract MediaListItemsSource CreateListItemsSource();

		#endregion
		#region Commands

		DelegateCommand<string> _OpenExplorerCommand;
		public DelegateCommand<string> OpenExplorerCommand
		{
			get
			{
				if (_OpenExplorerCommand == null)
				{
					_OpenExplorerCommand = new DelegateCommand<string>(x =>
					{
						OutProcess.OpenToExplorer(x);
					});
				}
				return _OpenExplorerCommand;
			}
		}

		DelegateCommand<string> _CopyToClipBoardCommand;
		public DelegateCommand<string> CopyToClipBoardCommand
		{
			get
			{
				if (_CopyToClipBoardCommand == null)
				{
					_CopyToClipBoardCommand = new DelegateCommand<string>(x =>
					{
						Clipboard.SetText(x);
					});
				}
				return _CopyToClipBoardCommand;
			}
		}

		DelegateCommand<string> _AddToIgnoreCommand;
		public DelegateCommand<string> AddToIgnoreCommand
		{
			get
			{
				if (_AddToIgnoreCommand == null)
				{
					_AddToIgnoreCommand = new DelegateCommand<string>(x =>
					{
						var list = ManagerServices.MediaItemFilterManager.IgnoreWords.ToList();
						list.RemoveAll(i => string.Equals(i.Word, x, StringComparison.CurrentCultureIgnoreCase));
						list.Add(new MediaItemFilterManager.IgnoreWord(true, x));
						ManagerServices.MediaItemFilterManager.SetIgnoreWords(list.ToArray());
					});
				}
				return _AddToIgnoreCommand;
			}
		}

		DelegateCommand<object> _ChangeViewFocusCommand;
		public DelegateCommand<object> ChangeViewFocusCommand
		{
			get
			{
				if (_ChangeViewFocusCommand == null)
				{
					_ChangeViewFocusCommand = new DelegateCommand<object>(delegate(object x)
					{
						MediaTreeItemViewModel vm = this;
						MediaListWindowViewModel rootVM = x as MediaListWindowViewModel;
						if (rootVM != null)
						{
							MediaDBViewFocus oldViewFocus = ManagerServices.JukeboxManager.ViewFocus.Value;
							if (oldViewFocus != null)
							{
								oldViewFocus.Dispose();
								MediaDBViewFocus newVF = null;
								string newFP = null;
								if (vm is ISpecialMediaTreeItem)
								{
									newFP = ManagerServices.MediaDBViewManager.FocusPath.Value;
									if (vm is MediaTreeItem_AllItemsViewModel)
									{
										newVF = new MediaDBViewFocus(newFP);
									}
									if (vm is MediaTreeItem_FavoriteItemsViewModel)
									{
										newVF = new MediaDBViewFocus_FavoriteOnly(newFP);
									}
									else if (vm is MediaTreeItem_LatestAddItemsViewModel)
									{
										newVF = new MediaDBViewFocus_LatestAddOnly(newFP);
									}
									else if (vm is MediaTreeItem_NonPlayedItemsViewModel)
									{
										newVF = new MediaDBViewFocus_NonPlayedOnly(newFP);
									}
								}
								else if (vm is MediaTreeItem_DefaultItemsViewModel)
								{
									if (!(oldViewFocus is ISpecialMediaDBViewFocus))
									{
										newFP = string.Equals(
											oldViewFocus.FocusPath,
											((MediaTreeItem_DefaultItemsViewModel)vm).BasePath,
											StringComparison.CurrentCultureIgnoreCase)
											?
											ManagerServices.MediaDBViewManager.FocusPath.Value :
											((MediaTreeItem_DefaultItemsViewModel)vm).BasePath;
									}
									else
									{
										newFP = oldViewFocus.FocusPath;
									}
									newVF = new MediaDBViewFocus(newFP);
								}
								if (newVF != null)
								{
									ManagerServices.JukeboxManager.ViewFocus.Value = newVF;
									rootVM.FocusTreeItem(newFP);
								}
							}
						}
					});
				}
				return this._ChangeViewFocusCommand;
			}
		}

		#endregion
	}

	// 通常ツリー
	sealed class MediaTreeItem_DefaultItemsViewModel : MediaTreeItemViewModel, IDisposable
	{
		CompositeDisposable _disposables = new CompositeDisposable();

		public MediaTreeItem_DefaultItemsViewModel(string basePath, int depth)
		{
			SubItems = new ObservableCollection<MediaTreeItem_DefaultItemsViewModel>();
			BasePath = basePath;
			Depth = depth;

			if (depth == 0)
			{
				ManagerServices.JukeboxManager.ViewFocus
					.Where(x => x != null)
					.ObserveOnUIDispatcher()
					.Subscribe<MediaDBViewFocus>(x =>
					{
						BasePath = x.FocusPath;
						RaisePropertyChanged("Name");
						ResetSubItems(x.Items);
						IsSelected = false;	// MEMO: 一度falseに
						IsSelected = true;
						IsExpanded = true;
						x.Items
							.GetNotifyObservable()
							.ObserveOnUIDispatcher()
							.Subscribe(y =>
							{
								if (y.Type == VersionedCollection<MediaItem>.NotifyType.Add)
								{
									AppendByDirectoryPath(y.Item.GetFilePathDir(false));
								}
								else if (y.Type == VersionedCollection<MediaItem>.NotifyType.Remove)
								{
									RemoveByDirectoryPath(y.Item.GetFilePathDir(false));
								}
								else if (y.Type == VersionedCollection<MediaItem>.NotifyType.Clear)
								{
									Clear();
								}
							}).AddTo(this._disposables);
					}).AddTo(this._disposables);
			}
		}
		public void Dispose()
		{
			_disposables.Dispose();
		}

		#region properties & CreateListItemsSource

		string _basePath;
		public string BasePath
		{
			get { return this._basePath; }
			set
			{
				if (!string.Equals(this._basePath, value))
				{
					this._basePath = value;
					string newName = Path.GetFileName(value);
					if (string.IsNullOrWhiteSpace(newName))
					{
						newName = value;
					}
					this.Name = newName;
				}
			}
		}
		public int Depth { get; private set; }
		public override string Name
		{
			get
			{
				if (this.Depth == 0)
				{
					MediaDBViewFocus vf = ManagerServices.JukeboxManager.ViewFocus.Value;
					if (vf is ISpecialMediaDBViewFocus)
					{
						if (vf is MediaDBViewFocus_FavoriteOnly) { return "お気に入り"; }
						else if (vf is MediaDBViewFocus_LatestAddOnly) { return "最近追加"; }
						else if (vf is MediaDBViewFocus_NonPlayedOnly) { return "未再生"; }
					}
					else { return base.Name; }
				}
				return base.Name;
			}
			protected set
			{
				base.Name = value;
			}
		}

		public override MediaListItemsSource CreateListItemsSource()
		{
			if (string.IsNullOrWhiteSpace(BasePath)) return null;

			MediaDBViewFocus vf = ManagerServices.JukeboxManager.ViewFocus.Value;
			string fp = (vf is MediaDBViewFocus) ? this.BasePath : ManagerServices.MediaDBViewManager.FocusPath.Value;
			fp = this.BasePath;
			return new MediaListItemsSource(
				(vf is MediaDBViewFocus_FavoriteOnly) ? ((MediaDBViewFocus)new MediaDBViewFocus_FavoriteOnly(fp)) :
				((vf is MediaDBViewFocus_LatestAddOnly) ? ((MediaDBViewFocus)new MediaDBViewFocus_LatestAddOnly(fp)) :
				((vf is MediaDBViewFocus_NonPlayedOnly) ? ((MediaDBViewFocus)new MediaDBViewFocus_NonPlayedOnly(fp)) :
				((MediaDBViewFocus)new MediaDBViewFocus(fp)))));
		}

		public override Visibility TreeButtonVisibility
		{
			get { return base.TreeButtonVisibility; }
			protected set
			{
				// ViewMode=Default以外なら一番上以外ボタン非表示
				if (ManagerServices.JukeboxManager.ViewFocus.Value is ISpecialMediaDBViewFocus &&
					Depth != 0)
				{
					value = Visibility.Collapsed;
				}
				// ViewMode=DefaultでBasePathがFocusPathと一緒ならボタンを隠す
				// (一番上のボタンを消すが、サブ項目がフォーカス持っているときにはボタンは消えないようにする)
				if (!(ManagerServices.JukeboxManager.ViewFocus.Value is ISpecialMediaDBViewFocus) &&
					string.Equals(BasePath, ManagerServices.MediaDBViewManager.FocusPath.Value, StringComparison.CurrentCultureIgnoreCase))
				{
					value = Visibility.Collapsed;
				}
				base.TreeButtonVisibility = value;
			}
		}

		#endregion
		#region Add/Remove/Clear/Reset/Find

		int refcount = 0;	// 参照カウント: このViewModelを作成・削除しようとした分だけ増減する
		SortedList<string, MediaTreeItem_DefaultItemsViewModel> sub_items_cache = new SortedList<string, MediaTreeItem_DefaultItemsViewModel>(StringComparer.CurrentCultureIgnoreCase);
		public ObservableCollection<MediaTreeItem_DefaultItemsViewModel> SubItems { get; private set; }

		// 基準パスを素に子要素を作成＆追加
		public void AppendByDirectoryPath(string dirName)
		{
			if (string.IsNullOrWhiteSpace(BasePath))
				return;
			if (dirName.Length == BasePath.Length && dirName.Equals(BasePath, StringComparison.CurrentCultureIgnoreCase))
				return;	// 指定されたパスが基準パスと同一(自分自身は追加できないし)
			if (!MediaItemCache.ContainsDirPath(dirName, BasePath))
				return;	// 指定されたパスが基準パスと異なる
			AppendByDirectoryPath_Core(dirName);
		}
		void AppendByDirectoryPath_Core(string path)
		{
			if (path.Length <= BasePath.Length + 1)
				return;	// 文字数が足りない？

			// BasePath\???\の???部分を取得
			var subdirIndexTmp = path.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, BasePath.Length + 1);
			var subdir = (subdirIndexTmp != -1) ? path.Substring(0, subdirIndexTmp) : path;

			MediaTreeItem_DefaultItemsViewModel item;
			lock (sub_items_cache)
			{
				if (sub_items_cache.TryGetValue(subdir, out item) == false)
				{
					// 無いので追加
					item = new MediaTreeItem_DefaultItemsViewModel(subdir, Depth + 1);
					sub_items_cache.Add(subdir, item);
					var index = sub_items_cache.IndexOfKey(subdir);
					SubItems.Insert(index, item);
					_disposables.Add(item);
				}
			}
			Interlocked.Increment(ref item.refcount);

			// subdirがpathとは違う場合、子アイテムに処理委託
			if (subdir.Length != path.Length)
			{
				// 処理丸投げ
				item.AppendByDirectoryPath_Core(path);
			}
		}

		// 基準パスを元に子要素を削除
		public void RemoveByDirectoryPath(string dirName)
		{
			if (string.IsNullOrWhiteSpace(BasePath))
				return;
			if (dirName.Length == BasePath.Length && dirName.Equals(BasePath, StringComparison.CurrentCultureIgnoreCase))
				return;	// 指定されたパスが基準パスと同一(自分自身は追加できないし)
			if (!MediaItemCache.ContainsDirPath(dirName, BasePath))
				return;	// 指定されたパスが基準パスと異なる
			RemoveByDirectoryPath_Core(dirName);
		}
		void RemoveByDirectoryPath_Core(string path)
		{
			if (path.Length <= BasePath.Length + 1)
				return;	// 文字数が足りない？

			// BasePath\???\の???部分を取得
			var subdirIndexTmp = path.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, BasePath.Length + 1);
			var subdir = (subdirIndexTmp != -1) ? path.Substring(0, subdirIndexTmp) : path;

			lock (sub_items_cache)
			{
				MediaTreeItem_DefaultItemsViewModel item;
				if (sub_items_cache.TryGetValue(subdir, out item))
				{
					var refc = Interlocked.Decrement(ref item.refcount);

					// subdirがpathとは違う場合、子アイテムに処理委託
					if (subdir.Length != path.Length)
						item.RemoveByDirectoryPath_Core(path);

					// subitemが空ならアイテム削除
					// MEMO: たまにrefcが-1とかになっちゃう時がある...
					if (refc <= 0 && item.sub_items_cache.Count <= 0)
					{
						SubItems.Remove(item);
						sub_items_cache.Remove(subdir);
						item.Dispose();
						_disposables.Remove(item);
					}
				}
			}
		}

		// 
		public void Clear()
		{
			lock (sub_items_cache)
			{
				refcount = 0;
				sub_items_cache.Clear();
				SubItems.Clear();
			}
		}

		void ResetSubItems(VersionedCollection<MediaItem> items)
		{
			this.Clear();
			if (this.BasePath != null)
			{
				Stopwatch sw = Stopwatch.StartNew();
				items.GetLatest()
					.ToObservable()
					.GroupBy(x => x.GetFilePathDir(false))
					.Subscribe(g => this.AppendByDirectoryPath(g.Key));
				sw.Stop();
				Logger.AddDebugLog(" **ResetSubItems: {0}ms", new object[] { sw.ElapsedMilliseconds });
			}
		}

		// BasePathが指定要素のアイテムを返す
		public MediaTreeItem_DefaultItemsViewModel[] FindItemRoad(string path)
		{
			if (path.Length == BasePath.Length && path.Equals(BasePath, StringComparison.CurrentCultureIgnoreCase))
				return null;	// 指定されたパスが基準パスと同一(自分自身は追加できないし)
			if (!MediaItemCache.ContainsDirPath(path, BasePath))
				return null;	// 指定されたパスが基準パスと異なる
			return FindItemRoad_Core(path);
		}
		MediaTreeItem_DefaultItemsViewModel[] FindItemRoad_Core(string path)
		{
			if (string.IsNullOrWhiteSpace(BasePath)) return null;

			if (path.Length <= BasePath.Length + 1)
				return null;	// 文字数が足りない？

			// BasePath\???\の???部分を取得
			var subdirIndexTmp = path.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, BasePath.Length + 1);
			var subdir = (subdirIndexTmp != -1) ? path.Substring(0, subdirIndexTmp) : path;

			MediaTreeItem_DefaultItemsViewModel item;
			lock (sub_items_cache)
			{
				if (sub_items_cache.TryGetValue(subdir, out item) == false)
				{
					// 無い
					return null;
				}
			}
			if (subdir.Length == path.Length)
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

		#endregion
	}

	// 全部ツリー
	sealed class MediaTreeItem_AllItemsViewModel : MediaTreeItemViewModel, ISpecialMediaTreeItem
	{
		public MediaTreeItem_AllItemsViewModel()
		{
			Name = "◇すべて";
		}
		public override MediaListItemsSource CreateListItemsSource()
		{
			return new MediaListItemsSource(new MediaDBViewFocus(ManagerServices.MediaDBViewManager.FocusPath.Value));
		}
		public override Visibility TreeButtonVisibility
		{
			get { return base.TreeButtonVisibility; }
			protected set
			{
				if (ManagerServices.JukeboxManager.ViewFocus.Value == null)
				{
					value = Visibility.Collapsed;
				}
				else if (!(ManagerServices.JukeboxManager.ViewFocus.Value is ISpecialMediaDBViewFocus) &&
						ManagerServices.JukeboxManager.ViewFocus.Value.FocusPath == ManagerServices.MediaDBViewManager.FocusPath.Value)
				{
					value = Visibility.Collapsed;
				}
				base.TreeButtonVisibility = value;
			}
		}
	}

	// 未再生ツリー
	sealed class MediaTreeItem_NonPlayedItemsViewModel : MediaTreeItemViewModel, ISpecialMediaTreeItem
	{
		public MediaTreeItem_NonPlayedItemsViewModel()
		{
			Name = "◇未再生";
		}
		public override MediaListItemsSource CreateListItemsSource()
		{
			return new MediaListItemsSource(new MediaDBViewFocus_NonPlayedOnly(ManagerServices.MediaDBViewManager.FocusPath.Value));
		}
		public override Visibility TreeButtonVisibility
		{
			get { return base.TreeButtonVisibility; }
			protected set
			{
				if (ManagerServices.JukeboxManager.ViewFocus.Value is MediaDBViewFocus_NonPlayedOnly)
				{
					value = Visibility.Collapsed;
				}
				base.TreeButtonVisibility = value;
			}
		}
	}

	// 最近追加ツリー
	sealed class MediaTreeItem_LatestAddItemsViewModel : MediaTreeItemViewModel, ISpecialMediaTreeItem
	{
		public MediaTreeItem_LatestAddItemsViewModel()
		{
			Name = "◇最近追加";
		}
		public override MediaListItemsSource CreateListItemsSource()
		{
			return new MediaListItemsSource(new MediaDBViewFocus_LatestAddOnly(ManagerServices.MediaDBViewManager.FocusPath.Value));
		}
		public override Visibility TreeButtonVisibility
		{
			get { return base.TreeButtonVisibility; }
			protected set
			{
				if (ManagerServices.JukeboxManager.ViewFocus.Value is MediaDBViewFocus_LatestAddOnly)
				{
					value = Visibility.Collapsed;
				}
				base.TreeButtonVisibility = value;
			}
		}
	}

	// お気に入りツリー
	sealed class MediaTreeItem_FavoriteItemsViewModel : MediaTreeItemViewModel, ISpecialMediaTreeItem
	{
		public MediaTreeItem_FavoriteItemsViewModel()
		{
			Name = "◇お気に入り";
		}
		public override MediaListItemsSource CreateListItemsSource()
		{
			return new MediaListItemsSource(new MediaDBViewFocus_FavoriteOnly(ManagerServices.MediaDBViewManager.FocusPath.Value));
		}
		public override Visibility TreeButtonVisibility
		{
			get { return base.TreeButtonVisibility; }
			protected set
			{
				if (ManagerServices.JukeboxManager.ViewFocus.Value is MediaDBViewFocus_FavoriteOnly)
				{
					value = Visibility.Collapsed;
				}
				base.TreeButtonVisibility = value;
			}
		}
	}
}

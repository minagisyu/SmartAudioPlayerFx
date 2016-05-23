using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;
using Reactive.Bindings.Extensions;
using Quala.WPF;

namespace SmartAudioPlayerFx.Views
{
	// リスト用エントリー要素
	interface IListEntry
	{
		string FilePath { get; }
	}

	// ディレクトリ要素
	[DebuggerDisplay("[{Title}]")]
	sealed class MediaListDirectoryDifinition : IListEntry
	{
		public string FilePath { get; private set; }
		public string Title { get; private set; }

		public MediaListDirectoryDifinition(string path, string viewfocuspath)
		{
			FilePath = path;
			Title = path.Replace(viewfocuspath, string.Empty);
			if(Title.StartsWith("\\"))
				Title = Title.Remove(0, 1);
			Title = Title.Replace('\\', '/');
			if (string.IsNullOrEmpty(Title))
				Title = Path.GetFileName(path);
			if (string.IsNullOrEmpty(Title))
				Title = path;
		}

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
						App.Current.OpenToExplorer(x);
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

		#endregion
	}

	// 通常ファイル
	[DebuggerDisplay("{Title}")]
	sealed class MediaListItemViewModel : NotificationObject, IListEntry, IDisposable
	{
		static Brush[] _whatsnewBrush;
		static Brush _playingBrush_playing;
		static Brush _errorBrush;
		static MediaListItemViewModel()
		{
			_whatsnewBrush = new Brush[4];//new-old
			var color = Colors.LightYellow;
			color.A = 255;
			_whatsnewBrush[0] = new LinearGradientBrush(color, Colors.Transparent, new Point(0, 0.4), new Point(0, 1));
			_whatsnewBrush[0].Freeze();
			color.A = 195;
			_whatsnewBrush[1] = new LinearGradientBrush(color, Colors.Transparent, new Point(0, 0.4), new Point(0, 1));
			_whatsnewBrush[1].Freeze();
			color.A = 127;
			_whatsnewBrush[2] = new LinearGradientBrush(color, Colors.Transparent, new Point(0, 0.4), new Point(0, 1));
			_whatsnewBrush[2].Freeze();
			color.A = 63;
			_whatsnewBrush[3] = new LinearGradientBrush(color, Colors.Transparent, new Point(0, 0.4), new Point(0, 1));
			_whatsnewBrush[3].Freeze();
			_playingBrush_playing = new LinearGradientBrush(Colors.Transparent, Colors.LightSkyBlue, 0);
			_playingBrush_playing.Freeze();
			_errorBrush = new LinearGradientBrush(Color.FromRgb(255, 128, 128), Colors.Transparent, 0);
			_errorBrush.Freeze();
		}

		public static event Action _isTitleFromFilePathChanged;
		public static IObservable<Unit> _isTitleFromFilePathChangedAsObservable()
		{
			return Observable.FromEvent(v => _isTitleFromFilePathChanged += v, v => _isTitleFromFilePathChanged -= v);
		}
		static bool _isTitleFromFilePath = false;
		public static bool IsTitleFromFilePath
		{
			get { return _isTitleFromFilePath; }
			set
			{
				_isTitleFromFilePath = value;
				if(_isTitleFromFilePathChanged != null)
					_isTitleFromFilePathChanged();
			}
		}

		CompositeDisposable _disposables = new CompositeDisposable();
		public MediaListItemViewModel(MediaItem item)
		{
			if (item == null) throw new ArgumentNullException("item");
			_item = item;
			_isTitleFromFilePathChangedAsObservable()
				.Subscribe(_ =>
				{
					RaisePropertyChanged("Title");
					RaisePropertyChanged("CurrentItemName");
				})
				.AddTo(_disposables);
			ManagerServices.JukeboxManager.CurrentMedia.Subscribe(x =>
			{
				RefreshCurrentPlay();
			})
			.AddTo(_disposables);
		}
		public void Dispose()
		{
			_disposables.Dispose();
		}

		MediaItem _item;
		public MediaItem Item
		{
			get { return _item; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();
				_item = value;
				RaisePropertyChanged("MediaItem");
				RaisePropertyChanged("FilePath");
				RaisePropertyChanged("Title");
				RaisePropertyChanged("Artist");
				RaisePropertyChanged("Album");
				RaisePropertyChanged("Comment");
				RaisePropertyChanged("WhatsNewBrush");
				RaisePropertyChanged("PlayingBrush");
				RaisePropertyChanged("CommentOpacity");
				RaisePropertyChanged("MediaList_AlbumName");
				RaisePropertyChanged("CurrentItemName");
				RaisePropertyChanged("HasError");
			}
		}

		public string FilePath { get { return _item.FilePath; } }
		public string Title
		{
			get
			{
				return (IsTitleFromFilePath) ? Path.GetFileName(_item.FilePath) : _item.Title;
			}
		}
		public string Artist { get { return _item.Artist; } }
		public string Album { get { return _item.Album; } }
		public string Comment
		{
			get
			{
				var info = string.Format("再生:{0}  スキップ:{1}  選択:{2}",
					_item.PlayCount, _item.SkipCount, _item.SelectCount);
				if (!string.IsNullOrWhiteSpace(_item.Comment))
				{
					info += string.Format(
						"{0}" +
						"--------------------------------------------------{1}" +
						"{2}",
						Environment.NewLine, Environment.NewLine, _item.Comment);
				}
				return info;
			}
		}
		public bool IsFavorite
		{
			get { return _item.IsFavorite; }
			set
			{
				_item.IsFavorite = value;
				RaisePropertyChanged("IsFavorite");
			}
		}

		//=== for view_model (MediaListView用/あとで分離) ======
		// ファイル作成日が現在から30日以内なら新着扱い、7日単位で5段階に薄くなる。
		public Brush WhatsNewBrush
		{
			get
			{
				var days = new TimeSpan(DateTime.UtcNow.Ticks - this.Item.CreatedDate).Days;
				var level = Math.Min(Math.Max(0, (days / 7)), 4);
				return (level == 4) ? Brushes.Transparent : _whatsnewBrush[level];
			}
		}
		// MediaPlayerServiceで再生中ファイルとパスが同一？
		public Brush PlayingBrush
		{
			get
			{
				var current = ManagerServices.JukeboxManager.CurrentMedia.Value;
				var is_current = (current != null) ?
					current.FilePath.Equals(this.FilePath, StringComparison.CurrentCultureIgnoreCase) :
					false;
				return is_current ? _playingBrush_playing : Brushes.Transparent;
			}
		}
		// エラーがある？
		public Brush ErrorBrush
		{
			get { return has_error ? _errorBrush : Brushes.Transparent; }
		}

		public double CommentOpacity
		{
			get { return (string.IsNullOrEmpty(this._item.Comment)) ? 0.2 : 1.0; }
		}
		public string MediaList_AlbumName
		{
			get { return (string.IsNullOrEmpty(this._item.Album)) ? string.Empty : "[" + this._item.Album + "]"; }
		}
		public string CurrentItemName
		{
			get
			{
				var name = (IsTitleFromFilePath) ? Path.GetFileName(this._item.FilePath) : this._item.Title;
				if (string.IsNullOrEmpty(this._item.Artist) == false)
					name += " - " + this._item.Artist;
				return name;
			}
		}

		bool has_error;
		public bool HasError
		{
			get { return has_error; }
			set
			{
				has_error = value;
				RaisePropertyChanged("HasError");
				RaisePropertyChanged("ErrorBrush");
			}
		}

		void RefreshCurrentPlay()
		{
			RaisePropertyChanged("PlayingBrush");
		}


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
						App.Current.OpenToExplorer(x);
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

		DelegateCommand<string> _EditTagInfoCommand;
		public DelegateCommand<string> EditTagInfoCommand
		{
			get
			{
				if (_EditTagInfoCommand == null)
				{
					_EditTagInfoCommand = new DelegateCommand<string>(x =>
					{
						MediaTagUtil.TagEditGUI(new FileInfo(x));
					});
				}
				return _EditTagInfoCommand;
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

		DelegateCommand<bool> _ChangeFavoriteCommand;
		public DelegateCommand<bool> ChangeFavoriteCommand
		{
			get
			{
				if (_ChangeFavoriteCommand == null)
				{
					_ChangeFavoriteCommand = new DelegateCommand<bool>(x =>
					{
						// Twoway Bindingで変化するけど一応。
						this.IsFavorite = x;
						this.Item.LastUpdate = DateTime.UtcNow.Ticks;
						ManagerServices.MediaDBViewManager.RaiseDBUpdateAsync(this.Item, _ => _.IsFavorite, _ => _.LastUpdate);
					});
				}
				return _ChangeFavoriteCommand;
			}
		}

		#endregion

	}
}

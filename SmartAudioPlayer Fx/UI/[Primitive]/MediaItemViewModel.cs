using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Quala;
using Quala.Windows.Mvvm;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI
{
	// リスト用エントリー要素
	interface IListEntry
	{
		string FilePath { get; }
		void OnDoubleClicked();
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

		public void OnDoubleClicked()
		{
			// 項目がダブルクリックされたのでツリーを展開して選択
			var treeitems = UIService.MediaListWindow.ViewModel.MediaTreeSource.GetTreeItems(FilePath);
			if (treeitems == null) return;
			treeitems.Run(i => i.IsExpanded.Value = true);
			treeitems.Last().IsSelected.Value = true;
		}
	}

	// 通常ファイル
	[DebuggerDisplay("{Title}")]
	sealed class MediaItemViewModel : ViewModel, IListEntry
	{
		static Brush[] _whatsnewBrush;
		static Brush _playingBrush_playing;
		static Brush _errorBrush;
		static MediaItemViewModel()
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

		MediaItem item;
		public MediaItem Item
		{
			get { return item; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();
				item = value;
				OnPropertyChanged("MediaItem");
				OnPropertyChanged("FilePath");
				OnPropertyChanged("Title");
				OnPropertyChanged("Artist");
				OnPropertyChanged("Album");
				OnPropertyChanged("Comment");
				OnPropertyChanged("WhatsNewBrush");
				OnPropertyChanged("PlayingBrush");
				OnPropertyChanged("CommentOpacity");
				OnPropertyChanged("MediaList_AlbumName");
				OnPropertyChanged("CurrentItemName");
				OnPropertyChanged("HasError");
			}
		}
		public MediaItemViewModel(MediaItem item)
		{
			if (item == null)
				throw new ArgumentNullException("item");
			this.item = item;
		}

		public string FilePath { get { return item.FilePath; } }
		public string Title { get { return item.Title; } }
		public string Artist { get { return item.Artist; } }
		public string Album { get { return item.Album; } }
		public string Comment
		{
			get
			{
				var info = string.Format("再生:{0}  スキップ:{1}  選択:{2}",
					item.PlayCount, item.SkipCount, item.SelectCount);
				if (!string.IsNullOrWhiteSpace(item.Comment))
				{
					info += string.Format(
						"{0}" +
						"--------------------------------------------------{1}" +
						"{2}",
						Environment.NewLine, Environment.NewLine, item.Comment);
				}
				return info;
			}
		}
		public bool IsFavorite
		{
			get { return item.IsFavorite; }
			set
			{
				item.IsFavorite = value;
				OnPropertyChanged("IsFavorite");
			}
		}

		//=== for view_model (MediaListView用/あとで分離) ======
		// ファイル作成日が現在から30日以内なら新着扱い、7日単位で5段階に薄くなる。
		public Brush WhatsNewBrush
		{
			get
			{
				var days = new TimeSpan(DateTime.UtcNow.Ticks - this.Item.CreatedDate).Days;
				var level = (days / 7).Limit(0, 4);
				return (level == 4) ? Brushes.Transparent : _whatsnewBrush[level];
			}
		}
		// MediaPlayerServiceで再生中ファイルとパスが同一？
		public Brush PlayingBrush
		{
			get
			{
				var current = JukeboxService.CurrentMedia;
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
			get { return (string.IsNullOrEmpty(this.item.Comment)) ? 0.2 : 1.0; }
		}
		public string MediaList_AlbumName
		{
			get { return (string.IsNullOrEmpty(this.item.Album)) ? string.Empty : "[" + this.item.Album + "]"; }
		}
		public string CurrentItemName
		{
			get
			{
				var name = this.item.Title;
				if (string.IsNullOrEmpty(this.item.Artist) == false)
					name += " - " + this.item.Artist;
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
				OnPropertyChanged("HasError");
				OnPropertyChanged("ErrorBrush");
			}
		}

		public void RefreshCurrentPlay()
		{
			OnPropertyChanged("PlayingBrush");
		}

		public void OnDoubleClicked()
		{
			// 項目がダブルクリックされたので、選択カウントを更新してからメディアを再生
			Task.Factory.StartNew(() =>
			{
				item.SelectCount++;
				MediaDBService.Update(item, _ => _.SelectCount);
			});
			JukeboxService.SetCurrentMedia(item, true, true);
		}

	}
}

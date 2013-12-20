using System.Collections.ObjectModel;
using System.Linq;
using Quala.Windows.Mvvm;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.ViewModels
{
	sealed class MediaListViewModel : ViewModel
	{
		public MediaListViewSource MediaListSource { get; private set; }
		public MediaTreeViewSource MediaTreeSource { get; private set; }

		public MediaListViewModel()
		{
			MediaListSource = new MediaListViewSource();
			MediaTreeSource = new MediaTreeViewSource();
			CurrentMedia__ctor();
			ListItems__ctor();
		}

		#region CurrentMedia

		// 現在選択中のMediaItemViewModelを取得設定。
		// 設定するとItem.SelectCountが増加します。
		public MediaItemViewModel CurrentMedia { get; private set; }

		void CurrentMedia__ctor()
		{
			JukeboxService.ViewFocus.ViewItemChanged += e =>
			{
				if (e.ChangedType != MediaDBView.MediaItemChangedType.Update) return;
				var current = CurrentMedia;
				if (current == null) return;

				var item = e.Items
					.Where(i => i.ID == current.Item.ID)
					.FirstOrDefault();
				if (item != null)
				{
					// イベントを発生させるため、CopyTo()ではなく代入を使う
					CurrentMedia.Item = item;
				}
			};
			JukeboxService.CurrentMediaChanged += e =>
			{
				var oldMedia = FindMediaItem(e.OldMedia);
				CurrentMedia = FindMediaItem(e.NewMedia);
				OnPropertyChanged("CurrentMedia");
				// CurrentMedia変更に伴ってPlayingBrushが変更されるので。
				if (oldMedia != null)
					oldMedia.RefreshCurrentPlay();
				if (CurrentMedia != null)
					CurrentMedia.RefreshCurrentPlay();
			};
			CurrentMedia = FindMediaItem(JukeboxService.CurrentMedia);
		}

		// 指定itemをViewItemsから探して返す、なければ作る(ViewItemsに追加はしない)、nullならnullを返す
		MediaItemViewModel FindMediaItem(MediaItem item)
		{
			if (item == null)
				return null;

			var list = MediaListSource.ListItems;
			if (list == null)
				return new MediaItemViewModel(item);

			lock (list)
			{
				return list
					.OfType<MediaItemViewModel>()
					.Where(i => i.Item.ID == item.ID)
					.FirstOrDefault() ?? new MediaItemViewModel(item);
			}
		}

		#endregion
		#region ListItems

		void ListItems__ctor()
		{
			MediaListSource.ListItemsChanged +=
				() => OnPropertyChanged("ListItems");
		}

		public ObservableCollection<IListEntry> ListItems
		{
			get { return MediaListSource.ListItems; }
		}

		#endregion
		#region TreeItems

		public ObservableCollection<ITreeEntry> TreeItems
		{
			get { return MediaTreeSource.TreeItems; }
		}

		#endregion

	}
}

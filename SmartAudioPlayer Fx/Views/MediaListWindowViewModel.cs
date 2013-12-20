using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using __Primitives__;
using Codeplex.Reactive;
using Codeplex.Reactive.Extensions;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;

namespace SmartAudioPlayerFx.Views
{
	sealed class MediaListWindowViewModel
	{
		#region Properties

		// Window
		public ReactiveProperty<double> Width { get; private set; }
		public ReactiveProperty<double> Height { get; private set; }
		public ReactiveProperty<Visibility> Visibility { get; private set; }
		public ReactiveProperty<GridLength> TreeWidth { get; private set; }
		public ReactiveProperty<Brush> VideoDrawingBrush { get; private set; }

		// MiniOption
		public ReactiveProperty<bool> IsVideoDrawing { get; private set; }
		public ReactiveProperty<bool> IsEnableSoundFadeEffect { get; private set; }
		public ReactiveProperty<bool> IsTitleFromFileName { get; private set; }
		public ReactiveProperty<bool> IsAutoCloseWhenInactive { get; private set; }
		public ReactiveProperty<bool> IsAutoCloseWhenListSelected { get; private set; }

		// Common
		public ReactiveProperty<MediaItem> CurrentMedia { get; private set; }
		public ReactiveProperty<System.Windows.Visibility> StatusBarVisibility { get; private set; }
		public ReactiveProperty<string> StatusBarText { get; private set; }

		// Sub Property
		public ReactiveProperty<string> CurrentMediaName { get; private set; }

		// TreeView
		public ObservableCollection<MediaTreeItemViewModel> TreeItems { get; private set; }

		// ListBox
		public ReactiveProperty<MediaListItemsSource> ListFocus { get; private set; }
		public ReactiveProperty<ObservableCollection<IListEntry>> ListItems { get; private set; }
		public ReactiveCommand<IListEntry> ListSelectedCommand { get; private set; }

		#endregion

		public MediaListWindowViewModel()
		{
			// Window
			Width = new ReactiveProperty<double>(750);
			Height = new ReactiveProperty<double>(350);
			Visibility = new ReactiveProperty<Visibility>(System.Windows.Visibility.Hidden);
			TreeWidth = new ReactiveProperty<GridLength>(new GridLength(250));
			VideoDrawingBrush = new ReactiveProperty<Brush>(Brushes.Transparent);

			// MiniOption
			IsVideoDrawing = new ReactiveProperty<bool>(true);
			IsEnableSoundFadeEffect = new ReactiveProperty<bool>(true);
			IsTitleFromFileName = new ReactiveProperty<bool>(false);
			IsAutoCloseWhenInactive = new ReactiveProperty<bool>(false);
			IsAutoCloseWhenListSelected = new ReactiveProperty<bool>(false);

			//=[ Jukebox ViewModel (common) ]
			CurrentMedia = new ReactiveProperty<MediaItem>();
			StatusBarVisibility = new ReactiveProperty<System.Windows.Visibility>(System.Windows.Visibility.Collapsed);
			StatusBarText = new ReactiveProperty<string>();

			//=[ Sub Property ]
			CurrentMediaName = new ReactiveProperty<string>();
			TreeItems = new ObservableCollection<MediaTreeItemViewModel>();
			ListFocus = new ReactiveProperty<MediaListItemsSource>();
			ListItems = new ReactiveProperty<ObservableCollection<IListEntry>>();
			ListSelectedCommand = new ReactiveCommand<IListEntry>();

			SetupEvents();
		}
		void SetupEvents()
		{
			IsVideoDrawing
				.ObserveOnUIDispatcher()
				.Subscribe(x => ResetVideoDrawing(x));
			IsEnableSoundFadeEffect
				.Subscribe(x => AudioPlayerManager.IsEnableSoundFadeEffect = x);
			IsTitleFromFileName
				.Subscribe(x => MediaListItemViewModel.IsTitleFromFilePath = x);

			ManagerServices.JukeboxManager.CurrentMedia
				.Subscribe(x => CurrentMedia.Value = x);
			// 新しく再生 or 設定されたらビデオ描画用ブラシをリセット
			ManagerServices.AudioPlayerManager.OpenedAsObservable()
				.Select(_ => IsVideoDrawing.Value)
				.Merge(IsVideoDrawing)
				.ObserveOnUIDispatcher()
				.Subscribe(x => ResetVideoDrawing(x));
			// TODO: ↓これ要らないかも？
			ManagerServices.JukeboxManager.ViewFocus
				.Where(x => x != null)
				.Subscribe(x =>
				{
					x.Items
						.GetNotifyObservable()
						.Where(xx =>
								xx.Type == VersionedCollection<MediaItem>.NotifyType.Update &&
								CurrentMedia.Value != null &&
								xx.Item.ID == CurrentMedia.Value.ID)
						.Subscribe(xx => CurrentMedia.Value = xx.Item);
				});
			ManagerServices.MediaDBViewManager.ItemCollect_ScanFinishedAsObservable()
				.Merge(Observable.Return(Unit.Default))	// first drain
				.Subscribe(_ => StatusBarVisibility.Value = System.Windows.Visibility.Collapsed);
			ManagerServices.MediaDBViewManager.ItemsCollectingAsObservable()
				.Merge(Observable.Return(string.Empty))
				.Subscribe(x => StatusBarText.Value = "ライブラリを更新しています... " + x);
			StatusBarText
				.Skip(1)
				.Subscribe(_ => StatusBarVisibility.Value = System.Windows.Visibility.Visible);
			StatusBarVisibility.Value = System.Windows.Visibility.Collapsed;

			//=[ Sub Property ]
			CurrentMedia
				.CombineLatest(IsTitleFromFileName, (x, y) => new { CurrentMedia = x, IsTitleFromFileName = y, })
				.Select(x =>
				{
					if (x.CurrentMedia == null) return string.Empty;
					var name = x.IsTitleFromFileName ?
						Path.GetFileName(x.CurrentMedia.FilePath) :
						x.CurrentMedia.Title;
					if (string.IsNullOrWhiteSpace(x.CurrentMedia.Artist) == false)
						name += " - " + x.CurrentMedia.Artist;
					return name;
				})
				.Subscribe(x => CurrentMediaName.Value = x);
			// TreeView
			TreeItems.Add(new MediaTreeItem_AllItemsViewModel());
			TreeItems.Add(new MediaTreeItem_NonPlayedItemsViewModel());
			TreeItems.Add(new MediaTreeItem_LatestAddItemsViewModel());
			TreeItems.Add(new MediaTreeItem_FavoriteItemsViewModel());
			TreeItems.Add(new MediaTreeItem_DefaultItemsViewModel(null, 0));
			// ListBox
			ListFocus
				.Subscribe(x => ListItems.Value = (x == null) ? null : x.Items);
			ListSelectedCommand
				.Subscribe(x =>
				{
					if (x is MediaListDirectoryDifinition)
					{
						// ディレクトリ項目がダブルクリックされたのでツリーを展開して選択
						FocusTreeItem(x.FilePath);
					}
					else if (x is MediaListItemViewModel)
					{
						// ファイル項目がダブルクリックされたので、選択カウントを更新してからメディアを再生
						var item = ((MediaListItemViewModel)x).Item;
						item.SelectCount++;
						item.LastUpdate = DateTime.UtcNow.Ticks;
						ManagerServices.MediaDBViewManager.RaiseDBUpdateAsync(item, _ => _.SelectCount, _ => _.LastUpdate);
						ManagerServices.JukeboxManager.CurrentMedia.Value = item;
					}
					// リスト選択時に、許可されていればウィンドウを閉じる
					if (IsAutoCloseWhenListSelected.Value)
						Visibility.Value = System.Windows.Visibility.Hidden;
				});

			// Preferences
			ManagerServices.PreferencesManager.PlayerSettings
				.Subscribe(x => LoadPlayerPreferences(x));
			ManagerServices.PreferencesManager.WindowSettings
				.Subscribe(x => LoadWindowPrefrences(x));
			ManagerServices.PreferencesManager.SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences());
		}

		void LoadPlayerPreferences(XElement playerSettings)
		{
			playerSettings
				.GetAttributeValueEx(IsVideoDrawing, _ => _.Value, "IsVideoDrawing")
				.GetAttributeValueEx(IsEnableSoundFadeEffect, _ => _.Value, "IsEnableSoundFadeEffect");
		}
		void LoadWindowPrefrences(XElement windowSettings)
		{
			windowSettings.SubElement("MediaListWindow", false, elm =>
			{
				elm
					.GetAttributeValueEx(Width, _ => _.Value, "Width")
					.GetAttributeValueEx(Height, _ => _.Value, "Height")
					.GetAttributeValueEx(IsTitleFromFileName, _ => _.Value, "IsTitleFromFileName")
					.GetAttributeValueEx(IsAutoCloseWhenInactive, _ => _.Value, "IsAutoCloseWhenInactive")
					.GetAttributeValueEx(IsAutoCloseWhenListSelected, _ => _.Value, "IsAutoCloseWhenListSelected")
					;
				TreeWidth.Value = new GridLength(elm.GetAttributeValueEx("TreeWidth", 250));
			});
		}
		void SavePreferences()
		{
			ManagerServices.PreferencesManager.PlayerSettings.Value
				.SetAttributeValueEx("IsVideoDrawing", IsVideoDrawing.Value)
				.SetAttributeValueEx("IsEnableSoundFadeEffect", IsEnableSoundFadeEffect.Value);
			ManagerServices.PreferencesManager.WindowSettings.Value
				.SubElement("MediaListWindow", true, elm =>
				{
					elm
						.SetAttributeValueEx("Width", Width.Value)
						.SetAttributeValueEx("Height", Height.Value)
						.SetAttributeValueEx("IsTitleFromFileName", IsTitleFromFileName.Value)
						.SetAttributeValueEx("IsAutoCloseWhenInactive", IsAutoCloseWhenInactive.Value)
						.SetAttributeValueEx("IsAutoCloseWhenListSelected", IsAutoCloseWhenListSelected.Value)
						.SetAttributeValueEx("TreeWidth", TreeWidth.Value.Value)
						;
				});
		}

		void ResetVideoDrawing(bool isVideoDrawing)
		{
			// 動画描画用のブラシを準備
			// 動画表示無効なら透明なブラシで
			DrawingBrush b = null;
			if (isVideoDrawing)
			{
				b = ManagerServices.AudioPlayerManager.GetVideoBrush();
				if (b != null)
					b.Stretch = Stretch.UniformToFill;
			}
			VideoDrawingBrush.Value = (Brush)b ?? Brushes.Transparent;
		}

		// for ツリービュー
		public MediaTreeItem_DefaultItemsViewModel[] GetTreeItems(string dir)
		{
			var treeVM = TreeItems.OfType<MediaTreeItem_DefaultItemsViewModel>().FirstOrDefault();
			return (treeVM == null) ?
				null :
				treeVM.FindItemRoad(dir);
		}
		public void FocusTreeItem(string path)
		{
			if(string.IsNullOrWhiteSpace(path)) return;
			var treeitems = GetTreeItems(path);
			if (treeitems == null) return;
			treeitems.ForEach(i => i.IsExpanded = true);
			treeitems.Last().IsSelected = true;
		}

		public MediaListItemViewModel GetListItem(string dir)
		{
			var items = this.ListItems.Value;
			if (items == null)
			{
				return null;
			}
			return (from x in items.OfType<MediaListItemViewModel>()
					where string.Equals(x.FilePath, dir, StringComparison.CurrentCultureIgnoreCase)
					select x).FirstOrDefault<MediaListItemViewModel>();
		}

	}
}

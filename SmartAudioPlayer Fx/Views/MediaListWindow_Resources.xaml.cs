using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Windows;
using SmartAudioPlayerFx.ViewModels;

namespace SmartAudioPlayerFx.Views
{
	sealed partial class MediaListWindow_Resources : ResourceDictionary
	{
		void MenuItem_ListItem_Open_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if(menuitem == null) return;
			var item = menuitem.DataContext as MediaItemViewModel;
			if(item == null) return;
			// ファイルの保存場所を開く
			Process.Start("explorer.exe", "/e, /select, \"" + item.FilePath + "\"");
		}
		void MenuItem_ListItem_Copy_FilePath_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaItemViewModel;
			if (item == null) return;
			Clipboard.SetText(item.FilePath);
		}
		void MenuItem_ListItem_Copy_Title_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaItemViewModel;
			if (item == null) return;
			Clipboard.SetText(item.Title);
		}
		void MenuItem_ListItem_Copy_Artist_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaItemViewModel;
			if (item == null) return;
			Clipboard.SetText(item.Artist);
		}
		void MenuItem_ListItem_Copy_Album_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaItemViewModel;
			if (item == null) return;
			Clipboard.SetText(item.Album);
		}

		void MenuItem_ListItem_EditTag_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaItemViewModel;
			if (item == null) return;
			MediaTagService.TagEditGUI(item.FilePath);
		}
		void MenuItem_ListItem_IgnoreFile_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaItemViewModel;
			if (item == null) return;
			var list = new List<MediaItemFilter.IgnoreWord>(JukeboxService.AllItems.MediaItemFilter.IgnoreWords);
			list.RemoveAll(i => string.Equals(i.Word, item.FilePath, StringComparison.CurrentCultureIgnoreCase));
			list.Add(new MediaItemFilter.IgnoreWord(true, item.FilePath));
			JukeboxService.AllItems.MediaItemFilter.SetIgnoreWords(list.ToArray());
			Observable.Start(() => JukeboxService.AllItems.ReValidate_Items());
		}

		void MenuItem_ListDir_Open_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if(menuitem == null) return;
			var item = menuitem.DataContext as MediaListDirectoryDifinition;
			if(item == null) return;
			Process.Start("explorer.exe", "/e, " + item.FilePath);
		}
		void MenuItem_ListDir_Copy_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaListDirectoryDifinition;
			if (item == null) return;
			Clipboard.SetText(item.FilePath);
		}
		void MenuItem_ListDir_IgnoreDir_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as MediaListDirectoryDifinition;
			if (item == null) return;
			var list = new List<MediaItemFilter.IgnoreWord>(JukeboxService.AllItems.MediaItemFilter.IgnoreWords);
			list.RemoveAll(i => string.Equals(i.Word, item.FilePath, StringComparison.CurrentCultureIgnoreCase));
			list.Add(new MediaItemFilter.IgnoreWord(true, item.FilePath));
			JukeboxService.AllItems.MediaItemFilter.SetIgnoreWords(list.ToArray());
			Observable.Start(() => JukeboxService.AllItems.ReValidate_Items());
		}

		void MenuItem_TreeItem_Open_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if(menuitem == null) return;
			var item = menuitem.DataContext as ItemsTreeViewModel;
			if(item == null) return;
			Process.Start("explorer.exe", "/e, " + item.BasePath);
		}
		void MenuItem_TreeItem_Copy_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as ItemsTreeViewModel;
			if (item == null) return;
			Clipboard.SetText(item.BasePath);
		}
		void MenuItem_TreeItem_IgnoreDir_Click(object sender, RoutedEventArgs e)
		{
			var menuitem = sender as MenuItem;
			if (menuitem == null) return;
			var item = menuitem.DataContext as ItemsTreeViewModel;
			if (item == null) return;
			var list = new List<MediaItemFilter.IgnoreWord>(JukeboxService.AllItems.MediaItemFilter.IgnoreWords);
			list.RemoveAll(i => string.Equals(i.Word, item.BasePath, StringComparison.CurrentCultureIgnoreCase));
			list.Add(new MediaItemFilter.IgnoreWord(true, item.BasePath));
			JukeboxService.AllItems.MediaItemFilter.SetIgnoreWords(list.ToArray());
			Observable.Start(() => JukeboxService.AllItems.ReValidate_Items());
		}

		void TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			// RenderedSizeがDesiredSizeより大きい(領域からはみ出している)ならツールチップ有効
			var elem = sender as FrameworkElement;
			if (elem == null) return;
			ToolTipService.SetIsEnabled(elem, elem.RenderSize.Width > elem.DesiredSize.Width);
		}

		void Image_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var elem = sender as Image;
			if (elem != null)
				elem.Opacity = 1.0;
		}
		void Image_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var elem = sender as Image;
			if (elem != null)
				elem.Opacity = 0.5;
		}
		void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var elem = sender as Image;
			if (elem == null) return;
			var vm = elem.DataContext as ItemsTreeViewModel;
			if (vm == null) return;

			Observable.Start(() =>
			{
				Action resetAction = null;
				resetAction = new Action(() =>
				{
					JukeboxService.ViewFocus.ViewItemsResetted -= resetAction;
					UIService.UIThreadInvoke(() =>
					{
						var path = vm.BasePath;
						var treeitems = UIService.MediaListWindow.ViewModel.MediaTreeSource.GetTreeItems(path);
						if (treeitems == null) return;
						treeitems.Run(i => i.IsExpanded.Value = true);
						treeitems.Last().IsSelected.Value = true;
					});
				});
				var newViewFocus =
					(string.Equals(JukeboxService.ViewFocus.ViewFocusPath, vm.BasePath, StringComparison.CurrentCultureIgnoreCase)) ?
					JukeboxService.AllItems.FocusPath : vm.BasePath;
				JukeboxService.ViewFocus.ViewItemsResetted += resetAction;
				JukeboxService.ViewFocus.SetViewFocusPath(newViewFocus);
			});
		}

		void CheckBox_Click(object sender, RoutedEventArgs e)
		{
			var elem = sender as CheckBox;
			if (elem == null) return;
			var vm = elem.DataContext as MediaItemViewModel;
			if (vm == null) return;
			vm.Item.LastUpdate = DateTime.UtcNow.Ticks;
			JukeboxService.AllItems.RaiseDBUpdate(vm.Item, _ => _.IsFavorite, _ => _.LastUpdate);
		}

		void MediaItemViewModel_Style_Title_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// 自動レイアウトが上手く動かないので、favorite用checkboxの位置を手動で調整
			var textblock = sender as TextBlock;
			if (textblock == null) return;
			var parent_grid = textblock.Parent as Grid;
			if (parent_grid == null) return;
			var checkbox = parent_grid.Children.OfType<CheckBox>().FirstOrDefault();
			if (checkbox == null) return;
			checkbox.Margin = new Thickness(e.NewSize.Width + 5, 0, 0, 0);
		}

	}

	sealed class LeftMarginMultiplierConverter : IValueConverter
	{
		public double Length { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is int) ?
				new Thickness(Length * (int)value, 0, 0, 0) :
				new Thickness(0);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new System.NotImplementedException();
		}
	}

	sealed class IsElementWrappingConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var elem = value as FrameworkElement;
			if (elem == null) return false;
			return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new System.NotImplementedException();
		}
	}

	sealed class RootNodeVisiblityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var elem = value as FrameworkElement;
			if (elem == null)
				return elem.Visibility;

			if (elem.DataContext is ISpecialTreeEntry)
				return Visibility.Collapsed;
			
			var vm = elem.DataContext as ItemsTreeViewModel;
			if (vm == null)
				return elem.Visibility;

			return string.Equals(JukeboxService.AllItems.FocusPath, vm.BasePath, StringComparison.CurrentCultureIgnoreCase) ?
				Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

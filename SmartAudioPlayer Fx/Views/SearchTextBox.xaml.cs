using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartAudioPlayerFx.Views
{
	partial class SearchTextBox : UserControl
	{
		public SearchTextBox()
		{
			InitializeComponent();

			// テキストボックスのテキスト全選択処理
			// GotFocus後、PreviewMouseUpが飛んできてからSelectAll()を実行する
			var gotfocus = Observable.FromEvent<RoutedEventArgs>(searchText, "GotFocus");
			var mouseup = Observable.FromEvent<MouseButtonEventArgs>(searchText, "PreviewMouseUp");
			gotfocus
				.Zip(mouseup, (_, __) => RoutedEventArgs.Empty)
				.Subscribe(_ => searchText.SelectAll());
		}

		void UserControl_GotFocus(object sender, RoutedEventArgs e)
		{
			main_border.BorderBrush = new SolidColorBrush(Colors.Orange);
		}
		void UserControl_LostFocus(object sender, RoutedEventArgs e)
		{
			main_border.BorderBrush = new SolidColorBrush(Colors.LightGray);
		}

		public ContextMenu SearchMenu { get; set; }

		void searchMenuImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var menu = SearchMenu;
			if (menu == null) return;
			menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
			menu.PlacementTarget = searchMenuImage;
			menu.IsOpen = true;
		}

		public event TextChangedEventHandler TextChanged;
		public string Text
		{
			get { return searchText.Text; }
			set { searchText.Text = value; }
		}

		void searchText_TextChanged(object sender, TextChangedEventArgs e)
		{
			searchTextDelete.Visibility =
				string.IsNullOrEmpty(searchText.Text) ? Visibility.Collapsed :
				Visibility.Visible;
			if (TextChanged != null)
				TextChanged(this, e);
		}

		void searchTextDelete_MouseEnter(object sender, MouseEventArgs e)
		{
			searchTextDelete.Source = new BitmapImage(new Uri("/Resources/検索：削除-フォーカス.png", UriKind.Relative));
		}
		void searchTextDelete_MouseLeave(object sender, MouseEventArgs e)
		{
			searchTextDelete.Source = new BitmapImage(new Uri("/Resources/検索：削除-デフォルト.png", UriKind.Relative));
		}
		void searchTextDelete_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			searchText.Clear();
		}


	}
}

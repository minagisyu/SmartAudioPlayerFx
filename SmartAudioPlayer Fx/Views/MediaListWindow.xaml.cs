using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Quala;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.Windows;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;
using SmartAudioPlayerFx.ViewModels;

namespace SmartAudioPlayerFx.Views
{
	sealed partial class MediaListWindow : Window
	{
		public MediaListViewModel ViewModel { get { return (DataContext as MediaListViewModel); } }
		public WindowInteropHelper WindowHelper { get; private set; }

		public double TreeWidth
		{
			get { return r_mainContentTreeSplit.Width.Value; }
			set { r_mainContentTreeSplit.Width = new GridLength(value); }
		}
		public bool IsShowVideo { get { return _mi_showvideo.IsChecked; } set { _mi_showvideo.IsChecked = value; } }
		public bool IsEnableSoundFadeEffect { get { return _mi_soundfade.IsChecked; } set { _mi_soundfade.IsChecked = value; } }
		public bool IsTitleFromFileName
		{
			get { return _mi_titleWhenFilename.IsChecked; }
			set
			{
				_mi_titleWhenFilename.IsChecked = value;
				MediaItemViewModel.IsTitleFromFilePath = value;
			}
		}
		public bool IsAutoCloseWhenInactive { get { return _mi_close_inactive.IsChecked; } set { _mi_close_inactive.IsChecked = value; } }
		public bool IsAutoCloseWhenListSelected { get { return _mi_close_listSelect.IsChecked; } set { _mi_close_listSelect.IsChecked = value; } }

		#region ctor

		public MediaListWindow()
		{
			InitializeComponent();
			WindowHelper = new WindowInteropHelper(this);

			// とりあえずPlayerWindowViewModelとMediaListWindowのプロパティをここで同期 (old)
			IsShowVideo = UIService.PlayerWindow.ViewModel.IsVideoDrawing.Value;
			IsEnableSoundFadeEffect = UIService.PlayerWindow.ViewModel.IsEnableSoundFadeEffect.Value;
			AudioPlayer.IsEnableSoundFadeEffect = IsEnableSoundFadeEffect;

			// リスト変更したらリストを上へ変更
			ViewModel.MediaListSource.ListItemsChanged += () => ListBoxScrollToTop();
			// ColectItem()中に情報通知用ステータスバーを表示したり隠したりする
			JukeboxService.AllItems.ItemsCollecting += OnCollecting;
			// ステータスバーを自動的に閉じるためのタイマー
			Observable.Timer(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2))
				.ObserveOnDispatcher()
				.Subscribe(_=>
				{
					if (statusbar.IsVisible == false) return;
					if ((DateTime.Now - last_collecting_message_date).TotalSeconds > 2)
						statusbar.Visibility = System.Windows.Visibility.Collapsed;
				});
			//
			JukeboxService.AudioPlayer.Opened += () => ResetVideoDrawing();
			// デフォルト設定
			this.Width = 750;
			this.Height = 350;
			this.TreeWidth = 250;
			this.IsAutoCloseWhenInactive = false;
			this.IsAutoCloseWhenListSelected = false;
		}

		public IntPtr Handle
		{
			get
			{
				return (WindowHelper != null)
					? WindowHelper.Handle
					: IntPtr.Zero;
			}
		}

		void ResetVideoDrawing()
		{
			System.Windows.Media.DrawingBrush b = null;
			if(IsShowVideo)
			{
				b = JukeboxService.AudioPlayer.GetVideoBrush();
				if(b != null)
					b.Stretch = System.Windows.Media.Stretch.UniformToFill;
			}
			this.videoDrawing.Fill = (System.Windows.Media.Brush)b ?? System.Windows.Media.Brushes.Transparent;
		}

		DateTime last_collecting_message_date = DateTime.Now;
		void OnCollecting(string text)
		{
			// UIスレッドを止めるとDBロックが露呈するので駄目
			UIService.UIThreadInvoke(() =>
			{
				if(statusbar.IsVisible==false)
					statusbar.Visibility = System.Windows.Visibility.Visible;

				status_text.Text = "ライブラリを更新しています... " + text;
				last_collecting_message_date = DateTime.Now;
			});
		}

		void ListBoxScrollToTop()
		{
			var panel = r_listbox.FindItemsHostPanel() as VirtualizingStackPanel;
			if (panel != null)
			{
				// 1回だと失敗するときがあるので2回やってみる
				panel.SetVerticalOffset(0);
				panel.SetVerticalOffset(0);
			}
		}

		#endregion
		#region Window

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			// ツールウィンドウに設定
		//	var exstyle = API.GetWindowLong(WindowHelper.Handle, GWL.EXSTYLE);
		//	API.SetWindowLong(WindowHelper.Handle, GWL.EXSTYLE, (IntPtr)((WS_EX)exstyle | WS_EX.TOOLWINDOW));
			//
			// 非アクティブになったらウィンドウを閉じる...場合によって。
			App.Current.Deactivated += delegate
			{
				if (IsAutoCloseWhenInactive)
				{
					this.Visibility = System.Windows.Visibility.Hidden;
					UIService.PlayerWindow.ForceDeactivateAnimation();
				}
			};
			//
			if (UIService.PlayerWindow == null)
				throw new ApplicationException("initialize require \"UIService.PlayerWindow\" instance.");
			this.Owner = UIService.PlayerWindow;
			// 読み込まれたときにウィンドウ位置を調整
			this.Loaded += delegate { this.WindowRelayout(UIService.PlayerWindow); };
		}

		public void WindowRelayout(Window owner)
		{
			// リ・レイアウト
			var rc = WinForms.Screen.FromHandle(WindowHelper.Handle).WorkingArea;
			var pt = new Drawing.Point((int)owner.Left, (int)owner.Top);
			pt.X += (int)owner.RenderSize.Width;
			pt -= new Drawing.Size((int)this.RenderSize.Width, (int)this.RenderSize.Height);

			// サイズ変更グリップの有効・無効, 新しいウィンドウの位置設定
			resizeBorder.Left = resizeBorder.Top = resizeBorder.Right = resizeBorder.Bottom = null;
			if (rc.Left > pt.X)
			{
				pt.X = (int)owner.Left;
				resizeBorder.Right = 5;
			}
			else
			{
				resizeBorder.Left = 5;
			}
			if (rc.Top > pt.Y)
			{
				pt.Y = (int)(owner.Top + owner.RenderSize.Height);
				resizeBorder.Bottom = 5;
			}
			else
			{
				resizeBorder.Top = 5;
			}

			this.Left = pt.X;
			this.Top = pt.Y;
		}

		#endregion

		void CurrentMedia_Title_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// ツリーとツリーを移動、現在の曲へフォーカスを当てる
			// コンテナ初生成の時はちゃんとスクロールされないような・・・？
			if (ViewModel == null) return;
			var element = sender as FrameworkElement;
			if (element == null) return;
			var item = element.DataContext as MediaItemViewModel;
			if (item == null) return;
			var dir = Path.GetDirectoryName(item.FilePath);
			var treeitems = ViewModel.MediaTreeSource.GetTreeItems(dir);
			if (treeitems == null) return;
			treeitems.Run(i => i.IsExpanded.Value = true);
			treeitems.Last().IsSelected.Value = true;
			// 
			var listfocus = ViewModel.MediaListSource.ListFocus;
			if (listfocus == null) return;
			listfocus.Items
				.OfType<MediaItemViewModel>()
				.Where(i => i.Item.ID == ViewModel.CurrentMedia.Item.ID)
				.Take(1)
				.Run(i => r_listbox.ScrollIntoView(i));
		}

		void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left) return;
			var listbox = sender as ListBox;
			if (listbox == null) return;
			var selected_item = listbox.SelectedItem as IListEntry;
			if (selected_item == null) return;
			// ダブルクリックされた位置が本当に自分か調べる(イベントの透過を考慮する処理)
			var element = listbox.ItemContainerGenerator.ContainerFromItem(selected_item) as UIElement;
			if (element.InputHitTest(e.GetPosition(element)) == null) return; // ダブルクリックされた IInputElement がない
			//
			// 必要ならウィンドウを閉じる
			if ((selected_item is MediaItemViewModel) && IsAutoCloseWhenListSelected)
			{
				this.Visibility = System.Windows.Visibility.Hidden;
				UIService.PlayerWindow.ForceDeactivateAnimation();
			}
			// コマンド実行
			selected_item.OnDoubleClicked();
		}

		IListFocusCondition prevListFocus = null;
		void treeView_GotFocus(object sender, RoutedEventArgs e)
		{
			// フォーカスもらったらツリーの要素を表示する(ようにprevListFocusを設定)
			ViewModel.MediaListSource.SetListFocus(prevListFocus);
		}
		void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			// 選択項目が変化したら専用のListFocusConditionを生成させて設定する (prevListFocusも上書き)
			var treeview = sender as TreeView;
			if (treeview == null) return;
			var selected_item = treeview.SelectedItem as ITreeEntry;
			if (selected_item == null) return;
			prevListFocus = selected_item.CreateListFocusCondition();
			ViewModel.MediaListSource.SetListFocus(prevListFocus);
		}

		void searchText_GotFocus(object sender, RoutedEventArgs e)
		{
			// フォーカスもらったら、検索用リストに切り替える為にTextChangedを呼ぶ
			// ListFocusが設定されてるならバックアップ
			if (ViewModel.MediaListSource.ListFocus != null)
			{
				prevListFocus = ViewModel.MediaListSource.ListFocus;
			}
			searchText_TextChanged(sender, null);
		}
		void searchText_TextChanged(object sender, TextChangedEventArgs e)
		{
			// MEMO: GotFocusから呼び出される都合上、eは使わない
			// テキストが変更されたのでListConditionを変更してリストを更新
			var textbox = sender as SearchTextBox;
			if (textbox == null) return;
			var word = textbox.Text;
			if (string.IsNullOrEmpty(word))
			{
				// テキストクリアされたのでリストを元の内容に戻す
				ViewModel.MediaListSource.SetListFocus(prevListFocus);
				return;
			}
			// 検索するまでウェイトをかけてみる
			Observable.Timer(TimeSpan.FromMilliseconds(100))
				.ObserveOnDispatcher()
				.Subscribe(delegate
				{
					if (!word.Equals(textbox.Text)) return;	// 検索ワードが変更されたのでここでの処理を破棄
					ViewModel.MediaListSource.SetListFocus(new ListFocusCondition_SearchWord(word));
				});
		}

		void _mi_showvideo_Checked(object sender, RoutedEventArgs e)
		{
			ResetVideoDrawing();
		}
		void _mi_showvideo_Unchecked(object sender, RoutedEventArgs e)
		{
			ResetVideoDrawing();
		}
		void _mi_soundfade_Checked(object sender, RoutedEventArgs e)
		{
			AudioPlayer.IsEnableSoundFadeEffect = true;
		}
		void _mi_soundfade_Unchecked(object sender, RoutedEventArgs e)
		{
			AudioPlayer.IsEnableSoundFadeEffect = false;
		}
		void _mi_titleWhenFilename_Checked(object sender, RoutedEventArgs e)
		{
			MediaItemViewModel.IsTitleFromFilePath = true;
		}
		void _mi_titleWhenFilename_Unchecked(object sender, RoutedEventArgs e)
		{
			MediaItemViewModel.IsTitleFromFilePath = false;
		}


	}
}

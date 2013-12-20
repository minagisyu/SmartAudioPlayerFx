using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Quala;
using Quala.Interop.Win32;
using SmartAudioPlayerFx.Player;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx.UI.Views
{
	sealed partial class MediaListWindow : Window
	{
		public MediaListViewModel ViewModel { get; private set; }
		public WindowInteropHelper WindowHelper { get; private set; }
		public double TreeWidth
		{
			get { return r_mainContentTreeSplit.Width.Value; }
			set { r_mainContentTreeSplit.Width = new GridLength(value); }
		}

		#region ctor

		public MediaListWindow(MediaListViewModel viewModel)
		{
			InitializeComponent();
			DataContext = this.ViewModel = viewModel;
			WindowHelper = new WindowInteropHelper(this);

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
			UIService.PlayerWindow.ViewModel.IsVideoDrawing.PropertyChanged += delegate { ResetVideoDrawing(); };
			JukeboxService.CurrentMediaChanged += delegate { UIService.UIThreadInvoke(() => ResetVideoDrawing()); };
			JukeboxService.AudioPlayer.Opened += () => ResetVideoDrawing();
		}

		void ResetVideoDrawing()
		{
			System.Windows.Media.DrawingBrush b = null;
			if(UIService.PlayerWindow.ViewModel.IsVideoDrawing.Value)
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
				panel.SetVerticalOffset(0);
		}

		#endregion
		#region Window

		bool isEnableSizeGrip_Left = true;
		bool isEnableSizeGrip_Top = true;
		bool isEnableSizeGrip_Right = true;
		bool isEnableSizeGrip_Bottom = true;

		protected override void OnSourceInitialized(EventArgs e)
		{
			HwndSource.FromHwnd(WindowHelper.Handle).AddHook(WndProc);
			base.OnSourceInitialized(e);
			// ツールウィンドウに設定
			var exstyle = API.GetWindowLong(WindowHelper.Handle, GWL.EXSTYLE);
			API.SetWindowLong(WindowHelper.Handle, GWL.EXSTYLE, (IntPtr)((WS_EX)exstyle | WS_EX.TOOLWINDOW));
		}

		[DebuggerHidden]
		IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if ((WM)msg == WM.NCHITTEST)
			{
				// カーソルの位置がウィンドウのどこの部分？
				RECT rc;
				POINT pt;
				API.GetWindowRect(hWnd, out rc);
				API.GetCursorPos(out pt);

				var lr = HT.NOWHERE;
				if (pt.x >= rc.left && pt.x <= rc.left + 5)
					lr = HT.LEFT;
				else if (pt.x <= rc.right && pt.x >= rc.right - 5)
					lr = HT.RIGHT;

				var tb = HT.NOWHERE;
				if (pt.y >= rc.top && pt.y <= rc.top + 5)
					tb = HT.TOP;
				else if (pt.y <= rc.bottom && pt.y >= rc.bottom - 5)
					tb = HT.BOTTOM;

				var ret = IntPtr.Zero;
				if (lr == HT.LEFT && isEnableSizeGrip_Left)
				{
					if (tb == HT.TOP && isEnableSizeGrip_Top)
						ret = (IntPtr)HT.TOPLEFT;
					else if (tb == HT.BOTTOM && isEnableSizeGrip_Bottom)
						ret = (IntPtr)HT.BOTTOMLEFT;
					else
						ret = (IntPtr)HT.LEFT;
				}
				else if (lr == HT.RIGHT && isEnableSizeGrip_Right)
				{
					if (tb == HT.TOP && isEnableSizeGrip_Top)
						ret = (IntPtr)HT.TOPRIGHT;
					else if (tb == HT.BOTTOM && isEnableSizeGrip_Bottom)
						ret = (IntPtr)HT.BOTTOMRIGHT;
					else
						ret = (IntPtr)HT.RIGHT;
				}
				else if ((tb == HT.TOP && isEnableSizeGrip_Top) ||
					(tb == HT.BOTTOM && isEnableSizeGrip_Bottom))
				{
					ret = (IntPtr)tb;
				}
				else
				{
					ret = (IntPtr)HT.CLIENT;
				}
				handled = true;
				return ret;
			}
			return IntPtr.Zero;
		}

		public void WindowRelayout(Window owner)
		{
			// リ・レイアウト
			var rc = WinForms.Screen.FromHandle(WindowHelper.Handle).WorkingArea;
			var pt = new Drawing.Point((int)owner.Left, (int)owner.Top);
			pt.X += (int)owner.RenderSize.Width;
			pt -= new Drawing.Size((int)this.RenderSize.Width, (int)this.RenderSize.Height);

			// サイズ変更グリップの有効・無効
			isEnableSizeGrip_Left = isEnableSizeGrip_Top =
				isEnableSizeGrip_Right = isEnableSizeGrip_Bottom = false;
			if (rc.Left > pt.X)
			{
				pt.X = (int)owner.Left;
				isEnableSizeGrip_Right = true;
			}
			else
			{
				isEnableSizeGrip_Left = true;
			}
			if (rc.Top > pt.Y)
			{
				pt.Y = (int)(owner.Top + owner.RenderSize.Height);
				isEnableSizeGrip_Bottom = true;
			}
			else
			{
				isEnableSizeGrip_Top = true;
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
			// コマンド実行
			selected_item.OnDoubleClicked();
		}

		ListFocusCondition prevListFocus = null;
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
			// 検索するまで0.3秒ウェイトをかけてみる
			Observable.Timer(TimeSpan.FromMilliseconds(300))
				.ObserveOnDispatcher()
				.Subscribe(delegate
				{
					if (!word.Equals(textbox.Text)) return;	// 検索ワードが変更されたのでここでの処理を破棄
					ViewModel.MediaListSource.SetListFocus(new ListFocusCondition_SearchWord(word));
				});
		}

	}
}

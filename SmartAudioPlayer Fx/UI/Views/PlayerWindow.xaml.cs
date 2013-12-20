using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using Microsoft.Win32;
using Quala;
using Quala.Interop.Win32;
using Quala.Windows;
using Quala.Windows.Controls;
using SmartAudioPlayerFx.Player;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx.UI.Views
{
	sealed partial class PlayerWindow : Window
	{
		public PlayerServiceViewModel ViewModel { get; private set; }
		public WindowInteropHelper WindowHelper { get; private set; }

		public PlayerWindow(PlayerServiceViewModel viewModel)
		{
			InitializeComponent();
			DataContext = this.ViewModel = viewModel;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			WindowHelper = new WindowInteropHelper(this);
			HwndSource.FromHwnd(WindowHelper.Handle).AddHook(WndProc);
			var exstyle = API.GetWindowLong(WindowHelper.Handle, GWL.EXSTYLE);
			API.SetWindowLong(WindowHelper.Handle, GWL.EXSTYLE, (IntPtr)((WS_EX)exstyle | WS_EX.TOOLWINDOW | WS_EX.NOACTIVATE));
		}
		void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// スムーズなアニメーションのために、ちょっと工夫
			Opacity = 0.0;
			Dispatcher.BeginInvoke(new Action(() =>
			{
				this.BeginAnimation(Window.OpacityProperty,
					new DoubleAnimation(UIService.inactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(300))),
					() =>
					{
						this.BeginAnimation(Window.OpacityProperty,
							new DoubleAnimation(UIService.deactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(3000))));
					});
			}), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
			//
			this.Closing += (s2,e2) =>
			{
				e2.Cancel = (this.Opacity != 0.0);
				if (e2.Cancel)
				{
					this.BeginAnimation(Window.OpacityProperty,
						new DoubleAnimation(0.0, new Duration(TimeSpan.FromMilliseconds(100))),
						() =>
						{
							UIService.UIThreadBeginInvoke(
								System.Windows.Threading.DispatcherPriority.ApplicationIdle,
								() =>
								{
									this.Opacity = 0.0;
									this.Close();
								});
						});
				}
			};
			//
			// SystemEvents.DisplaySettingsChangingがアテにならないので、
			// サイズ変更後に毎回、ウィンドウ位置の確認をやる...
			//
			// WindowLocationがPrimaryScreen依存なのでセカンダリモニタで挙動が怪しい...
			//
			var winloc = new DynamicWindowBounds();
			var winloc_update = new Action(() =>
			{
				winloc = new DynamicWindowBounds(Left, Top, Width, Height);
			});
			this.LocationChanged += delegate { winloc_update(); };
			this.SizeChanged += delegate { winloc_update(); };
			SystemEvents.DisplaySettingsChanged += delegate
			{
				var rect = winloc.ToRect(true);
				this.Left = rect.X;
				this.Top = rect.Y;
				this.Width = rect.Width;
				this.Height = rect.Height;
			};
		}

		#region ウィンドウリサイズ関係

		[DebuggerHidden]
		IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if((WM)msg == WM.NCHITTEST)
			{
				// カーソルの位置がウィンドウのどこの部分？
				RECT rc;
				POINT pt;
				API.GetWindowRect(hWnd, out rc);
				API.GetCursorPos(out pt);
				// 左端=LEFT、右端=RIGHT、それ以外=CLIENT(HT_CAPTIONを返すとWPFシステムがクライアント領域を見失う)
				var ret =
					(pt.x >= rc.left && pt.x <= rc.left + 5) ? HT.LEFT :
					(pt.x <= rc.right && pt.x >= rc.right - 5) ? HT.RIGHT :
					HT.CLIENT;
				handled = true;
				return (IntPtr)ret;
			}
			return IntPtr.Zero;
		}

		#region ドラッグ移動関係

		Point? dragStartPos;

		void mainContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			dragStartPos = e.GetPosition(mainContent);
			Mouse.Capture(mainContent);
		}

		void mainContent_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			dragStartPos = null;
			Mouse.Capture(null);
		}

		void mainContent_MouseMove(object sender, MouseEventArgs e)
		{
			if(dragStartPos.HasValue)
			{
				// スクリーン座標
				var windowLocation = new Point(Left, Top);
				var contentLocation = mainContent.PointToScreen(new Point(0, 0));

				#region マウスドラッグによるWindowの移動計算
				{
					Point pos = e.GetPosition(mainContent);
					Vector moved = pos - dragStartPos.Value;
					windowLocation += moved;
					contentLocation += moved;
				}
				#endregion
				#region デスクトップ端に吸い付き
				var shiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
				if(!shiftDown)
				{
					var rc = new Rect(contentLocation, mainContent.RenderSize);

					// 左上
					var screen = WinForms.Screen.FromPoint(
						new Drawing.Point((int)rc.X, (int)rc.Y)).WorkingArea;
					if(rc.Left > screen.Left - 10 && rc.Left <= screen.Left + 10)
						windowLocation.X += (screen.Left - rc.Left);
					if(rc.Top > screen.Top - 10 && rc.Top <= screen.Top + 10)
						windowLocation.Y += (screen.Top - rc.Top);

					// 右下
					screen = WinForms.Screen.FromPoint(
						new Drawing.Point((int)rc.Right, (int)rc.Bottom)).WorkingArea;
					if(rc.Right > screen.Right - 10 && rc.Right <= screen.Right + 10)
						windowLocation.X += (screen.Right - rc.Right);
					if(rc.Bottom > screen.Bottom - 10 && rc.Bottom <= screen.Bottom + 10)
						windowLocation.Y += (screen.Bottom - rc.Bottom);
				}
				#endregion

				// [MEMO]
				// LeftやTopプロパティで直接指定するとガクガクしてしまう…。
				API.SetWindowPos(WindowHelper.Handle, IntPtr.Zero,
					(int)windowLocation.X, (int)windowLocation.Y, 0, 0,
					SWP.NOSIZE | SWP.NOZORDER);
			}
		}

		#endregion
		#endregion
		//
		void mainContent_MouseEnter(object sender, MouseEventArgs e)
		{
			if (UIService.MediaListWindow.IsVisible) return;
			this.BeginAnimation(Window.OpacityProperty,
				new DoubleAnimation(UIService.inactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(200))));
		}
		void mainContent_MouseLeave(object sender, MouseEventArgs e)
		{
			if (UIService.MediaListWindow.IsVisible) return;
			this.BeginAnimation(Window.OpacityProperty,
				new DoubleAnimation(UIService.deactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(200))));
		}

		// 再生モード
		void modeImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
		void modeImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var newMode =
				(JukeboxService.SelectMode == JukeboxService.SelectionMode.Filename) ?
				JukeboxService.SelectionMode.Random :
				JukeboxService.SelectionMode.Filename;
			JukeboxService.SetSelectMode(newMode);
		}
		// リピート
		void repeatImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
		void repeatImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var newRepeat = !JukeboxService.IsRepeat;
			JukeboxService.SetIsRepeat(newRepeat);
		}
		// ステート
		void stateImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
		void stateImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			JukeboxService.AudioPlayer.PlayPause();
		}
		// タイトル
		void titleText_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
		void titleText_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			JukeboxService.SelectNext(true);
		}
		void titleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}
		void titleText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var toOpen = !UIService.MediaListWindow.IsVisible;
			if (toOpen)
			{
				this.BeginAnimation(Window.OpacityProperty,
					new DoubleAnimation(UIService.inactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(300))));
				UIService.MediaListWindow.WindowRelayout(this);
				API.SetForegroundWindow(WindowHelper.Handle);
				API.SetActiveWindow(WindowHelper.Handle);
			}
			UIService.MediaListWindow.Visibility = toOpen ? Visibility.Visible : Visibility.Hidden;
		}
		// シーク
		void seekSlider_Expander_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				((sender as Expander)
					.Content as SliderEx)
					.AutoTooltipConverter = v =>
					{
						var ts = TimeSpan.FromTicks((long)v);
						var total = (JukeboxService.AudioPlayer.Duration.HasValue) ?
							JukeboxService.AudioPlayer.Duration.Value :
							TimeSpan.MinValue;
						return string.Format("{0}{1:D2}:{2:D2} / {3}{4:D2}:{5:D2}",
							ts.Hours > 0 ? ts.Hours.ToString() + ":" : string.Empty,
							ts.Minutes,
							ts.Seconds,
							total.Hours > 0 ? total.Hours.ToString() + ":" : string.Empty,
							total.Minutes,
							total.Seconds);
					};
			}
			catch (NullReferenceException) { }
		}
		void seekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				var slider = (sender as Expander).Content as Slider;
				Mouse.Capture(slider, CaptureMode.SubTree);
				// シーク選択中にINPCで変更通知を受け取らないように・・・
				slider.SetBinding(Slider.ValueProperty, new Binding()
				{
					Path = new PropertyPath("PositionTicks"),
					Mode = BindingMode.OneTime,
				});
			}
			catch (NullReferenceException) { }
		}
		void seekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				Mouse.Capture(null);
				// シーク時にフェード効果を出したいので
				// OneWayでバインディングして変化時に再バインディングする
				Action post_process = null;
				if (!JukeboxService.AudioPlayer.IsPaused)
				{
					JukeboxService.AudioPlayer.PlayPause();
					post_process = delegate { JukeboxService.AudioPlayer.PlayPause(); };
				}
				var slider = (sender as Expander).Content as Slider;
				ViewModel.PositionTicks = (long)slider.Value;
				slider.SetBinding(Slider.ValueProperty, new Binding()
				{
					Path = new PropertyPath("PositionTicks"),
					Mode = BindingMode.OneWay,
				});
				//
				if (post_process != null)
					post_process();
			}
			catch (NullReferenceException) { }
		}
		// ボリューム
		void volumeSlider_Expander_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				((sender as Expander).Content as SliderEx)
					.AutoTooltipConverter = v => string.Format("{0:F0}%", (double)v * 100.0);
			}
			catch (NullReferenceException) { }
		}
		void volumeSlider_MouseEnter(object sender, MouseEventArgs e)
		{
			var slider = sender as SliderEx;
			if (slider == null) return;

			// AutoTooltipの出現位置を調整する
			var screen = WinForms.Screen.FromHandle(WindowHelper.Handle);
			var area = screen.WorkingArea;
			var topSpace = this.Top - area.Top;
			slider.AutoToolTipPlacement =
				(topSpace > 32) ?
				System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft :
				System.Windows.Controls.Primitives.AutoToolTipPlacement.BottomRight;
		}

		// PlayerBG
		MethodInfo cmenu_executer = null;
		WinForms.ContextMenu cmenu = null;
		void PlayerBG_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// ダブルクリック以上で発動
			if (e.ClickCount >= 2)
			{
				JukeboxService.AudioPlayer.PlayPause();
				e.Handled = true;
			}
		}
		void PlayerBG_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (ViewModel == null) return;
			if (cmenu == null)
			{
				var asm = typeof(WinForms.ICommandExecutor).Assembly;
				if (asm == null) return;
				var type = asm.GetType("System.Windows.Forms.Command");
				if (type == null) return;
				var method = type.GetMethod("DispatchID");
				if (method == null) return;
				cmenu_executer = method;
				cmenu = new WinForms.ContextMenu();
			}

			cmenu.MenuItems.Clear();
			cmenu.MenuItems.AddRange(UIService.CreateWinFormsMenuItems());

			API.SetForegroundWindow(WindowHelper.Handle);
			API.SetActiveWindow(WindowHelper.Handle);
			var pos = this.PointToScreen(e.GetPosition(this));

			// 反則っぽい気がするけど...
			// リフレクションでWinFormsのコマンド実行子を強制実行
			var id = API.TrackPopupMenuEx(cmenu.Handle, 0x142, (int)pos.X, (int)pos.Y, WindowHelper.Handle, IntPtr.Zero);
			if (id != 0)
				cmenu_executer.Invoke(null, new object[] { id });
		}

	}
}

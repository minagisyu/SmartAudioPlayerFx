using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using Quala.Interop.Win32;
using Quala.Windows;
using Quala.Windows.Controls;
using SmartAudioPlayerFx.Player;
using WinForms = System.Windows.Forms;
using SmartAudioPlayerFx.ViewModels;

namespace SmartAudioPlayerFx.Views
{
	sealed partial class PlayerWindow : Window
	{
		public PlayerServiceViewModel ViewModel { get { return (DataContext as PlayerServiceViewModel); } }
		public WindowInteropHelper WindowHelper { get; private set; }

		public PlayerWindow()
		{
			InitializeComponent();
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

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			
			// HWNDの取得とウィンドウプロシージャのフック
			WindowHelper = new WindowInteropHelper(this);

			// フォントサイズの変更に対応するために、表示してから取得する
			// (デフォルトのウィンドウ位置を計算するために必要)
			UIService._DefaultDesignedHeight = this.Height = this.DesiredSize.Height;

			// PlayerWindowの移動に追随する
			var relayoutAction = new Action(() =>
			{
				if (UIService.MediaListWindow != null)
					UIService.MediaListWindow.WindowRelayout(this);
			});
			this.LocationChanged += delegate { relayoutAction(); };
			this.SizeChanged += delegate { relayoutAction(); };
		}

		void Window_MouseMove(object sender, MouseEventArgs e)
		{
			var w = UIService.MediaListWindow;
			ddBehavior.ExcludeSnapHWND = (w != null) ? w.Handle : IntPtr.Zero;
		}

		void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// スムーズなアニメーションのために、ちょっと工夫
			// メッセージ処理が全部終えてからアニメーションするために優先度をIdleまで下げる
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
			}), DispatcherPriority.ApplicationIdle);

			// ウィンドウを閉じるときにアニメーションしたいので
			// 特定条件になるまでウィンドウ閉じる処理をキャンセル
			this.Closing += (s2,e2) =>
			{
				e2.Cancel = (this.Opacity != 0.0);
				if (e2.Cancel)
				{
					this.BeginAnimation(Window.OpacityProperty,
						new DoubleAnimation(0.0, new Duration(TimeSpan.FromMilliseconds(100))),
						() =>
						{
							UIService.UIThreadBeginInvoke(DispatcherPriority.ApplicationIdle, () => this.Opacity = 0.0);
							UIService.UIThreadBeginInvoke(DispatcherPriority.ApplicationIdle, () => this.Close());
						});
				}
			};

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

		//
		void mainContent_MouseEnter(object sender, MouseEventArgs e)
		{
			if (UIService.MediaListWindow != null &&
				UIService.MediaListWindow.IsVisible)
				return;
			this.BeginAnimation(Window.OpacityProperty,
				new DoubleAnimation(UIService.inactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(200))));
		}
		void mainContent_MouseLeave(object sender, MouseEventArgs e)
		{
			if (UIService.MediaListWindow != null &&
				UIService.MediaListWindow.IsVisible)
				return;
			this.BeginAnimation(Window.OpacityProperty,
				new DoubleAnimation(UIService.deactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(200))));
		}
		public void ForceDeactivateAnimation()
		{
			if (UIService.MediaListWindow != null &&
				UIService.MediaListWindow.IsVisible) return;
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
			if(JukeboxService.AudioPlayer != null)
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
			if (UIService.MediaListWindow == null) return;
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
						var total =
							(JukeboxService.AudioPlayer != null && JukeboxService.AudioPlayer.Duration.HasValue) ?
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
				if (JukeboxService.AudioPlayer != null &&
					JukeboxService.AudioPlayer.IsPaused == false)
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
				if(JukeboxService.AudioPlayer != null)
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

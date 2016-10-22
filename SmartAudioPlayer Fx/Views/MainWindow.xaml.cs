using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;
using Reactive.Bindings.Extensions;
using Quala;
using Quala.Win32;
using Quala.WPF.Extensions;
using SmartAudioPlayerFx.MediaPlayer;
using SmartAudioPlayerFx.Notification;
using SmartAudioPlayerFx.Shortcut;

namespace SmartAudioPlayerFx.Views
{
	partial class MainWindow : Window
	{
		public MainWindowViewModel ViewModel { get; private set; }
		public MediaListWindow MediaListWindow { get; private set; }

		public MainWindow()
		{
			InitializeComponent();
			PrepareMediaListWindow();
			DataContext = ViewModel = new MainWindowViewModel();
			SetupEvents();
		}
		void PrepareMediaListWindow()
		{
			// MEMO: ウィンドウハンドル出来てからじゃないとOwnerの設定で例外吐いちゃう
			MediaListWindow = new MediaListWindow();
			MediaListWindow.SourceInitialized += (_, __) =>
			{
				wdmbehavior.ExcludeSnapHWND = new WindowInteropHelper(MediaListWindow).Handle;
				MediaListWindow.Owner = this;
				MediaListWindow.WindowRelayout();
			};
		}
		void SetupEvents()
		{
			//=[ Startup, Shutdown ]
			SourceInitialized += (_, __) =>
			{
				LayoutRoot.Opacity = 0;
			};
			Loaded += (_, __) =>
			{
				// WindowPlacementを設定してから起動アニメーション、同時にJukeboxをスタートする
				this.SetWindowPlacement(ViewModel.WindowPlacement.Value);
				StartupAnimation();
				ViewModel.JukeboxStart();
			};
			Closing += (_, ev) =>
			{
				// WindowPlacementを取得してからクローズアニメーション(終わるまでクローズキャンセル)
				// アニメーションが終わってから設定保存
				MediaListWindow.Hide();
				ViewModel.WindowPlacement.Value = this.GetWindowPlacement();
				ev.Cancel = ClosingAnimation();
				if (ev.Cancel == false)
				{
					App.Services.GetInstance<LogManager>().AddDebugLog("App MainWindow Closing, (save/hide)");
					ViewModel.SavePreferences();
					App.Models.Get<AudioPlayerManager>().Close();
				}
			};

			//=[ Size Changing ]
			SystemEvents.DisplaySettingsChanged += (_, __) =>
			{
				// 画面サイズが変わったらWindowPlacementを再設定/再取得する
				var rect = new Int32Rect((int)Left, (int)Top, (int)RenderSize.Width, (int)RenderSize.Height);
				this.SetWindowPlacement(rect);
				ViewModel.WindowPlacement.Value = this.GetWindowPlacement();
			};
			LocationChanged += (_, __) =>
			{
				// ウィンドウの位置が変更されたらMediaListWindowの位置を調整する
				MediaListWindow.WindowRelayout();
			};
			SizeChanged += (_, __) =>
			{
				// ウィンドウのサイズが(ry
				MediaListWindow.WindowRelayout();
			};

			//=[ LayoutRoot ]
			LayoutRoot.MouseEnter += (_, __) =>
			{
				// カーソルオンでアニメーション
				OpacityAnimation(ViewModel.InactiveOpacity.Value, 200, null);
			};
			LayoutRoot.MouseLeave += (_, ev) =>
			{
				// MediaListWindow表示中は透明度をイジらない
				if (MediaListWindow.IsVisible) return;
				// 左クリックでMouseLeaveが発行されるので無視する
				if (ev.LeftButton == MouseButtonState.Pressed) return;

				// カーソルオフでアニメーション
				OpacityAnimation(ViewModel.DeactiveOpacity.Value, 200, null);
			};
			LayoutRoot.MouseRightButtonUp += (_, ev) =>
			{
				// 他の要素でも右クリック反応してしまうのでRectangle以外には反応しないようにする
				if (ev.Source.GetType() == typeof(System.Windows.Shapes.Rectangle))
					ShowContextMenu(this.PointToScreen(ev.GetPosition(this)));
			};

			//=[ Title, Seek, Volume ]
			titleButton.Click += (_, __) =>
			{
				// クリックされたらMediaListWindowを開いたり閉じたり
				// スキップ操作はXAML側でCommandをバインディング
				ShowHideMediaListWindow();
			};
			seekExpander.PreviewMouseLeftButtonDown += (_, __) =>
			{
				// シーク選択中にINPCで変更通知を受け取らないようにバインディングを変更する
				Mouse.Capture(seekSlider, CaptureMode.SubTree);
				seekSlider.SetBinding(Slider.ValueProperty,
					new Binding("PositionTicks.Value") { Mode = BindingMode.OneTime, });
			};
			seekExpander.PreviewMouseLeftButtonUp += (_, __) =>
			{
				Mouse.Capture(null);
				// シーク選択が終了したら手動でプレーヤー再生位置を修正して...
				var v = (long)seekSlider.Value;
				ViewModel.SetPlayerPosition(TimeSpan.FromTicks(v));
				// INPC変更通知を受け取るようにする
				seekSlider.SetBinding(Slider.ValueProperty,
					new Binding("PositionTicks.Value") { Mode = BindingMode.OneWay, });
			};
			seekSlider.AutoTooltipConverter = (v) =>
			{
				// custom tooltip converter
				var ts = TimeSpan.FromTicks((long)v);
				var total = TimeSpan.FromTicks((long)seekSlider.Maximum);
				return string.Format("{0}{1:D2}:{2:D2} / {3}{4:D2}:{5:D2}",
					ts.Hours > 0 ? ts.Hours.ToString() + ":" : string.Empty,
					ts.Minutes, ts.Seconds,
					total.Hours > 0 ? total.Hours.ToString() + ":" : string.Empty,
					total.Minutes, total.Seconds);
			};
			volumeSlider.AutoTooltipConverter = (v) =>
			{
				// custom tooltip converter
				return string.Format("{0:F0}%", (double)v * 100.0);
			};

			//
			// Shortcut Key Handling
			ManagerServices.ShortcutKeyManager.Window_Move_On_RightDown_RequestAsObservable()
				.ObserveOnUIDispatcher()
				.Subscribe(_ => ResetWindowPosition());
			ManagerServices.ShortcutKeyManager.Window_ShowHide_RequestAsObservable()
				.ObserveOnUIDispatcher()
				.Subscribe(_ => WindowShowHideToggle());
		}

		//=[ ContextMenu ]
		MethodInfo cmenu_executer = null;
		WinForms.ContextMenu cmenu = null;
		void ShowContextMenu(Point screenPos)
		{
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
			cmenu.MenuItems.AddRange(TasktrayIconView.CreateWinFormsMenuItems());

			var helper = new WindowInteropHelper(this);
			WinAPI.SetForegroundWindow(helper.Handle);
			WinAPI.SetActiveWindow(helper.Handle);

			// 反則っぽい気がするけど...
			// リフレクションでWinFormsのコマンド実行子を強制実行
			var id = WinAPI.TrackPopupMenuEx(
				new HandleRef(cmenu, cmenu.Handle),
				0x142, (int)screenPos.X, (int)screenPos.Y,
				new HandleRef(helper, helper.Handle),
				IntPtr.Zero);
			App.Services.GetInstance<LogManager>().AddDebugLog($"TrackPopupMenuEx() result: {id}");
			if (id != 0)
			{
				var result = cmenu_executer.Invoke(null, new object[] { id });
				App.Services.GetInstance<LogManager>().AddDebugLog($"System.Windows.Forms.Command.DispatchID() result: {result}");
			}
		}

		//=[ Animation ]
		void OpacityAnimation(double to, double ms, Action complated)
		{
			var anim = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(ms)));
			if(complated != null)
			{
				Observable.FromEventPattern<EventHandler, EventArgs>(
					v => anim.Completed += v,
					v => anim.Completed -= v)
					.Take(1)
					.Subscribe(_ => complated());
			}
			this.LayoutRoot.BeginAnimation(Grid.OpacityProperty, anim);
		}
		void StartupAnimation()
		{
			// スムーズなアニメーションのために、ちょっと工夫
			// メッセージ処理が全部終えてからアニメーションするために優先度をIdleまで下げる
			LayoutRoot.Opacity = 0.0;
			App.Current.UIThreadBeginInvoke(() =>
			{
				// アクティブ時透明度→非アクティブ時透明度へアニメーション
				OpacityAnimation(ViewModel.InactiveOpacity.Value, 200, () =>
				{
					OpacityAnimation(ViewModel.DeactiveOpacity.Value, 3000, null);
				});
			}, DispatcherPriority.ApplicationIdle);
		}
		bool _window_closing_animation_completed;
		bool ClosingAnimation()
		{
			// ウィンドウを閉じるときにアニメーション
			// アニメーションが完了するまで閉じる処理をキャンセル
			// アニメーション中はtrueを返す
			var animating = !_window_closing_animation_completed;
			if (animating)
			{
				OpacityAnimation(0.0, 100, () =>
				{
					_window_closing_animation_completed = true;
					this.LayoutRoot.Opacity = 0.0;
					this.Close();
				});
			}
			return animating;
		}
		public void ForceDeactivateAnimation()
		{
			OpacityAnimation(ViewModel.DeactiveOpacity.Value, 200, null);
		}

		//
		public void ShowHideMediaListWindow()
		{
			if (MediaListWindow.IsVisible)
			{
				MediaListWindow.Hide();
				// マウスカーソルがウィンドウ上にいない場合は非アクティブ透明度にする
				var pt = InputManager.Current.PrimaryMouseDevice.GetPosition(this);
				var ie = this.InputHitTest(pt);
				if (ie == null)
				{
					OpacityAnimation(ViewModel.DeactiveOpacity.Value, 200, null);
				}
			}
			else
			{
				// MediaListWindowの表示指示が来たらウィンドウの透明度を変更
				// MediaListWindowの位置調整も行う、フォアグラウンド化とアクティブ化はIME絡みで念のため
				OpacityAnimation(ViewModel.InactiveOpacity.Value, 200, null);
				MediaListWindow.WindowRelayout();
				MediaListWindow.Show();
				var handle = new WindowInteropHelper(this).Handle;
				WinAPI.SetForegroundWindow(handle);
				WinAPI.SetActiveWindow(handle);
			}
		}
		public void WindowShowHideToggle()
		{
			if (IsVisible)
			{
				OpacityAnimation(0, 200, () => Hide());
			}
			else
			{
				LayoutRoot.Opacity = 0.001;
				Show();
				OpacityAnimation(ViewModel.InactiveOpacity.Value, 200, () =>
					OpacityAnimation(ViewModel.DeactiveOpacity.Value, 3000, null));
			}
		}

		public static Int32Rect GetDefaultWindowPosition()
		{
			var default_height = (int)(/*_DefaultDesignedHeight ??*/ 24);
			var area = WinForms.Screen.PrimaryScreen.WorkingArea;
			return new Int32Rect(area.Right - 450, area.Bottom - default_height, 450, default_height);
		}
		public void ResetWindowPosition()
		{
			var rc = GetDefaultWindowPosition();
			Left = rc.X;
			Top = rc.Y;
			Width = rc.Width;
			Height = rc.Height;
		}

	}
}

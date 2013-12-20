using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Quala;
using Quala.Interop.Win32;
using Quala.Windows;
using Quala.Windows.Forms;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.Properties;
using SmartAudioPlayerFx.Update;
using SmartAudioPlayerFx.Views;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	static class UIService
	{
		public static PlayerWindow PlayerWindow { get; private set; }
		public static MediaListWindow MediaListWindow { get; private set; }

		public static void Start()
		{
			// [TODO]
			// 極力早めにUIを出す
			// UIは初期化中の文字でも出す
			// 非UIはバックグラウンドで処理させる
			//

			// 初期化
			Preferences.Load();
			TasktrayService.IsVisible = true;
			UpdateService.Start();

			// 初期設定
			option_page = 0;
			inactive_opacity = 80;
			deactive_opacity = 65;

			// インスタンス作成
			PlayerWindow = new PlayerWindow();
			MediaListWindow = new MediaListWindow();

			// Preferences連携
			Preferences.Loaded += LoadPreferences;
			Preferences.Saving += SavePreferences;
			LoadPreferences(null, EventArgs.Empty);

			// とある情報の定期保存
			var savingTimer = Observable
				.Timer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5))
				.ObserveOnDispatcher()
				.Subscribe(_ => SaveAllPreferences());

			// Closeイベント
			Observable
				.FromEvent<CancelEventArgs>(PlayerWindow, "Closing")
				.Where(e => e.EventArgs.Cancel == false)
				.Take(1)
				.ObserveOnDispatcher()
				.Subscribe(_ =>
				{
					// AudioPlayer.Position等に影響があるので
					// この場所(AudioPlayer.Close()する前)で保存しないと正常なデータにならない
					savingTimer.Dispose();
					SaveAllPreferences();
					//
					MediaListWindow.Hide();
					JukeboxService.Dispose();
				});

			//
			Observable.Start(() => JukeboxService.Start());
		}

		#region preferences

		public static double? _DefaultDesignedHeight;
		static int option_page;
		public static int inactive_opacity;
		public static int deactive_opacity;

		static Rect GetDefaultWindowPosition()
		{
			var default_height = (int)(_DefaultDesignedHeight ?? 24);
			var area = WinForms.Screen.PrimaryScreen.WorkingArea;
			return new Rect(area.Right - 450, area.Bottom - default_height, 450, default_height);
		}

		static void LoadPreferences(object sender, EventArgs e)
		{
			var elm = Preferences.WindowSettings;

			// PlayerWindow
			var default_height = (int)(_DefaultDesignedHeight ?? 24);
			var bounds = new DynamicWindowBounds();
			elm.SubElement("DynamicWindowBounds", false, el =>
			{
				bounds.RealBounds = new Int32Rect(el.GetAttributeValueEx("RealLeft", 0), el.GetAttributeValueEx("RealTop", 0), el.GetAttributeValueEx("RealWidth", 0), el.GetAttributeValueEx("RealHeight", 0));
				bounds.LogicalBase = el.GetAttributeValueEx("LogicalBase", DynamicWindowBounds.LogicalBasePoint.LeftTop);
				bounds.LogicalPoint = new Point(el.GetAttributeValueEx("LogicalLeft", 0.0), el.GetAttributeValueEx("LogicalTop", 0.0));
				bounds.LogicalSize = new Size(el.GetAttributeValueEx("LogicalWidth", 0.0), el.GetAttributeValueEx("LogicalHeight", 0.0));
			});
			var rc = bounds.ToRect(true);
			if (rc.Width == 0 && rc.Height == 0)
			{
				rc = GetDefaultWindowPosition();
			}
			PlayerWindow.Left = rc.X;
			PlayerWindow.Top = rc.Y;
			PlayerWindow.Width = rc.Width;
			PlayerWindow.Height = default_height;
			PlayerWindow.Visibility = elm.GetAttributeValueEx("IsVisible", true) ? Visibility.Visible : Visibility.Hidden;
			//
			elm
				.GetAttributeValueEx((object)null, _ => option_page, "OptionPage")
				.GetAttributeValueEx((object)null, _ => inactive_opacity, "InactiveOpacity")
				.GetAttributeValueEx((object)null, _ => deactive_opacity, "DeactiveOpacity")
				// MediaListWindow
				.SubElement("MediaListWindow", false, el =>
				{
					el
						.GetAttributeValueEx(MediaListWindow, _ => _.Width)
						.GetAttributeValueEx(MediaListWindow, _ => _.Height)
						.GetAttributeValueEx(MediaListWindow, _ => _.TreeWidth)
						.GetAttributeValueEx(MediaListWindow, _ => _.IsTitleFromFileName)
						.GetAttributeValueEx(MediaListWindow, _ => _.IsAutoCloseWhenInactive)
						.GetAttributeValueEx(MediaListWindow, _ => _.IsAutoCloseWhenListSelected);
				});
		}
		static void SavePreferences(object sender, CancelEventArgs e)
		{
			var elm = Preferences.WindowSettings;
			var bounds = new DynamicWindowBounds(PlayerWindow.Left, PlayerWindow.Top, PlayerWindow.Width, PlayerWindow.Height);
			// PlayerWindow
			elm
				.SetAttributeValueEx("OptionPage", option_page)
				.SetAttributeValueEx("InactiveOpacity", inactive_opacity)
				.SetAttributeValueEx("DeactiveOpacity", deactive_opacity)
				.SetAttributeValueEx(() => PlayerWindow.IsVisible);
			// ** DynamicWindowBounds
			elm
				.GetOrCreateElement("DynamicWindowBounds")
				.SetAttributeValueEx("RealLeft", bounds.RealBounds.X)
				.SetAttributeValueEx("RealTop", bounds.RealBounds.Y)
				.SetAttributeValueEx("RealWidth", bounds.RealBounds.Width)
				.SetAttributeValueEx("RealHeight", bounds.RealBounds.Height)
				.SetAttributeValueEx("LogicalBase", bounds.LogicalBase)
				.SetAttributeValueEx("LogicalLeft", bounds.LogicalPoint.X)
				.SetAttributeValueEx("LogicalTop", bounds.LogicalPoint.Y)
				.SetAttributeValueEx("LogicalWidth", bounds.LogicalSize.Width)
				.SetAttributeValueEx("LogicalHeight", bounds.LogicalSize.Height);
			// MediaListWindow
			elm
				.GetOrCreateElement("MediaListWindow")
				.SetAttributeValueEx(() => MediaListWindow.Width)
				.SetAttributeValueEx(() => MediaListWindow.Height)
				.SetAttributeValueEx(() => MediaListWindow.TreeWidth)
				.SetAttributeValueEx(() => MediaListWindow.IsTitleFromFileName)
				.SetAttributeValueEx(() => MediaListWindow.IsAutoCloseWhenInactive)
				.SetAttributeValueEx(() => MediaListWindow.IsAutoCloseWhenListSelected);
			//
		}

		static void SaveAllPreferences()
		{
			JukeboxService.AllItems.RaiseDBCommit();
			Preferences.Save();
		}

		#endregion
		#region ContextMenu

		// PlayerWindowでも使う
		public static WinForms.MenuItem[] CreateWinFormsMenuItems()
		{
			return ConvertToWinFormsMenuItems(CreateMenuItems()).ToArray();
		}
		static IEnumerable<WinForms.MenuItem> ConvertToWinFormsMenuItems(IEnumerable<MenuItemDefinition> items)
		{
			foreach (var i in items)
			{
				var item = new WinForms.MenuItem();
				item.Text = i.Name;
				item.Checked = i.Checked;
				item.Enabled = i.Enabled;
				item.Visible = i.Visibled;
				if (i.Clicked != null)
				{
					var i2 = i;
					item.Click += delegate { i2.Clicked(); };
				}
				if (i.SubItems != null)
				{
					item.MenuItems.AddRange(ConvertToWinFormsMenuItems(i.SubItems).ToArray());
				}
				yield return item;
			}
		}
		static IEnumerable<MenuItemDefinition> CreateMenuItems()
		{
			var is_videodraw = PlayerWindow.ViewModel.IsVideoDrawing.Value;
			var is_repeat = JukeboxService.IsRepeat;
			var is_random = (JukeboxService.SelectMode == JukeboxService.SelectionMode.Random);
			var is_sequential = (JukeboxService.SelectMode == JukeboxService.SelectionMode.Filename);
			yield return new MenuItemDefinition("再生モード", subitems: new[]
			{
				new MenuItemDefinition("リピート", is_repeat, ()=> JukeboxService.SetIsRepeat(!is_repeat)),
				new MenuItemDefinition("-"),
				new MenuItemDefinition("ランダム", is_random, ()=> JukeboxService.SetSelectMode(JukeboxService.SelectionMode.Random)),
				new MenuItemDefinition("ファイル名順", is_sequential, ()=> JukeboxService.SetSelectMode(JukeboxService.SelectionMode.Filename))
			});
			var vol = JukeboxService.AudioPlayer.Volume;
			var vol_is_max = vol >= 1.0;
			var vol_is_min = vol <= 0.0;
			var vol_text = "ボリューム (" + (vol * 100.0).ToString("F0") + "%)";
			yield return new MenuItemDefinition(vol_text, subitems: new[]
			{
				new MenuItemDefinition("上げる", enabled: !vol_is_max, clicked: ()=>JukeboxService.AudioPlayer.SetVolume(JukeboxService.AudioPlayer.Volume+0.1)),
				new MenuItemDefinition("下げる", enabled: !vol_is_min, clicked: ()=>JukeboxService.AudioPlayer.SetVolume(JukeboxService.AudioPlayer.Volume-0.1)),
			});
			yield return new MenuItemDefinition("-");
			var play_pause_text = (JukeboxService.AudioPlayer.IsPaused) ? "再生" : "一時停止";
			yield return new MenuItemDefinition(play_pause_text, clicked: () => JukeboxService.AudioPlayer.PlayPause());
			yield return new MenuItemDefinition("スキップ", clicked: () => JukeboxService.SelectNext(true));
			yield return new MenuItemDefinition("始めから再生", clicked: () => JukeboxService.AudioPlayer.Replay());
			yield return new MenuItemDefinition("再生履歴", subitems: CreateRecentPlayMenuItems());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("開く", subitems: CreateRecentFolderMenuItems());
			yield return new MenuItemDefinition("-");
			var window_show_hide_text = UIService.PlayerWindow.IsVisible ? "ウィンドウを隠す" : "ウィンドウを表示する";
			yield return new MenuItemDefinition(window_show_hide_text, clicked: () => WindowShowHideToggle());
			yield return new MenuItemDefinition("ウィンドウを画面右下へ移動", clicked: () => ResetWindowPosition());
			yield return new MenuItemDefinition("-");
			yield return new MenuItemDefinition("アップデート", enabled: !UpdateService.IsShowingUpdateMessage, is_visibled: UpdateService.IsUpdateReady, clicked: OnUpdate);
			yield return new MenuItemDefinition("オプション", enabled: !option_dialog_opened, clicked: OpenOptionDialog);
			yield return new MenuItemDefinition("終了", clicked: () => PlayerWindow.Close());
		}

		static bool folder_dialog_opened = false;
		static bool option_dialog_opened = false;
		static MenuItemDefinition[] CreateRecentFolderMenuItems()
		{
			var head = new[]
			{
				new MenuItemDefinition("フォルダ", clicked: OpenFolderDialog, enabled: !folder_dialog_opened),
				new MenuItemDefinition("-"),
			};
			//
			var current = JukeboxService.AllItems.FocusPath;
			var items = JukeboxService.GetFolderRecents()
				.Select(f => new MenuItemDefinition(f,
					is_checked: f.Equals(current, StringComparison.CurrentCultureIgnoreCase),
					clicked: delegate { Observable.Start(() => JukeboxService.AllItems.SetFocusPath(f, true)); }))
				.ToArray();
			if (!items.Any())
				items = new[] { new MenuItemDefinition("履歴はありません", enabled: false), };
			//
			var ret = head.Concat(items).ToArray();
			return ret;
		}
		static MenuItemDefinition[] CreateRecentPlayMenuItems()
		{
			var current = JukeboxService.AudioPlayer.CurrentOpenedPath;
			var ret = JukeboxService.GetCachedRecentPlayItemsPath(20)
				.Select((f, i) => new MenuItemDefinition(Path.GetFileName(f),
					is_checked: f.Equals(current, StringComparison.CurrentCultureIgnoreCase),
					clicked: delegate { JukeboxService.SelectPrevious(i); }))
				.ToArray();
			return ret.Any() ? ret : new[] { new MenuItemDefinition("履歴はありません", enabled: false), };
		}
		static void OnUpdate()
		{
			if (UpdateService.ShowUpdateMessage(UIService.PlayerWindow.WindowHelper.Handle))
				PlayerWindow.Close();
		}
		static void OpenFolderDialog()
		{
			if (folder_dialog_opened) return;
			if (PlayerWindow.WindowHelper == null) return;
			try
			{
				folder_dialog_opened = true;
				using (var dlg = new FolderBrowserDialogEx())
				{
					dlg.UseNewDialog = true;
					dlg.SelectedPath = JukeboxService.AllItems.FocusPath;
					var nativeWindow = new WinForms.NativeWindow();
					nativeWindow.AssignHandle(PlayerWindow.WindowHelper.Handle);
					try
					{
						if (dlg.ShowDialog(nativeWindow) == WinForms.DialogResult.OK)
							Observable.Start(() => JukeboxService.AllItems.SetFocusPath(dlg.SelectedPath, true));
					}
					finally { nativeWindow.ReleaseHandle(); }
				}
			}
			finally { folder_dialog_opened = false; }
		}
		static void OpenOptionDialog()
		{
			if (option_dialog_opened) return;
			if (PlayerWindow.WindowHelper == null) return;

			var nativeWindow = new WinForms.NativeWindow();
			try
			{
				option_dialog_opened = true;
				ShortcutKeyService.HotKeyManager.SuspressKeyEvent = true;
				nativeWindow.AssignHandle(PlayerWindow.WindowHelper.Handle);
				using (var dlg = new Options.OptionDialog())
				{
					CenterWindow(dlg.Handle, PlayerWindow.WindowHelper.Handle);
					dlg.PageIndex = option_page;
					dlg.InactiveOpacity = inactive_opacity;
					dlg.DeactiveOpacity = deactive_opacity;
					if (dlg.ShowDialog(nativeWindow) == WinForms.DialogResult.OK)
					{
						option_page = dlg.PageIndex;
						inactive_opacity = dlg.InactiveOpacity;
						deactive_opacity = dlg.DeactiveOpacity;
						if (MediaListWindow.IsVisible)
						{
							PlayerWindow.BeginAnimation(Window.OpacityProperty,
								new DoubleAnimation(inactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(200))));
						}
						else
						{
							PlayerWindow.BeginAnimation(Window.OpacityProperty,
								new DoubleAnimation(deactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(200))));
						}
					}
				}
			}
			finally
			{
				option_dialog_opened = false;
				ShortcutKeyService.HotKeyManager.SuspressKeyEvent = false;
				nativeWindow.ReleaseHandle();
			}
		}
		static void ResetWindowPosition()
		{
			var rc = GetDefaultWindowPosition();
			PlayerWindow.Left = rc.Left;
			PlayerWindow.Top = rc.Top;
			PlayerWindow.Width = rc.Width;
			PlayerWindow.Height = rc.Height;
		}
		static void WindowShowHideToggle()
		{
			if (PlayerWindow.IsVisible)
			{
				PlayerWindow.BeginAnimation(
					Window.OpacityProperty,
					new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(200))),
					() => PlayerWindow.Hide());
			}
			else
			{
				PlayerWindow.Opacity = 0.001;
				PlayerWindow.Show();
				PlayerWindow.BeginAnimation(
					Window.OpacityProperty,
					new DoubleAnimation(deactive_opacity / 100.0, new Duration(TimeSpan.FromMilliseconds(200))));
			}
		}

		struct MenuItemDefinition
		{
			public string Name;
			public bool Enabled;
			public bool Checked;
			public Action Clicked;
			public MenuItemDefinition[] SubItems;
			public bool Visibled;

			public MenuItemDefinition(
				string name,
				bool is_checked = false,
				Action clicked = null,
				MenuItemDefinition[] subitems = null,
				bool enabled = true,
				bool is_visibled = true)
			{
				this.Name = name;
				this.Enabled = enabled;
				this.Checked = is_checked;
				this.Clicked = clicked;
				this.SubItems = subitems;
				this.Visibled = is_visibled;
			}
		}

		#endregion
		#region Helpers

		// hWndをhWndCenterの中央に配置します
		public static void CenterWindow(IntPtr hWnd, IntPtr hWndCenter)
		{
			if (hWnd == IntPtr.Zero)
			{
				throw new ArgumentException("handle == 0", "hWnd");
			}
			if (hWndCenter == IntPtr.Zero)
			{
				hWndCenter = API.GetDesktopWindow();
			}

			var dwStyle = (WS)API.GetWindowLong(hWnd, GWL.STYLE);
			RECT rcArea;
			RECT rcCenter;
			IntPtr hWndParent;
			if (dwStyle.HasFlag(WS.CHILD))
			{
				hWndParent = API.GetParent(hWnd);
				API.GetClientRect(hWndParent, out rcArea);
				API.GetClientRect(hWndCenter, out rcCenter);
				var rcCemter_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(rcCenter));
				Marshal.StructureToPtr(rcCenter, rcCemter_ptr, false);
				API.MapWindowPoints(hWndCenter, hWndParent, rcCemter_ptr, 2);
				rcCenter = (RECT)Marshal.PtrToStructure(rcCemter_ptr, typeof(RECT));
				Marshal.FreeHGlobal(rcCemter_ptr);
			}
			else
			{
				dwStyle = (WS)API.GetWindowLong(hWndCenter, GWL.STYLE);
				API.GetWindowRect(hWndCenter, out rcCenter);
				var hMonitor = API.MonitorFromWindow(hWndCenter, MONITOR_DEFAULTTO.NEAREST);
				var mi = new MONITORINFO();
				mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
				API.GetMonitorInfo(hMonitor, ref mi);
				rcArea = mi.rcWork;
			}
			RECT rcWindow;
			API.GetWindowRect(hWnd, out rcWindow);
			int wndWidth = rcWindow.right - rcWindow.left;
			int wndHeight = rcWindow.bottom - rcWindow.top;
			int xLeft = (rcCenter.left + rcCenter.right) / 2 - wndWidth / 2;
			int yTop = (rcCenter.top + rcCenter.bottom) / 2 - wndHeight / 2;
			if (xLeft < rcArea.left)
				xLeft = rcArea.left;
			else if (xLeft + wndWidth > rcArea.right)
				xLeft = rcArea.right - wndWidth;
			if (yTop < rcArea.top)
				yTop = rcArea.top;
			else if (yTop + wndHeight > rcArea.bottom)
				yTop = rcArea.bottom - wndHeight;
			API.SetWindowPos(hWnd, IntPtr.Zero, xLeft, yTop, 0, 0, SWP.NOSIZE | SWP.NOZORDER | SWP.NOACTIVATE);
		}

		/// <summary>
		/// Application.Current.Dispatcher.Invoke().
		/// </summary>
		/// <param name="action"></param>
		public static void UIThreadInvoke(Action action)
		{
			if (action == null) throw new ArgumentNullException("action");
			var currentApp = Application.Current;
			if (currentApp == null) return;
			var dispatcher = currentApp.Dispatcher;
			if (dispatcher.HasShutdownStarted) return;
			dispatcher.Invoke(action);
		}
		/// <summary>
		/// Application.Current.Dispatcher.BeginInvoke().
		/// </summary>
		/// <param name="action"></param>
		public static void UIThreadBeginInvoke(DispatcherPriority priority, Action action)
		{
			if (action == null) throw new ArgumentNullException("action");
			var currentApp = Application.Current;
			if (currentApp == null) return;
			var dispatcher = currentApp.Dispatcher;
			if (dispatcher.HasShutdownStarted) return;
			dispatcher.BeginInvoke(action, priority);
		}

		/// <summary>
		/// UIElement.BeginAnimation()
		/// </summary>
		/// <param name="element"></param>
		/// <param name="property"></param>
		/// <param name="animation"></param>
		/// <param name="onComplate"></param>
		public static void BeginAnimation(this UIElement element,
			DependencyProperty property, AnimationTimeline animation, Action onComplate)
		{
			if (element == null) throw new ArgumentNullException("element");
			if (property == null) throw new ArgumentNullException("property");
			if (animation == null) throw new ArgumentNullException("animation");

			if (onComplate != null)
			{
				animation.Completed += delegate { onComplate(); };
			}
			element.BeginAnimation(property, animation);
		}

		#endregion

		/// <summary>
		/// メッセージ表示メソッド
		/// </summary>
		public static void ShowMessage(string message)
		{
			WinForms.MessageBox.Show(message, "SmartAudioPlayer Fx");
		}
		/// <summary>
		/// 未処理の例外用メッセージ表示メソッド
		/// </summary>
		/// <param name="source">例外の発生源</param>
		/// <param name="ex">例外</param>
		public static void ShowExceptionMessage(string source, Exception ex)
		{
			LogService.AddCriticalErrorLog("UnhandledException", ex);
			var message = string.Format(
				"未処理の例外エラーが発生しました ({1}){0}" +
				"----------------------------------------{0}" +
				"{2}",
				Environment.NewLine,
				source,
				ex);
			WinForms.MessageBox.Show(message, "SmartAudioPlayer Fx");
		}

	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Linq;
using Quala;
using Quala.Interop.Win32;
using Quala.Windows;
using Quala.Windows.Forms;
using SmartAudioPlayerFx.Player;
using SmartAudioPlayerFx.UI.Views;
using SmartAudioPlayerFx.Update;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx.UI
{
	static class UIService
	{
		public static PlayerWindow PlayerWindow { get; private set; }
		public static MediaListWindow MediaListWindow { get; private set; }

		static UIService()
		{
			PlayerWindow__ctor();
			MediaListWindow__ctor();
		}

		public static void PrepareService()
		{
			LoadPreferences();
			// 定期的にデータを保存する
			var savingTimer = Observable
				.Timer(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5))
				.ObserveOnDispatcher()
				.Subscribe(_ => SaveAllPreferences());
			// ログオフ -> Close
			Observable.FromEvent<SessionEndingCancelEventArgs>(Application.Current, "SessionEnding")
				.Where(e => e.EventArgs.Cancel == false)
				.Take(1)
				.ObserveOnDispatcher()
				.Subscribe(_ => PlayerWindow.Close());
			// Closeイベント
			Observable.FromEvent<CancelEventArgs>(PlayerWindow, "Closing")
				.Where(e => e.EventArgs.Cancel == false)
				.Take(1)
				.ObserveOnDispatcher()
				.Subscribe(_ =>
				{
					savingTimer.Dispose();
					SaveAllPreferences();
					MediaListWindow.Hide();
					if (!JukeboxService.AudioPlayer.IsPaused)
						JukeboxService.AudioPlayer.Close();
				});
		}
		static void SaveAllPreferences()
		{
			PlayerWindow.ViewModel.SavePreferences();
			SavePreferences();
			MediaDBService.SaveChanges();
			UpdateService.SavePreferences();
		}

		#region preferences

		static double? _DefaultDesignedHeight;
		public static int option_page = 0;
		public static int inactive_opacity = 80;
		public static int deactive_opacity = 65;

		static void SavePreferences()
		{
			var elm = PreferenceService.Load("data", "window.xml") ?? new XElement("Window");
			if (elm.Name != "Window") elm.Name = "Window";
			// PlayerWindow
			elm.SetAttributeValue("OptionPage", option_page);
			elm.SetAttributeValue("InactiveOpacity", inactive_opacity);
			elm.SetAttributeValue("DeactiveOpacity", deactive_opacity);
			elm.SetAttributeValue("IsVisible", PlayerWindow.IsVisible);
			// ** DynamicWindowBounds
			var bounds = new DynamicWindowBounds(PlayerWindow.Left, PlayerWindow.Top, PlayerWindow.Width, PlayerWindow.Height);
			var elm2 = elm.Element("DynamicWindowBounds");
			if (elm2 == null) { elm2 = new XElement("DynamicWindowBounds"); elm.Add(elm2); }
			elm2.SetAttributeValue("RealLeft", bounds.RealBounds.X);
			elm2.SetAttributeValue("RealTop", bounds.RealBounds.Y);
			elm2.SetAttributeValue("RealWidth", bounds.RealBounds.Width);
			elm2.SetAttributeValue("RealHeight", bounds.RealBounds.Height);
			elm2.SetAttributeValue("LogicalBase", bounds.LogicalBase);
			elm2.SetAttributeValue("LogicalLeft", bounds.LogicalPoint.X);
			elm2.SetAttributeValue("LogicalTop", bounds.LogicalPoint.Y);
			elm2.SetAttributeValue("LogicalWidth", bounds.LogicalSize.Width);
			elm2.SetAttributeValue("LogicalHeight", bounds.LogicalSize.Height);
			// MediaListWindow
			elm2 = elm.Element("MediaListWindow");
			if (elm2 == null) { elm2 = new XElement("MediaListWindow"); elm.Add(elm2); }
			elm2.SetAttributeValue("Width", MediaListWindow.Width);
			elm2.SetAttributeValue("Height", MediaListWindow.Height);
			elm2.SetAttributeValue("TreeWidth", MediaListWindow.TreeWidth);
			//
			PreferenceService.Save(elm, "data", "window.xml");
		}

		static void LoadPreferences()
		{
			var elm = PreferenceService.Load("data", "window.xml");
			if (elm == null || elm.Name.LocalName != "Window") elm = null;
			// PlayerWindow
			var elm2 = (elm != null) ? elm.Element("DynamicWindowBounds") : (XElement)null;
			var default_height = (int)(_DefaultDesignedHeight ?? 24);
			var bounds = new DynamicWindowBounds();
			bounds.RealBounds = new Int32Rect(
				elm2.GetOrDefaultValue("RealLeft", 0),
				elm2.GetOrDefaultValue("RealTop", 0),
				elm2.GetOrDefaultValue("RealWidth", 0),
				elm2.GetOrDefaultValue("RealHeight", 0));
			bounds.LogicalBase =
				elm2.GetOrDefaultValue("LogicalBase", DynamicWindowBounds.LogicalBasePoint.LeftTop);
			bounds.LogicalPoint = new Point(
				elm2.GetOrDefaultValue("LogicalLeft", 0.0),
				elm2.GetOrDefaultValue("LogicalTop", 0.0));
			bounds.LogicalSize = new Size(
				elm2.GetOrDefaultValue("LogicalWidth", 0.0),
				elm2.GetOrDefaultValue("LogicalHeight", 0.0));
			var rc = bounds.ToRect(true);
			if (rc.Width == 0 && rc.Height == 0)
			{
				rc = GetDefaultWindowPosition();
			}
			PlayerWindow.Left = rc.X;
			PlayerWindow.Top = rc.Y;
			PlayerWindow.Width = rc.Width;
			PlayerWindow.Height = default_height;
			PlayerWindow.Visibility =
				elm.GetOrDefaultValue("IsVisible", true) ? Visibility.Visible : Visibility.Hidden;
			option_page = elm.GetOrDefaultValue("OptionPage", 0);
			inactive_opacity = elm.GetOrDefaultValue("InactiveOpacity", 80);
			deactive_opacity = elm.GetOrDefaultValue("DeactiveOpacity", 65);
			// MediaListWindow
			var elm3 = (elm != null) ? elm.Element("MediaListWindow") : (XElement)null;
			MediaListWindow.Width = elm3.GetOrDefaultValue("Width", 750);
			MediaListWindow.Height = elm3.GetOrDefaultValue("Height", 350);
			MediaListWindow.TreeWidth = elm3.GetOrDefaultValue("TreeWidth", 250);
		}

		#endregion
		#region PlayerWindow

		static void PlayerWindow__ctor()
		{
			var vm = new PlayerServiceViewModel();
			vm.LoadPreferences();
			PlayerWindow = new PlayerWindow(vm);
			PlayerWindow.SourceInitialized += delegate
			{
				// フォントサイズの変更に対応するために、表示してから取得する
				// (デフォルトのウィンドウ位置を計算するために必要)
				_DefaultDesignedHeight = PlayerWindow.Height = PlayerWindow.DesiredSize.Height;
				// PlayerWindowの移動に追随する
				PlayerWindow.LocationChanged += delegate { MediaListWindow.WindowRelayout(PlayerWindow); };
				PlayerWindow.SizeChanged += delegate { MediaListWindow.WindowRelayout(PlayerWindow); };
			};
		}

		public static Rect GetDefaultWindowPosition()
		{
			var default_height = (int)(_DefaultDesignedHeight ?? 24);
			var area = WinForms.Screen.PrimaryScreen.WorkingArea;
			return new Rect(area.Right - 450, area.Bottom - default_height, 450, default_height);
		}

		#endregion
		#region MediaListWindow

		static void MediaListWindow__ctor()
		{
			MediaListWindow = new MediaListWindow(new MediaListViewModel());
			MediaListWindow.SourceInitialized += delegate
			{
				MediaListWindow.Owner = PlayerWindow;
				// 読み込まれたときにウィンドウ位置を調整
				MediaListWindow.Loaded += delegate { MediaListWindow.WindowRelayout(PlayerWindow); };
			};
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
			yield return new MenuItemDefinition("ビデオを描画する", is_videodraw, () => PlayerWindow.ViewModel.IsVideoDrawing.Value = !is_videodraw);
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
			var items = JukeboxService.AllItems.GetFolderRecents()
				.Select(f => new MenuItemDefinition(f,
					is_checked: f.Equals(current, StringComparison.CurrentCultureIgnoreCase),
					clicked: delegate { JukeboxService.AllItems.SetFocusPath(f, true); }))
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
					dlg.SelectedPath = JukeboxService.AllItems.FocusPath;
					var nativeWindow = new WinForms.NativeWindow();
					nativeWindow.AssignHandle(PlayerWindow.WindowHelper.Handle);
					try
					{
						if (dlg.ShowDialog(nativeWindow) == WinForms.DialogResult.OK)
							JukeboxService.AllItems.SetFocusPath(dlg.SelectedPath, true);
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


		#endregion

		#region Helpers

		public static Dispatcher Dispatcher
		{
			get
			{
				var app = System.Windows.Application.Current;
				return (app == null) ? (Dispatcher)null : app.Dispatcher;
			}
		}

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
				MONITORINFO mi;
				mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
				API.GetMonitorInfo(hMonitor, out mi);
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
		/// Application.Current.Dispatcher.Invoke().
		/// </summary>
		/// <param name="action"></param>
		public static T UIThreadInvoke<T>(Func<T> func)
		{
			if (func == null) throw new ArgumentNullException("func");
			var currentApp = Application.Current;
			if (currentApp == null) return default(T);
			var dispatcher = currentApp.Dispatcher;
			if (dispatcher.HasShutdownStarted) return default(T);
			return (T)dispatcher.Invoke(func);
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
		#region define

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

	}
}

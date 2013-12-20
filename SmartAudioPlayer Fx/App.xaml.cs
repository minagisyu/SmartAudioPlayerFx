using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using __Primitives__;
using Codeplex.Reactive;
using SmartAudioPlayerFx.Managers;
using SmartAudioPlayerFx.Views;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			var sw = Stopwatch.StartNew();

			// WinForms Initialize
			WinForms.Application.EnableVisualStyles();
			WinForms.Application.SetCompatibleTextRenderingDefault(false);
			WinForms.Application.DoEvents();
#if !DEBUG
			// Exception Handling
			AppDomain.CurrentDomain.UnhandledException += (_, x) =>
				ShowExceptionMessage(x.ExceptionObject as Exception);
			this.DispatcherUnhandledException += (_, x) =>
				ShowExceptionMessage(x.Exception);
#endif
			// Logger Dispsose
			AppDomain.CurrentDomain.ProcessExit += (_, __) =>
			{
				Logger.Save(PreferencesManager.CreateFullPath("SmartAudioPlayer Fx.log"));
				Logger.Dispose();
			};

			// minimum Initialize
			UIDispatcherScheduler.Initialize();
			var dbFilename = PreferencesManager.CreateFullPath("data", "media.db");
			ManagerServices.Initialize(dbFilename);

			// アップデートチェック
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (HandleUpdateProcess(e.Args))
				return;

			// 多重起動の抑制
			// trueが帰ったときはShutdown()呼んだ後なのでretuenする
			if (CheckApplicationInstance())
				return;

			this.MainWindow = new Views.MainWindow();
			this.MainWindow.Show();

			// LogOff -> Close
			App.Current.SessionEnding += delegate
			{
				if (MainWindow == null) return;
				MainWindow.Close();
			};

			// Set TrayIcon Menus
			ManagerServices.TaskIconManager.SetMenuItems();

			// 定期保存(すぐに開始する)
			new DispatcherTimer(
				TimeSpan.FromMinutes(5),
				DispatcherPriority.Normal,
				(_, __) => ManagerServices.PreferencesManager.Save(),
				Dispatcher);

			sw.Stop();
			Logger.AddDebugLog("App.OnStartrup: {0}ms", sw.ElapsedMilliseconds);
		}

		bool HandleUpdateProcess(string[] args)
		{
			ManagerServices.AppUpdateManager.OnPostUpdate(args);
			if (ManagerServices.AppUpdateManager.OnUpdate(args))
			{
				Shutdown(-2);
				return true;
			}
			return false;
		}
		bool CheckApplicationInstance()
		{
#if DEBUG
			return false;
#else
			var mutex = new Mutex(false, "SmartAudioPlayer Fx");
			if (mutex.WaitOne(0, false))
			{
				// 新規起動
				Observable.FromEventPattern<ExitEventHandler, ExitEventArgs>(
					v => this.Exit += v,
					v => this.Exit -= v)
					.Take(1)
					.Subscribe(_ =>
					{
						// ReleaseMutex()呼ばないとMutexが残る...
						mutex.ReleaseMutex();
						mutex.Dispose();
					});
				return false;
			}
			else
			{
				// すでに起動しているインスタンスがある
				Logger.AddInfoLog("多重起動を確認しました。");
				ShowMessage("多重起動は出来ません。アプリケーションを終了します。");
				mutex.Dispose();
				Shutdown(-1);
				return true;
			}
#endif
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
			ManagerServices.Dispose();
		}

		#region Utiliy Methods

		public static void UIThreadInvoke(Action action)
		{
			if (action == null) throw new ArgumentNullException("action");

			var currentApp = Application.Current;
			if (currentApp == null)
			{
				action();
				return;
			}

			var dispatcher = currentApp.Dispatcher;
			if (dispatcher.HasShutdownStarted) return;
			if (Thread.CurrentThread == dispatcher.Thread)
				action();
			else
				dispatcher.Invoke(action);
		}
		public static T UIThreadInvoke<T>(Func<T> action)
		{
			if (action == null) throw new ArgumentNullException("action");
			var currentApp = Application.Current;
			if (currentApp == null)
			{
				return action();
			}
			var dispatcher = currentApp.Dispatcher;
			if (dispatcher.HasShutdownStarted) return default(T);
			return (Thread.CurrentThread == dispatcher.Thread) ?
				action() :
				(T)dispatcher.Invoke(action);
		}
		public static void UIThreadBeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			if (action == null) throw new ArgumentNullException("action");
			var currentApp = Application.Current;
			if (currentApp == null) return;
			var dispatcher = currentApp.Dispatcher;
			if (dispatcher.HasShutdownStarted) return;
			dispatcher.BeginInvoke(action, priority);
		}

		public static void ShowMessage(string message)
		{
			// WPFのMessageBoxはビジュアルスタイルが効かないから使わない
			WinForms.MessageBox.Show(message, "SmartAudioPlayer Fx");
		}
		public static void ShowExceptionMessage(Exception ex)
		{
			// todo: 専用のダイアログ使う？
			Logger.AddCriticalErrorLog("UnhandledException", ex);
			var message = string.Format(
				"未処理の例外エラーが発生しました{0}" +
				"----------------------------------------{0}" +
				"{1}",
				Environment.NewLine,
				ex);
			using (var dlg = new MessageDialog())
			{
				dlg.Title = "SmartAudioPlayer Fx";
				dlg.HeaderMessage = "未処理の例外エラーが発生しました";
				dlg.DescriptionMessage = ex.ToString();
				dlg.ShowDialog();
			}
		}

		/// <summary>
		/// 保存場所をエクスプローラで開く
		/// ファイルならそのフォルダを開いた後ファイルを選択する
		/// </summary>
		/// <param name="path"></param>
		public static void OpenToExplorer(string path)
		{
			if (File.Exists(path))
				Process.Start("explorer.exe", "/e, /select, \"" + path + "\"");
			else
				Process.Start("explorer.exe", "/e, \"" + path + "\"");
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
				hWndCenter = WinAPI.GetDesktopWindow();
			}

			var dwStyle = (WinAPI.WS)WinAPI.GetWindowLong(hWnd, WinAPI.GWL.STYLE);
			WinAPI.RECT rcArea;
			WinAPI.RECT rcCenter;
			IntPtr hWndParent;
			if (dwStyle.HasFlag(WinAPI.WS.CHILD))
			{
				hWndParent = WinAPI.GetParent(hWnd);
				WinAPI.GetClientRect(hWndParent, out rcArea);
				WinAPI.GetClientRect(hWndCenter, out rcCenter);
				var rcCemter_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(rcCenter));
				Marshal.StructureToPtr(rcCenter, rcCemter_ptr, false);
				WinAPI.MapWindowPoints(hWndCenter, hWndParent, rcCemter_ptr, 2);
				rcCenter = (WinAPI.RECT)Marshal.PtrToStructure(rcCemter_ptr, typeof(WinAPI.RECT));
				Marshal.FreeHGlobal(rcCemter_ptr);
			}
			else
			{
				dwStyle = (WinAPI.WS)WinAPI.GetWindowLong(hWndCenter, WinAPI.GWL.STYLE);
				WinAPI.GetWindowRect(hWndCenter, out rcCenter);
				var hMonitor = WinAPI.MonitorFromWindow(hWndCenter, WinAPI.MONITOR_DEFAULTTO.NEAREST);
				var mi = new WinAPI.MONITORINFO();
				mi.cbSize = Marshal.SizeOf(typeof(WinAPI.MONITORINFO));
				WinAPI.GetMonitorInfo(hMonitor, ref mi);
				rcArea = mi.rcWork;
			}
			WinAPI.RECT rcWindow;
			WinAPI.GetWindowRect(hWnd, out rcWindow);
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
			WinAPI.SetWindowPos(hWnd, IntPtr.Zero, xLeft, yTop, 0, 0, WinAPI.SWP.NOSIZE | WinAPI.SWP.NOZORDER | WinAPI.SWP.NOACTIVATE);
		}

		#endregion

	}
}

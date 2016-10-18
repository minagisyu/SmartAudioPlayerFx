using Quala;
using Quala.Win32;
using Quala.Win32.Dialog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayerFx
{
	public static class ApplicationExtension
	{
		[Obsolete]
		public static void UIThreadInvoke(this Application app, Action action)
		{
			if (action == null) throw new ArgumentNullException("action");

			if (app == null)
			{
				action();
				return;
			}

			var dispatcher = app.Dispatcher;
			if (dispatcher.HasShutdownStarted) return;
			if (Thread.CurrentThread == dispatcher.Thread)
				action();
			else
				dispatcher.Invoke(action);
		}
		[Obsolete]
		public static T UIThreadInvoke<T>(this Application app, Func<T> action)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (app == null)
			{
				return action();
			}
			var dispatcher = app.Dispatcher;
			if (dispatcher.HasShutdownStarted) return default(T);
			return (Thread.CurrentThread == dispatcher.Thread) ?
				action() :
				(T)dispatcher.Invoke(action);
		}
		[Obsolete]
		public static void UIThreadBeginInvoke(this Application app, Action action, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (app == null) return;
			var dispatcher = app.Dispatcher;
			if (dispatcher.HasShutdownStarted) return;
			dispatcher.BeginInvoke(action, priority);
		}

		[Obsolete]
		public static void ShowMessage(this Application app, string message)
		{
			// WPFのMessageBoxはビジュアルスタイルが効かないから使わない
			WinForms.MessageBox.Show(message, "SmartAudioPlayer Fx");
		}
		[Obsolete]
		public static void ShowExceptionMessage(this Application app, Exception ex)
		{
			// todo: 専用のダイアログ使う？
			App.Models.Get<LogManager>().AddCriticalErrorLog("UnhandledException", ex);
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
		[Obsolete]
		public static void OpenToExplorer(this Application app, string path)
		{
			if (File.Exists(path))
				Process.Start("explorer.exe", "/e, /select, \"" + path + "\"");
			else
				Process.Start("explorer.exe", "/e, \"" + path + "\"");
		}

		// hWndをhWndCenterの中央に配置します
		[Obsolete]
		public static void CenterWindow(this Application app, IntPtr hWnd, IntPtr hWndCenter)
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
	}
}

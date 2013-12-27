using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using __Primitives__;
using WinForms = System.Windows.Forms;

namespace SmartAudioPlayer
{
	public static class UIThreadExtension
	{
		public static void UIThreadInvoke(this Action action)
		{
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
		public static void UIThreadBeginInvoke(this Action action, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			var currentApp = Application.Current;
			if (currentApp == null) return;
			var dispatcher = currentApp.Dispatcher;
			if (dispatcher.HasShutdownStarted) return;
			dispatcher.BeginInvoke(action, priority);
		}
	}

	public static class WinFormsControlExtension
	{
		// hWndをhWndCenterの中央に配置します
		public static void CenterWindow(this WinForms.Control control, IntPtr hWndCenter)
		{
			var hWnd = control.Handle;

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

	public static class OutProcess
	{
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
	}

}

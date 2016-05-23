using Quala.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Quala.WPF.Extensions
{
	public static class WindowExtensions
	{
		// Window Placement API wrapper
		public static void SetWindowPlacement(this Window window, Int32Rect rect)
		{
			var wp = new WinAPI.WINDOWPLACEMENT()
			{
				flags = 0,
				length = Marshal.SizeOf(typeof(WinAPI.WINDOWPLACEMENT)),
				maxPosition = new WinAPI.POINT(),
				minPosition = new WinAPI.POINT(),
				normalPosition = new WinAPI.RECT(
					(int)rect.X,
					(int)rect.Y,
					(int)rect.X + (int)rect.Width,
					(int)rect.Y + (int)rect.Height),
				showCmd = WinAPI.SW.SHOWNORMAL,
			};

			// MEMO: TOOLWINDOWが適用されているとうまく設定されないバグがある？
			//       とりあえず、実行時にWS_EX_TOOLWINDOWだけ取り除いてみる
			var helper = new WindowInteropHelper(window);
			var exstyle = (WinAPI.WS_EX)WinAPI.GetWindowLong(helper.EnsureHandle(), WinAPI.GWL.EXSTYLE);
			var new_exstyle = (exstyle | WinAPI.WS_EX.TOOLWINDOW) ^ WinAPI.WS_EX.TOOLWINDOW;
			WinAPI.SetWindowLong(helper.EnsureHandle(), WinAPI.GWL.EXSTYLE, (IntPtr)new_exstyle);
			WinAPI.SetWindowPlacement(helper.EnsureHandle(), ref wp);
			WinAPI.SetWindowLong(helper.EnsureHandle(), WinAPI.GWL.EXSTYLE, (IntPtr)exstyle);
		}

		public static Int32Rect GetWindowPlacement(this Window window)
		{
			var helper = new WindowInteropHelper(window);
			WinAPI.WINDOWPLACEMENT wp;
			WinAPI.GetWindowPlacement(helper.EnsureHandle(), out wp);
			var ret = new Int32Rect(
				wp.normalPosition.left,
				wp.normalPosition.top,
				wp.normalPosition.right - wp.normalPosition.left,
				wp.normalPosition.bottom - wp.normalPosition.top);
			return ret;
		}

	}
}

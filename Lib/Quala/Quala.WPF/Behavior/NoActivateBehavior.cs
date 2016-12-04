using Quala.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace Quala.WPF.Behavior
{
	/// <summary>
	/// ウィンドウのアクティブ化を防ぐビヘイビア。
	/// 初回変更のみの手抜き実装です
	/// </summary>
	public sealed class NoActivateBehavior : Behavior<Window>
	{
		WindowInteropHelper windowHelper;
		HwndSource hwndSource;

		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.SourceInitialized += OnSourceInitialized;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.SourceInitialized -= OnSourceInitialized;
			if(hwndSource != null && hwndSource.IsDisposed == false)
			{
				hwndSource.RemoveHook(WndProc);
				hwndSource.Dispose();
			}
		}

		void OnSourceInitialized(object sender, EventArgs e)
		{
			windowHelper = new WindowInteropHelper(AssociatedObject);
			hwndSource = HwndSource.FromHwnd(windowHelper.Handle);
			hwndSource.AddHook(WndProc);

			var exstyle = (WinAPI.WS_EX)WinAPI.GetWindowLong(windowHelper.Handle, WinAPI.GWL.EXSTYLE);
			exstyle |= WinAPI.WS_EX.NOACTIVATE;
			WinAPI.SetWindowLong(windowHelper.Handle, WinAPI.GWL.EXSTYLE, (IntPtr)exstyle);
		}

		[DebuggerHidden]
		IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			// MEMO:
			// WS_EX_NOACTIVATEが指定されているときにマウスドラッグでリサイズしようとするとウィンドウが描画されず、
			// マウスを離したときに描画されるという謎現象を解決するためにSetWindowPosし直してごまかす
			if ((WinAPI.WM)msg == WinAPI.WM.MOVING || (WinAPI.WM)msg == WinAPI.WM.SIZING)
			{
				var rc = (WinAPI.RECT)Marshal.PtrToStructure(lParam, typeof(WinAPI.RECT));
				WinAPI.SetWindowPos(hWnd, IntPtr.Zero, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, 0);
				handled = true;
				return IntPtr.Zero;
			}

			// MEMO:
			// マウスクリックされてもアクティブ化しないようにする。
			// ウィンドウスタイルだけだと不足なのかな？
			if ((WinAPI.WM)msg == WinAPI.WM.MOUSEACTIVATE)
			{
				handled = true;
				return (IntPtr)WinAPI.MA.NOACTIVATE;
			}

			return IntPtr.Zero;
		}

	}
}

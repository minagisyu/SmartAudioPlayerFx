using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Quala.Interop.Win32;

namespace SmartAudioPlayerFx.Behavior
{
	/// <summary>
	/// WS_EX_NOACTIVATEが指定されているときにマウスドラッグでリサイズしようとするとウィンドウが描画されず、
	/// マウスを離したときに描画されるという謎現象を解決するためのビヘイビア
	/// </summary>
	class NoActivateResizingBugPatchBehavior : Behavior<Window>
	{
		WindowInteropHelper windowHelper;
		HwndSource hwndSource;

		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.SourceInitialized += OnSourceInitialized;
		}

		void OnSourceInitialized(object sender, EventArgs e)
		{
			windowHelper = new WindowInteropHelper(AssociatedObject);
			hwndSource = HwndSource.FromHwnd(windowHelper.Handle);
			hwndSource.AddHook(WndProc);
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


		[DebuggerHidden]
		IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if ((WM)msg == WM.MOVING || (WM)msg == WM.SIZING)
			{
				var rc = (RECT)Marshal.PtrToStructure(lParam, typeof(RECT));
				API.SetWindowPos(hWnd, IntPtr.Zero, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, 0);
				handled = true;
			}
			return IntPtr.Zero;
		}

	}
}

using Quala.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;

namespace Quala.WPF.Behavior
{
	/// <summary>
	/// WM_NCHITTESTメッセージを処理し
	/// ウィンドウ端から指定位置までをウィンドウのボーダーであるかのようにWindowsに報告する
	/// </summary>
	public class UnvisibleResizeBorderBehavior : Behavior<Window>
	{
		WindowInteropHelper windowHelper;
		HwndSource hwndSource;

		public int? Left { get; set; }
		public int? Top { get; set; }
		public int? Right { get; set; }
		public int? Bottom { get; set; }

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
		}

		[DebuggerHidden]
		IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if ((WinAPI.WM)msg == WinAPI.WM.NCHITTEST)
			{
				// MEMO: HT_CAPTIONを返すとWPFシステムがクライアント領域を見失うので手抜き実装はできない...
				// カーソルの位置がウィンドウのどこの部分？
				WinAPI.RECT rc;
				WinAPI.POINT pt;
				WinAPI.GetWindowRect(hWnd, out rc);
				WinAPI.GetCursorPos(out pt);

				// 左右判定
				var lr = WinAPI.HT.NOWHERE;
				if (Left.HasValue && pt.x >= rc.left && pt.x <= rc.left + Left.Value)
					lr = WinAPI.HT.LEFT;
				else if (Right.HasValue && pt.x <= rc.right && pt.x >= rc.right - Right.Value)
					lr = WinAPI.HT.RIGHT;

				// 上下判定
				var tb = WinAPI.HT.NOWHERE;
				if (Top.HasValue && pt.y >= rc.top && pt.y <= rc.top + Top.Value)
					tb = WinAPI.HT.TOP;
				else if (Bottom.HasValue && pt.y <= rc.bottom && pt.y >= rc.bottom - Bottom.Value)
					tb = WinAPI.HT.BOTTOM;

				// ナナメ判定
				var ret = IntPtr.Zero;
				if (lr == WinAPI.HT.LEFT)
				{
					if (tb == WinAPI.HT.TOP)
						ret = (IntPtr)WinAPI.HT.TOPLEFT;
					else if (tb == WinAPI.HT.BOTTOM)
						ret = (IntPtr)WinAPI.HT.BOTTOMLEFT;
					else
						ret = (IntPtr)WinAPI.HT.LEFT;
				}
				else if (lr == WinAPI.HT.RIGHT)
				{
					if (tb == WinAPI.HT.TOP)
						ret = (IntPtr)WinAPI.HT.TOPRIGHT;
					else if (tb == WinAPI.HT.BOTTOM)
						ret = (IntPtr)WinAPI.HT.BOTTOMRIGHT;
					else
						ret = (IntPtr)WinAPI.HT.RIGHT;
				}
				else if (tb == WinAPI.HT.TOP || tb == WinAPI.HT.BOTTOM)
				{
					ret = (IntPtr)tb;
				}
				else
				{
					ret = (IntPtr)WinAPI.HT.CLIENT;
				}

				handled = true;
				return ret;
			}
			return IntPtr.Zero;
		}

	}
}

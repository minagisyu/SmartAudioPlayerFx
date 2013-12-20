using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Quala.Interop.Win32;

namespace SmartAudioPlayerFx.Behavior
{
	/// <summary>
	/// WM_NCHITTESTメッセージを処理し
	/// ウィンドウ端から指定位置までをウィンドウのボーダーであるかのようにWindowsに報告する
	/// </summary>
	class UnvisibleResizeBorderBehavior : Behavior<Window>
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
			if ((WM)msg == WM.NCHITTEST)
			{
				// MEMO: HT_CAPTIONを返すとWPFシステムがクライアント領域を見失うので手抜き実装はできない...
				// カーソルの位置がウィンドウのどこの部分？
				RECT rc;
				POINT pt;
				API.GetWindowRect(hWnd, out rc);
				API.GetCursorPos(out pt);

				// 左右判定
				var lr = HT.NOWHERE;
				if (Left.HasValue && pt.x >= rc.left && pt.x <= rc.left + Left.Value)
					lr = HT.LEFT;
				else if (Right.HasValue && pt.x <= rc.right && pt.x >= rc.right - Right.Value)
					lr = HT.RIGHT;

				// 上下判定
				var tb = HT.NOWHERE;
				if (Top.HasValue && pt.y >= rc.top && pt.y <= rc.top + Top.Value)
					tb = HT.TOP;
				else if (Bottom.HasValue && pt.y <= rc.bottom && pt.y >= rc.bottom - Bottom.Value)
					tb = HT.BOTTOM;

				// ナナメ判定
				var ret = IntPtr.Zero;
				if (lr == HT.LEFT)
				{
					if (tb == HT.TOP)
						ret = (IntPtr)HT.TOPLEFT;
					else if (tb == HT.BOTTOM)
						ret = (IntPtr)HT.BOTTOMLEFT;
					else
						ret = (IntPtr)HT.LEFT;
				}
				else if (lr == HT.RIGHT)
				{
					if (tb == HT.TOP)
						ret = (IntPtr)HT.TOPRIGHT;
					else if (tb == HT.BOTTOM)
						ret = (IntPtr)HT.BOTTOMRIGHT;
					else
						ret = (IntPtr)HT.RIGHT;
				}
				else if (tb == HT.TOP || tb == HT.BOTTOM)
				{
					ret = (IntPtr)tb;
				}
				else
				{
					ret = (IntPtr)HT.CLIENT;
				}

				handled = true;
				return ret;
			}
			return IntPtr.Zero;
		}

	}
}

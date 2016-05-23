using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Quala.Interop.Win32;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SmartAudioPlayerFx.Behavior
{
	/// <summary>
	/// ウィンドウをドラッグで移動させるビヘイビア
	/// 画面/ウィンドウ端吸着(スナップ)機能付き(Shiftキーで解除)
	/// </summary>
	class WindowDragMoveBehavior : Behavior<Window>
	{
		WindowInteropHelper windowHelper;
		public bool IsSnapped { get; set; }

		// 基準にする要素(ルート要素等)
		public FrameworkElement RelativeObject
		{
			get { return (FrameworkElement)GetValue(RelativeObjectProperty); }
			set { SetValue(RelativeObjectProperty, value); }
		}

		public static readonly DependencyProperty RelativeObjectProperty =
			DependencyProperty.Register("RelativeObject", typeof(FrameworkElement), typeof(WindowDragMoveBehavior));

		// 吸着させないウィンドウハンドル
		public IntPtr ExcludeSnapHWND
		{
			get { return (IntPtr)GetValue(ExcludeSnapHWNDProperty); }
			set { SetValue(ExcludeSnapHWNDProperty, value); }
		}

		public static readonly DependencyProperty ExcludeSnapHWNDProperty =
			DependencyProperty.Register("ExcludeSnapHWND", typeof(IntPtr), typeof(WindowDragMoveBehavior), new UIPropertyMetadata(IntPtr.Zero));

		public WindowDragMoveBehavior()
		{
			IsSnapped = true;
		}

		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.SourceInitialized += OnSourceInitialized;
			AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
			AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
			AssociatedObject.MouseMove += OnMouseMove;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.SourceInitialized -= OnSourceInitialized;
			AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
			AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
			AssociatedObject.MouseMove -= OnMouseMove;
		}

		void OnSourceInitialized(object sender, EventArgs e)
		{
			windowHelper = new WindowInteropHelper(AssociatedObject);
		}

		Point? dragStartPos;

		void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var relative = RelativeObject ?? sender as Window;
			dragStartPos = e.GetPosition(relative);
			Mouse.Capture(relative);
		}

		void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			dragStartPos = null;
			Mouse.Capture(null);
		}

		void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			var window = sender as Window;
			var relative = RelativeObject ?? window;

			if (dragStartPos.HasValue)
			{
				// スクリーン座標
				var windowLocation = new Point(window.Left, window.Top);
				var contentLocation = relative.PointToScreen(new Point(0, 0));

				// マウスドラッグによるWindowの移動計算
				Point pos = e.GetPosition(relative);
				Vector moved = pos - dragStartPos.Value;
				windowLocation += moved;
				contentLocation += moved;

				// Shiftキーの押下(吸着無効)
				var shiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
				var rrc = new RECT((int)windowLocation.X, (int)windowLocation.Y, (int)(windowLocation.X + window.Width), (int)(windowLocation.Y + window.Height));
				if (IsSnapped && !shiftDown)
					SnapWindow(windowHelper.Handle, ref rrc, 10, ExcludeSnapHWND);
				API.SetWindowPos(windowHelper.Handle, IntPtr.Zero, rrc.left, rrc.top, 0, 0, SWP.NOSIZE | SWP.NOZORDER);
			}
		}

		// Window吸着処理
		class SnapWindowInfo
		{
			public IntPtr hwnd;
			public RECT rcOriginal;
			public RECT rcNearest;
			public IntPtr hwndExclude;
		}

		static void SnapWindow(IntPtr hWnd, ref RECT prc, int margin, IntPtr hWndExclude)
		{
			var hMonitor = API.MonitorFromWindow(hWnd, MONITOR_DEFAULTTO.NEAREST);
			RECT xrc;
			var info = new SnapWindowInfo();
			int xoffset, yoffset;

			if (hMonitor != IntPtr.Zero)
			{
				var mi = new MONITORINFO();
				mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
				API.GetMonitorInfo(hMonitor, ref mi);
				xrc = mi.rcMonitor;
			}
			else
			{
				xrc.left = 0;
				xrc.top = 0;
				xrc.right = API.GetSystemMetrics(SM.CXSCREEN);
				xrc.bottom = API.GetSystemMetrics(SM.CYSCREEN);
			}
			//
			info.hwnd = hWnd;
			info.rcOriginal = prc;
			info.rcNearest.left = xrc.left - prc.left;
			info.rcNearest.top = xrc.top - prc.top;
			info.rcNearest.right = xrc.right - prc.right;
			info.rcNearest.bottom = xrc.bottom - prc.bottom;
			info.hwndExclude = hWndExclude;
			API.EnumWindows(new WNDENUMPROC((hwnd, lparam) =>
			{
				if (API.IsWindowVisible(hwnd) && hwnd != info.hwnd && hwnd != info.hwndExclude)
				{
					RECT rc, rcEdge;

					API.GetWindowRect(hwnd, out rc);
					if (rc.right > rc.left && rc.bottom > rc.top)
					{
						if (rc.top < info.rcOriginal.bottom && rc.bottom > info.rcOriginal.top)
						{
							if (Math.Abs(rc.left - info.rcOriginal.right) < Math.Abs(info.rcNearest.right))
							{
								rcEdge.left = rc.left;
								rcEdge.right = rc.left;
								rcEdge.top = Math.Max(rc.top, info.rcOriginal.top);
								rcEdge.bottom = Math.Min(rc.bottom, info.rcOriginal.bottom);
								if (IsWindowEdgeVisible(hwnd, API.GetTopWindow(API.GetDesktopWindow()), rcEdge, info.hwnd))
									info.rcNearest.right = rc.left - info.rcOriginal.right;
							}
							if (Math.Abs(rc.right - info.rcOriginal.left) < Math.Abs(info.rcNearest.left))
							{
								rcEdge.left = rc.right;
								rcEdge.right = rc.right;
								rcEdge.top = Math.Max(rc.top, info.rcOriginal.top);
								rcEdge.bottom = Math.Min(rc.bottom, info.rcOriginal.bottom);
								if (IsWindowEdgeVisible(hwnd, API.GetTopWindow(API.GetDesktopWindow()), rcEdge, info.hwnd))
									info.rcNearest.left = rc.right - info.rcOriginal.left;
							}
						}
						if (rc.left < info.rcOriginal.right && rc.right > info.rcOriginal.left)
						{
							if (Math.Abs(rc.top - info.rcOriginal.bottom) < Math.Abs(info.rcNearest.bottom))
							{
								rcEdge.left = Math.Max(rc.left, info.rcOriginal.left);
								rcEdge.right = Math.Min(rc.right, info.rcOriginal.right);
								rcEdge.top = rc.top;
								rcEdge.bottom = rc.top;
								if (IsWindowEdgeVisible(hwnd, API.GetTopWindow(API.GetDesktopWindow()), rcEdge, info.hwnd))
									info.rcNearest.bottom = rc.top - info.rcOriginal.bottom;
							}
							if (Math.Abs(rc.bottom - info.rcOriginal.top) < Math.Abs(info.rcNearest.top))
							{
								rcEdge.left = Math.Max(rc.left, info.rcOriginal.left);
								rcEdge.right = Math.Min(rc.right, info.rcOriginal.right);
								rcEdge.top = rc.bottom;
								rcEdge.bottom = rc.bottom;
								if (IsWindowEdgeVisible(hwnd, API.GetTopWindow(API.GetDesktopWindow()), rcEdge, info.hwnd))
									info.rcNearest.top = rc.bottom - info.rcOriginal.top;
							}
						}
					}
				}
				return true;
			}), IntPtr.Zero);
			if (Math.Abs(info.rcNearest.left) < Math.Abs(info.rcNearest.right)
				|| info.rcNearest.left == info.rcNearest.right)
				xoffset = info.rcNearest.left;
			else if (Math.Abs(info.rcNearest.left) > Math.Abs(info.rcNearest.right))
				xoffset = info.rcNearest.right;
			else
				xoffset = 0;
			if (Math.Abs(info.rcNearest.top) < Math.Abs(info.rcNearest.bottom)
				|| info.rcNearest.top == info.rcNearest.bottom)
				yoffset = info.rcNearest.top;
			else if (Math.Abs(info.rcNearest.top) > Math.Abs(info.rcNearest.bottom))
				yoffset = info.rcNearest.bottom;
			else
				yoffset = 0;
			if (Math.Abs(xoffset) <= margin)
				prc.left += xoffset;
			if (Math.Abs(yoffset) <= margin)
				prc.top += yoffset;
			prc.right = prc.left + (info.rcOriginal.right - info.rcOriginal.left);
			prc.bottom = prc.top + (info.rcOriginal.bottom - info.rcOriginal.top);
		}

		static bool IsWindowEdgeVisible(IntPtr hwnd, IntPtr hwndTop, RECT pRect, IntPtr hwndTarget)
		{
			RECT rc, rcEdge;
			IntPtr hwndNext;

			if (hwndTop == hwnd || hwndTop == IntPtr.Zero)
				return true;

			API.GetWindowRect(hwndTop, out rc);
			hwndNext = API.GetWindow(hwndTop, GW.HWNDNEXT);
			if (hwndTop == hwndTarget || !API.IsWindowVisible(hwndTop) || rc.left == rc.right || rc.top == rc.bottom)
				return IsWindowEdgeVisible(hwnd, hwndNext, pRect, hwndTarget);
			if (pRect.top == pRect.bottom)
			{
				if (rc.top <= pRect.top && rc.bottom > pRect.top)
				{
					if (rc.left <= pRect.left && rc.right >= pRect.right)
						return false;
					if (rc.left <= pRect.left && rc.right > pRect.left)
					{
						rcEdge = pRect;
						rcEdge.right = Math.Min(rc.right, pRect.right);
						return IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget);
					}
					else if (rc.left > pRect.left && rc.right >= pRect.right)
					{
						rcEdge = pRect;
						rcEdge.left = rc.left;
						return IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget);
					}
					else if (rc.left > pRect.left && rc.right < pRect.right)
					{
						rcEdge = pRect;
						rcEdge.right = rc.left;
						if (IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget))
							return true;
						rcEdge.left = rc.right;
						rcEdge.right = pRect.right;
						return IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget);
					}
				}
			}
			else
			{
				if (rc.left <= pRect.left && rc.right > pRect.left)
				{
					if (rc.top <= pRect.top && rc.bottom >= pRect.bottom)
						return false;
					if (rc.top <= pRect.top && rc.bottom > pRect.top)
					{
						rcEdge = pRect;
						rcEdge.bottom = Math.Min(rc.bottom, pRect.bottom);
						return IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget);
					}
					else if (rc.top > pRect.top && rc.bottom >= pRect.bottom)
					{
						rcEdge = pRect;
						rcEdge.top = rc.top;
						return IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget);
					}
					else if (rc.top > pRect.top && rc.bottom < pRect.bottom)
					{
						rcEdge = pRect;
						rcEdge.bottom = rc.top;
						if (IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget))
							return true;
						rcEdge.top = rc.bottom;
						rcEdge.bottom = pRect.bottom;
						return IsWindowEdgeVisible(hwnd, hwndNext, rcEdge, hwndTarget);
					}
				}
			}
			return IsWindowEdgeVisible(hwnd, hwndNext, pRect, hwndTarget);
		}

	}
}

using System;
using System.Windows;
using Quala.Interop.Win32;
using System.Windows.Interop;
using System.Windows.Media;

namespace Quala.Windows
{
	[Obsolete]
	public static class WindowBehavior
	{
		#region DwmExtendFrame

		public static Thickness GetDwmExtendFrame(Window window)
		{
			return (Thickness)window.GetValue(DwmExtendFrameProperty);
		}
		public static void SetDwmExtendFrame(Window window, Thickness value)
		{
			window.SetValue(DwmExtendFrameProperty, value);
		}

		public static readonly DependencyProperty DwmExtendFrameProperty =
			DependencyProperty.RegisterAttached(
				"DwmExtendFrame",
				typeof(Thickness),
				typeof(WindowBehavior),
				new UIPropertyMetadata(new Thickness(), OnDwmExtendFrameChanged));

		static void OnDwmExtendFrameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var window = d as Window;
			if (window == null) return;
			if (e.NewValue is Thickness == false) return;
			UpdateDwmExtendFrame(window, (Thickness)e.NewValue);
		}

		static void UpdateDwmExtendFrame(Window window, Thickness t)
		{
			var version = Environment.OSVersion;
			if (version.Platform != PlatformID.Win32NT || version.Version.Major < 6) return;
			if (API.DwmIsCompositionEnabled() == false) return;

			var helper = new WindowInteropHelper(window);
			var action = new Action(() =>
			{
				var hWnd = helper.Handle;

				// WPFとWin32部の背景を透明に設定
				window.Background = Brushes.Transparent;
				HwndSource.FromHwnd(hWnd).CompositionTarget.BackgroundColor = Colors.Transparent;

				var m = new MARGINS();
				m.cxLeftWidth = (int)t.Left;
				m.cyTopHeight = (int)t.Top;
				m.cxRightWidth = (int)t.Right;
				m.cyBottomHeight = (int)t.Bottom;
				API.DwmExtendFrameIntoClientArea(hWnd, ref m);
			});

			// ウィンドウ作られてないっぽいから作られてからやる
			if (helper.Handle == IntPtr.Zero)
			{
				window.SourceInitialized += delegate { action(); };
				return;
			}

			action();
		}

		#endregion
		#region Bounds

		public static WindowLocation GetBounds(Window window)
		{
			var value = window.GetValue(BoundsProperty);
			if(value == null)
			{
				// initialize
				window.LocationChanged += UpdateWindowBounds;
				window.SizeChanged += UpdateWindowBounds;
				window.Closed += window_Closed;
				window.SetValue(BoundsProperty, value);
			}
			return (WindowLocation)value;
		}

		static void window_Closed(object sender, EventArgs e)
		{
			var window = sender as Window;
			if(window == null) return;
			window.Closed -= window_Closed;
			window.LocationChanged -= UpdateWindowBounds;
			window.SizeChanged -= UpdateWindowBounds;
		}

		static void UpdateWindowBounds(object sender, EventArgs e)
		{
			var window = sender as Window;
			if(window == null) return;
			window.SetValue(BoundsProperty, new WindowLocation(
				new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight)));
		}

		public static void SetBounds(Window window, WindowLocation value)
		{
			// value == null -> throw
			window.SetValue(BoundsProperty, value);
		}

		public static readonly DependencyProperty BoundsProperty =
			DependencyProperty.RegisterAttached(
				"Bounds",
				typeof(WindowLocation),
				typeof(WindowBehavior),
				new PropertyMetadata(null, OnBoundsChanged));

		static void OnBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var window = d as Window;
			if (window == null) return;
			if (e.NewValue is WindowLocation == false) return;
		//	UpdateDwmExtendFrame(window, (Thickness)e.NewValue);
		}

		#endregion

	}
}

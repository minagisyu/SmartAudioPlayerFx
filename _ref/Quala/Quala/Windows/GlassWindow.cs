using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using Quala.Interop.Win32;

namespace Quala.Windows
{
	public class GlassWindow : Window
	{
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			// クライアントの全領域をグラス化する
			var version = Environment.OSVersion;
			if (version.Platform != PlatformID.Win32NT || version.Version.Major < 6) return;
			if (API.DwmIsCompositionEnabled() == false) 	return;

			var hWnd = new WindowInteropHelper(this).Handle;
			if (hWnd == IntPtr.Zero) return;

			// WPFとWin32部の背景を透明に設定
			this.Background = Brushes.Transparent;
			HwndSource.FromHwnd(hWnd).CompositionTarget.BackgroundColor = Colors.Transparent;

			var margins = new MARGINS() { cxLeftWidth=-1, cyTopHeight=-1, cxRightWidth=-1, cyBottomHeight=-1, };
			API.DwmExtendFrameIntoClientArea(hWnd, ref margins);
		}

	}
}

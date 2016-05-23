using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Quala.Win32
{
	using HWND = System.IntPtr;

	partial class WinAPI
	{
		const string Dwmapi = "dwmapi.dll";

		[DllImport(Dwmapi, PreserveSig = false)]
		public static extern void DwmExtendFrameIntoClientArea(HWND hWnd, ref MARGINS pMarInset);

		[DllImport(Dwmapi, PreserveSig = false)]
		public static extern bool DwmIsCompositionEnabled();

		[StructLayout(LayoutKind.Sequential)]
		public struct MARGINS
		{
			public int cxLeftWidth;
			public int cxRightWidth;
			public int cyTopHeight;
			public int cyBottomHeight;
		}
	}
}

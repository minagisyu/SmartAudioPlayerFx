using System;
using System.Runtime.InteropServices;

namespace Quala.Win32
{
	using HWND = System.IntPtr;

	partial class WinAPI
	{
		/****** USER Macro APIs ******************************************************/
		public static WS GetWindowStyle(HWND hWnd)
		{
			return (WS)GetWindowLong(hWnd, GWL.STYLE);
		}
		public static WS_EX GetWindowExStyle(HWND hWnd)
		{
			return (WS_EX)GetWindowLong(hWnd, GWL.EXSTYLE);
		}
	}
}

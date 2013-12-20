using System;
using System.Runtime.InteropServices;

namespace Quala.Interop.Win32
{
	using HWND = System.IntPtr;

	partial class API
	{
		/****** USER Macro APIs ******************************************************/
		public static WS GetWindowStyle(HWND hWnd)
		{
			return (WS)API.GetWindowLong(hWnd, GWL.STYLE);
		}
		public static WS_EX GetWindowExStyle(HWND hWnd)
		{
			return (WS_EX)GetWindowLong(hWnd, GWL.EXSTYLE);
		}
	}
}

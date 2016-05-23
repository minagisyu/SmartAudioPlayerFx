using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Quala.Win32
{
	using HIMAGELIST = System.IntPtr;
	using HINSTANCE = System.IntPtr;
	using COLORREF = System.UInt32;
	using UINT = System.UInt32;
	using HICON = System.IntPtr;

	partial class WinAPI
	{
		const string ComCtl32 = "comctl32.dll";
		public const UINT IMAGE_BITMAP = 0;
		public const UINT IMAGE_ICON = 1;
		public const UINT IMAGE_CURSOR = 2;
		public const UINT IMAGE_ENHMETAFILE = 3;

		[DllImport(ComCtl32)]
		public static extern bool ImageList_Destroy(HIMAGELIST himl);

		[DllImport(ComCtl32)]
		public static extern bool ImageList_Draw(HIMAGELIST himl, int i, IntPtr hdcDst, int x, int y, ILD fStyle);

		[DllImport(ComCtl32)]
		public static extern HICON ImageList_GetIcon(HIMAGELIST himl, int i, ILD flags);

		public static HIMAGELIST ImageList_LoadBitmap(HINSTANCE hi, string lpbmp, int cx, int cGrow, COLORREF crMask)
		{
			return ImageList_LoadImage(hi, lpbmp, cx, cGrow, crMask, IMAGE_BITMAP, 0);
		}

		[DllImport(ComCtl32)]
		public static extern HIMAGELIST ImageList_LoadImage(HINSTANCE hi, string lpbmp, int cx, int cGrow, COLORREF crMask, UINT uType, UINT uFlags);

		[DllImport(ComCtl32)]
		public static extern void InitCommonControls();

		[Flags]
		public enum ILD : uint
		{
			NORMAL = 0x00000000,
			TRANSPARENT = 0x00000001,
			MASK = 0x00000010,
			IMAGE = 0x00000020,
			ROP = 0x00000040,
			BLEND25 = 0x00000002,
			BLEND50 = 0x00000004,
			OVERLAYMASK = 0x00000F00,
			//#define INDEXTOOVERLAYMASK(i)   ((i) << 8)
			PRESERVEALPHA = 0x00001000,  // This preserves the alpha channel in dest
			SCALE = 0x00002000, // Causes the image to be scaled to cx, cy instead of clipped
			DPISCALE = 0x00004000,
			ASYNC = 0x00008000,

			SELECTED = BLEND50,
			FOCUS = BLEND25,
			BLEND = BLEND50,
		}
	}
}

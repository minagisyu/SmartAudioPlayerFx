using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Quala.Win32
{
	using HDC = System.IntPtr;
	using HFONT = System.IntPtr;
	using COLORREF = System.UInt32;
	using DWORD = System.UInt32;

	partial class WinAPI
	{
		const string Gdi32 = "gdi32.dll";

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern HDC CreateCompatibleDC(HDC hdc);

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern HFONT CreateFont(
			int cHeight, int cWidth, int cEscapement, int cOrientation, FW cWeight, DWORD bItalic,
			DWORD bUnderline, DWORD bStrikeOut, CHARSET iCharSet, OUT iOutPrecision, CLIP iClipPrecision,
			QUALITY iQuality, PITCH iPitchAndFamily, string pszFaceName);

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern bool DeleteObject(IntPtr ho);

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern bool DeleteDC(HDC hdc);

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern bool OffsetWindowOrgEx(HDC hdc, int nXOffset, int nYOffset, ref POINT lpPoint);

		public static COLORREF RGB(byte r, byte g, byte b)
		{
			return (COLORREF)(r | (g << 8) | (b << 16));
		}

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern IntPtr SelectObject(HDC hdc, IntPtr h);

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern int SetBkMode(HDC hdc, BKMODE mode);

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern COLORREF SetTextColor(HDC hdc, COLORREF color);

		[DllImport(Gdi32, CharSet = CharSet.Auto)]
		public static extern bool SetWindowOrgEx(HDC hdc, int X, int Y, ref POINT lpPoint);


		/// <summary>
		/// Background Modes
		/// </summary>
		public enum BKMODE
		{
			TRANSPARENT = 1,
			OPAQUE = 2,
			LAST = 2,
		}

		public enum CHARSET : uint
		{
			ANSI = 0,
			DEFAULT = 1,
			SYMBOL = 2,
			SHIFTJIS = 128,
			HANGEUL = 129,
			HANGUL = 129,
			GB2312 = 134,
			CHINESEBIG5 = 136,
			OEM = 255,
			JOHAB = 130,
			HEBREW = 177,
			ARABIC = 178,
			GREEK = 161,
			TURKISH = 162,
			VIETNAMESE = 163,
			THAI = 222,
			EASTEUROPE = 238,
			RUSSIAN = 204,

			MAC = 77,
			BALTIC = 186,
		}

		/// <summary>
		/// Font Weights
		/// </summary>
		public enum FW : int
		{
			DONTCARE = 0,
			THIN = 100,
			EXTRALIGHT = 200,
			LIGHT = 300,
			NORMAL = 400,
			MEDIUM = 500,
			SEMIBOLD = 600,
			BOLD = 700,
			EXTRABOLD = 800,
			HEAVY = 900,

			ULTRALIGHT = EXTRALIGHT,
			REGULAR = NORMAL,
			DEMIBOLD = SEMIBOLD,
			ULTRABOLD = EXTRABOLD,
			BLACK = HEAVY,
		}

		public enum OUT : uint
		{
			DEFAULT_PRECIS = 0,
			STRING_PRECIS = 1,
			CHARACTER_PRECIS = 2,
			STROKE_PRECIS = 3,
			TT_PRECIS = 4,
			DEVICE_PRECIS = 5,
			RASTER_PRECIS = 6,
			TT_ONLY_PRECIS = 7,
			OUTLINE_PRECIS = 8,
			SCREEN_OUTLINE_PRECIS = 9,
			PS_ONLY_PRECIS = 10,
		}

		public enum CLIP : uint
		{
			DEFAULT_PRECIS = 0,
			CHARACTER_PRECIS = 1,
			STROKE_PRECIS = 2,
			MASK = 0xf,
			LH_ANGLES = (1 << 4),
			TT_ALWAYS = (2 << 4),
			DFA_DISABLE = (4 << 4),
			EMBEDDED = (8 << 4),
		}

		/// <summary>
		/// Standard Cursor IDs
		/// </summary>
		public static class IDC
		{
			public static readonly IntPtr ARROW = MAKEINTRESOURCE(32512);
			public static readonly IntPtr IBEAM = MAKEINTRESOURCE(32513);
			public static readonly IntPtr WAIT = MAKEINTRESOURCE(32514);
			public static readonly IntPtr CROSS = MAKEINTRESOURCE(32515);
			public static readonly IntPtr UPARROW = MAKEINTRESOURCE(32516);
			public static readonly IntPtr SIZE = MAKEINTRESOURCE(32640);	// OBSOLETE: use IDC_SIZEALL
			public static readonly IntPtr ICON = MAKEINTRESOURCE(32641);	// OBSOLETE: use IDC_ARROW
			public static readonly IntPtr SIZENWSE = MAKEINTRESOURCE(32642);
			public static readonly IntPtr SIZENESW = MAKEINTRESOURCE(32643);
			public static readonly IntPtr SIZEWE = MAKEINTRESOURCE(32644);
			public static readonly IntPtr SIZENS = MAKEINTRESOURCE(32645);
			public static readonly IntPtr SIZEALL = MAKEINTRESOURCE(32646);
			public static readonly IntPtr NO = MAKEINTRESOURCE(32648);		// not in win3.1
			public static readonly IntPtr HAND = MAKEINTRESOURCE(32649);
			public static readonly IntPtr APPSTARTING = MAKEINTRESOURCE(32650);	// not in win3.1
			public static readonly IntPtr HELP = MAKEINTRESOURCE(32651);
		}

		public enum PITCH : uint
		{
			DEFAULT = 0,
			FIXED = 1,
			VARIABLE = 2,
			MONO_FONT = 8,
		}

		public enum QUALITY : uint
		{
			DEFAULT = 0,
			DRAFT = 1,
			PROOF = 2,
			NONANTIALIASED = 3,
			ANTIALIASED = 4,
			CLEARTYPE = 5,
			CLEARTYPE_NATURAL = 6,
		}

	}
}

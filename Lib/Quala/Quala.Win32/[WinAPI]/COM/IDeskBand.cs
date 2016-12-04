using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Quala.Win32
{
	partial class WinAPI
	{
		partial class COM
		{
			[ComImport]
			[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			[Guid("EB0FE172-1A3A-11D0-89B3-00A0C90A90AC")]
			[SuppressUnmanagedCodeSecurity]
			public interface IDeskBand
			{
				// IOleWindow
				void GetWindow(out IntPtr phWnd);
				void ContextSensitiveHelp(bool fEnterMode);

				// IDockingWindow
				void ShowDW(bool fShow);
				void CloseDW(int dwReserved);
				void ResizeBorderDW(
					ref RECT prcBorder,
					[MarshalAs(UnmanagedType.IUnknown)]
			object punkToolbarSite,
					bool fReserved);

				// IDeskBand
				void GetBandInfo(int dwBandID, DBIF dwViewMode, ref DESKBANDINFO dbi);
			}


			[Flags]
			public enum DBIF : int
			{
				VIEWMODE_NORMAL = 0x0000,
				VIEWMODE_VERTICAL = 0x0001,
				VIEWMODE_FLOATING = 0x0002,
				VIEWMODE_TRANSPARENT = 0x0004,
			}

			[Flags]
			public enum DBIM : int
			{
				MINSIZE = 0x0001,
				MAXSIZE = 0x0002,
				INTEGRAL = 0x0004,
				ACTUAL = 0x0008,
				TITLE = 0x0010,
				MODEFLAGS = 0x0020,
				BKCOLOR = 0x0040,
			}

			[Flags]
			public enum DBIMF : int
			{
				NORMAL = 0x0000,
				FIXED = 0x0001,
				FIXEDBMP = 0x0004,
				VARIABLEHEIGHT = 0x0008,
				UNDELETEABLE = 0x0010,
				DEBOSSED = 0x0020,
				BKCOLOR = 0x0040,
				USECHEVRON = 0x0080,
				BREAK = 0x0100,
				ADDTOFRONT = 0x0200,
				TOPALIGN = 0x0400,
			}


			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct DESKBANDINFO
			{
				public DBIM dwMask;
				public POINT ptMinSize;
				public POINT ptMaxSize;
				public POINT ptIntegral;
				public POINT ptActual;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
				public string wszTitle;
				public DBIMF dwModeFlags;
				public int crBkgnd;
			}
		}
	}
}

using System;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace __Primitives__
{
	partial class WinAPI
	{
		[Flags]
		public enum AW : int
		{
			HOR_POSITIVE = 0x00000001,
			HOR_NEGATIVE = 0x00000002,
			VER_POSITIVE = 0x00000004,
			VER_NEGATIVE = 0x00000008,
			CENTER = 0x00000010,
			HIDE = 0x00010000,
			ACTIVATE = 0x00020000,
			SLIDE = 0x00040000,
			BLEND = 0x00080000,
		}

		[Flags]
		public enum CDDS : int
		{
			PREPAINT = 0x00000001,
			POSTPAINT = 0x00000002,
			PREERASE = 0x00000003,
			POSTERASE = 0x00000004,
			ITEM = 0x00010000,
			ITEMPREPAINT = (CDDS.ITEM | CDDS.PREPAINT),
			ITEMPOSTPAINT = (CDDS.ITEM | CDDS.POSTPAINT),
			ITEMPREERASE = (CDDS.ITEM | CDDS.PREERASE),
			ITEMPOSTERASE = (CDDS.ITEM | CDDS.POSTERASE),
			SUBITEM = 0x00020000,
		}

		[Flags]
		public enum CDIS : int
		{
			SELECTED = 0x0001,
			GRAYED = 0x0002,
			DISABLED = 0x0004,
			CHECKED = 0x0008,
			FOCUS = 0x0010,
			DEFAULT = 0x0020,
			HOT = 0x0040,
			MARKED = 0x0080,
			INDETERMINATE = 0x0100,
			SHOWKEYBOARDCUES = 0x0200,
		}

		[Flags]
		public enum CDRF : int
		{
			DODEFAULT = 0x00000000,
			NEWFONT = 0x00000002,
			SKIPDEFAULT = 0x00000004,
			NOTIFYPOSTPAINT = 0x00000010,
			NOTIFYITEMDRAW = 0x00000020,
			NOTIFYSUBITEMDRAW = 0x00000020,
			NOTIFYPOSTERASE = 0x00000040,
		}

		[Flags]
		public enum DFC : int
		{
			CAPTION = 1,
			MENU = 2,
			SCROLL = 3,
			BUTTON = 4,
			POPUPMENU = 5,
		}

		[Flags]
		public enum DFCS : int
		{
			CAPTIONCLOSE = 0x0000,
			CAPTIONMIN = 0x0001,
			CAPTIONMAX = 0x0002,
			CAPTIONRESTORE = 0x0003,
			CAPTIONHELP = 0x0004,
			MENUARROW = 0x0000,
			MENUCHECK = 0x0001,
			MENUBULLET = 0x0002,
			MENUARROWRIGHT = 0x0004,
			SCROLLUP = 0x0000,
			SCROLLDOWN = 0x0001,
			SCROLLLEFT = 0x0002,
			SCROLLRIGHT = 0x0003,
			SCROLLCOMBOBOX = 0x0005,
			SCROLLSIZEGRIP = 0x0008,
			SCROLLSIZEGRIPRIGHT = 0x0010,
			BUTTONCHECK = 0x0000,
			BUTTONRADIOIMAGE = 0x0001,
			BUTTONRADIOMASK = 0x0002,
			BUTTONRADIO = 0x0004,
			BUTTON3STATE = 0x0008,
			BUTTONPUSH = 0x0010,
			INACTIVE = 0x0100,
			PUSHED = 0x0200,
			CHECKED = 0x0400,
			TRANSPARENT = 0x0800,
			HOT = 0x1000,
			ADJUSTRECT = 0x2000,
			FLAT = 0x4000,
			MONO = 0x8000,
		}

		[Flags]
		public enum FE : int
		{
			FONTSMOOTHINGSTANDARD = 0x0001,
			FONTSMOOTHINGCLEARTYPE = 0x0002,
			FONTSMOOTHINGDOCKING = 0x8000,
			FONTSMOOTHINGORIENTATIONBGR = 0x0000,
			FONTSMOOTHINGORIENTATIONRGB = 0x0001,
		}

		[Flags]
		public enum HT : int
		{
			ERROR = (-2),
			TRANSPARENT = (-1),
			NOWHERE = 0,
			CLIENT = 1,
			CAPTION = 2,
			SYSMENU = 3,
			GROWBOX = 4,
			SIZE = HT.GROWBOX,
			MENU = 5,
			HSCROLL = 6,
			VSCROLL = 7,
			MINBUTTON = 8,
			MAXBUTTON = 9,
			LEFT = 10,
			RIGHT = 11,
			TOP = 12,
			TOPLEFT = 13,
			TOPRIGHT = 14,
			BOTTOM = 15,
			BOTTOMLEFT = 16,
			BOTTOMRIGHT = 17,
			BORDER = 18,
			REDUCE = HT.MINBUTTON,
			ZOOM = HT.MAXBUTTON,
			SIZEFIRST = HT.LEFT,
			SIZELAST = HT.BOTTOMRIGHT,
			OBJECT = 19,
			CLOSE = 20,
			HELP = 21,
		}

		/// <summary>
		/// Flag for WM_MOUSEACTIVE
		/// </summary>
		[Flags]
		public enum MA : int
		{
			/// <summary>
			/// Activates the window, and does not discard the mouse message.
			/// </summary>
			ACTIVATE = 1,

			/// <summary>
			/// Activates the window, and discards the mouse message.
			/// </summary>
			ACTIVATEANDEAT = 2,

			/// <summary>
			/// Does not activate the window, and does not discard the mouse message.
			/// </summary>
			NOACTIVATE = 3,

			/// <summary>
			/// Does not activate the window, but discards the mouse message.
			/// </summary>
			NOACTIVATEANDEAT = 4,
		}

		[Flags]
		public enum MK : short
		{
			LBUTTON = 0x0001,
			RBUTTON = 0x0002,
			SHIFT = 0x0004,
			CONTROL = 0x0008,
			MBUTTON = 0x0010,
			XBUTTON1 = 0x0020,
			XBUTTON2 = 0x0040,
		}

		[Flags]
		public enum NM : int
		{
			CUSTOMDRAW = (-12),
		}

		// System Menu Command Values
		public enum SC : uint
		{
			SIZE = 0xF000,
			MOVE = 0xF010,
			MINIMIZE = 0xF020,
			MAXIMIZE = 0xF030,
			NEXTWINDOW = 0xF040,
			PREVWINDOW = 0xF050,
			CLOSE = 0xF060,
			VSCROLL = 0xF070,
			HSCROLL = 0xF080,
			MOUSEMENU = 0xF090,
			KEYMENU = 0xF100,
			ARRANGE = 0xF110,
			RESTORE = 0xF120,
			TASKLIST = 0xF130,
			SCREENSAVE = 0xF140,
			HOTKEY = 0xF150,
			DEFAULT = 0xF160,
			MONITORPOWER = 0xF170,
			CONTEXTHELP = 0xF180,
			SEPARATOR = 0xF00F,
			ICON = MINIMIZE,
			ZOOM = MAXIMIZE,
		}

		[Flags]
		public enum SPI : int
		{
			GETBEEP = 0x0001,
			SETBEEP = 0x0002,
			GETMOUSE = 0x0003,
			SETMOUSE = 0x0004,
			GETBORDER = 0x0005,
			SETBORDER = 0x0006,
			GETKEYBOARDSPEED = 0x000A,
			SETKEYBOARDSPEED = 0x000B,
			LANGDRIVER = 0x000C,
			ICONHORIZONTALSPACING = 0x000D,
			GETSCREENSAVETIMEOUT = 0x000E,
			SETSCREENSAVETIMEOUT = 0x000F,
			GETSCREENSAVEACTIVE = 0x0010,
			SETSCREENSAVEACTIVE = 0x0011,
			GETGRIDGRANULARITY = 0x0012,
			SETGRIDGRANULARITY = 0x0013,
			SETDESKWALLPAPER = 0x0014,
			SETDESKPATTERN = 0x0015,
			GETKEYBOARDDELAY = 0x0016,
			SETKEYBOARDDELAY = 0x0017,
			ICONVERTICALSPACING = 0x0018,
			GETICONTITLEWRAP = 0x0019,
			SETICONTITLEWRAP = 0x001A,
			GETMENUDROPALIGNMENT = 0x001B,
			SETMENUDROPALIGNMENT = 0x001C,
			SETDOUBLECLKWIDTH = 0x001D,
			SETDOUBLECLKHEIGHT = 0x001E,
			GETICONTITLELOGFONT = 0x001F,
			SETDOUBLECLICKTIME = 0x0020,
			SETMOUSEBUTTONSWAP = 0x0021,
			SETICONTITLELOGFONT = 0x0022,
			GETFASTTASKSWITCH = 0x0023,
			SETFASTTASKSWITCH = 0x0024,
			SETDRAGFULLWINDOWS = 0x0025,
			GETDRAGFULLWINDOWS = 0x0026,
			GETNONCLIENTMETRICS = 0x0029,
			SETNONCLIENTMETRICS = 0x002A,
			GETMINIMIZEDMETRICS = 0x002B,
			SETMINIMIZEDMETRICS = 0x002C,
			GETICONMETRICS = 0x002D,
			SETICONMETRICS = 0x002E,
			SETWORKAREA = 0x002F,
			GETWORKAREA = 0x0030,
			SETPENWINDOWS = 0x0031,
			GETHIGHCONTRAST = 0x0042,
			SETHIGHCONTRAST = 0x0043,
			GETKEYBOARDPREF = 0x0044,
			SETKEYBOARDPREF = 0x0045,
			GETSCREENREADER = 0x0046,
			SETSCREENREADER = 0x0047,
			GETANIMATION = 0x0048,
			SETANIMATION = 0x0049,
			GETFONTSMOOTHING = 0x004A,
			SETFONTSMOOTHING = 0x004B,
			SETDRAGWIDTH = 0x004C,
			SETDRAGHEIGHT = 0x004D,
			SETHANDHELD = 0x004E,
			GETLOWPOWERTIMEOUT = 0x004F,
			GETPOWEROFFTIMEOUT = 0x0050,
			SETLOWPOWERTIMEOUT = 0x0051,
			SETPOWEROFFTIMEOUT = 0x0052,
			GETLOWPOWERACTIVE = 0x0053,
			GETPOWEROFFACTIVE = 0x0054,
			SETLOWPOWERACTIVE = 0x0055,
			SETPOWEROFFACTIVE = 0x0056,
			SETCURSORS = 0x0057,
			SETICONS = 0x0058,
			GETDEFAULTINPUTLANG = 0x0059,
			SETDEFAULTINPUTLANG = 0x005A,
			SETLANGTOGGLE = 0x005B,
			GETWINDOWSEXTENSION = 0x005C,
			SETMOUSETRAILS = 0x005D,
			GETMOUSETRAILS = 0x005E,
			SETSCREENSAVERRUNNING = 0x0061,
			SCREENSAVERRUNNING = SPI.SETSCREENSAVERRUNNING,
			GETFILTERKEYS = 0x0032,
			SETFILTERKEYS = 0x0033,
			GETTOGGLEKEYS = 0x0034,
			SETTOGGLEKEYS = 0x0035,
			GETMOUSEKEYS = 0x0036,
			SETMOUSEKEYS = 0x0037,
			GETSHOWSOUNDS = 0x0038,
			SETSHOWSOUNDS = 0x0039,
			GETSTICKYKEYS = 0x003A,
			SETSTICKYKEYS = 0x003B,
			GETACCESSTIMEOUT = 0x003C,
			SETACCESSTIMEOUT = 0x003D,
			GETSERIALKEYS = 0x003E,
			SETSERIALKEYS = 0x003F,
			GETSOUNDSENTRY = 0x0040,
			SETSOUNDSENTRY = 0x0041,
			GETSNAPTODEFBUTTON = 0x005F,
			SETSNAPTODEFBUTTON = 0x0060,
			GETMOUSEHOVERWIDTH = 0x0062,
			SETMOUSEHOVERWIDTH = 0x0063,
			GETMOUSEHOVERHEIGHT = 0x0064,
			SETMOUSEHOVERHEIGHT = 0x0065,
			GETMOUSEHOVERTIME = 0x0066,
			SETMOUSEHOVERTIME = 0x0067,
			GETWHEELSCROLLLINES = 0x0068,
			SETWHEELSCROLLLINES = 0x0069,
			GETMENUSHOWDELAY = 0x006A,
			SETMENUSHOWDELAY = 0x006B,
			GETSHOWIMEUI = 0x006E,
			SETSHOWIMEUI = 0x006F,
			GETMOUSESPEED = 0x0070,
			SETMOUSESPEED = 0x0071,
			GETSCREENSAVERRUNNING = 0x0072,
			GETDESKWALLPAPER = 0x0073,
			GETACTIVEWINDOWTRACKING = 0x1000,
			SETACTIVEWINDOWTRACKING = 0x1001,
			GETMENUANIMATION = 0x1002,
			SETMENUANIMATION = 0x1003,
			GETCOMBOBOXANIMATION = 0x1004,
			SETCOMBOBOXANIMATION = 0x1005,
			GETLISTBOXSMOOTHSCROLLING = 0x1006,
			SETLISTBOXSMOOTHSCROLLING = 0x1007,
			GETGRADIENTCAPTIONS = 0x1008,
			SETGRADIENTCAPTIONS = 0x1009,
			GETKEYBOARDCUES = 0x100A,
			SETKEYBOARDCUES = 0x100B,
			GETMENUUNDERLINES = SPI.GETKEYBOARDCUES,
			SETMENUUNDERLINES = SPI.SETKEYBOARDCUES,
			GETACTIVEWNDTRKZORDER = 0x100C,
			SETACTIVEWNDTRKZORDER = 0x100D,
			GETHOTTRACKING = 0x100E,
			SETHOTTRACKING = 0x100F,
			GETMENUFADE = 0x1012,
			SETMENUFADE = 0x1013,
			GETSELECTIONFADE = 0x1014,
			SETSELECTIONFADE = 0x1015,
			GETTOOLTIPANIMATION = 0x1016,
			SETTOOLTIPANIMATION = 0x1017,
			GETTOOLTIPFADE = 0x1018,
			SETTOOLTIPFADE = 0x1019,
			GETCURSORSHADOW = 0x101A,
			SETCURSORSHADOW = 0x101B,
			GETMOUSESONAR = 0x101C,
			SETMOUSESONAR = 0x101D,
			GETMOUSECLICKLOCK = 0x101E,
			SETMOUSECLICKLOCK = 0x101F,
			GETMOUSEVANISH = 0x1020,
			SETMOUSEVANISH = 0x1021,
			GETFLATMENU = 0x1022,
			SETFLATMENU = 0x1023,
			GETDROPSHADOW = 0x1024,
			SETDROPSHADOW = 0x1025,
			GETBLOCKSENDINPUTRESETS = 0x1026,
			SETBLOCKSENDINPUTRESETS = 0x1027,
			GETUIEFFECTS = 0x103E,
			SETUIEFFECTS = 0x103F,
			GETFOREGROUNDLOCKTIMEOUT = 0x2000,
			SETFOREGROUNDLOCKTIMEOUT = 0x2001,
			GETACTIVEWNDTRKTIMEOUT = 0x2002,
			SETACTIVEWNDTRKTIMEOUT = 0x2003,
			GETFOREGROUNDFLASHCOUNT = 0x2004,
			SETFOREGROUNDFLASHCOUNT = 0x2005,
			GETCARETWIDTH = 0x2006,
			SETCARETWIDTH = 0x2007,
			GETMOUSECLICKLOCKTIME = 0x2008,
			SETMOUSECLICKLOCKTIME = 0x2009,
			GETFONTSMOOTHINGTYPE = 0x200A,
			SETFONTSMOOTHINGTYPE = 0x200B,
			GETFONTSMOOTHINGCONTRAST = 0x200C,
			SETFONTSMOOTHINGCONTRAST = 0x200D,
			GETFOCUSBORDERWIDTH = 0x200E,
			SETFOCUSBORDERWIDTH = 0x200F,
			GETFOCUSBORDERHEIGHT = 0x2010,
			SETFOCUSBORDERHEIGHT = 0x2011,
			GETFONTSMOOTHINGORIENTATION = 0x2012,
			SETFONTSMOOTHINGORIENTATION = 0x2013,
		}

		[Flags]
		public enum SPIF : int
		{
			UPDATEINIFILE = 0x0001,
			SENDWININICHANGE = 0x0002,
			SENDCHANGE = SPIF.SENDWININICHANGE,
		}

		public enum SW : int
		{
			HIDE = 0,
			SHOWNORMAL = 1,
			SHOWMINIMIZED = 2,
			SHOWMAXIMIZED = 3,
			MAXIMIZE = 3,
			SHOWNOACTIVATE = 4,
			SHOW = 5,
			MINIMIZE = 6,
			SHOWMINNOACTIVE = 7,
			SHOWNA = 8,
			RESTORE = 9,
			SHOWDEFAULT = 10,
			FORCEMINIMIZE = 11,
		}

		[Flags]
		public enum WM : int
		{
			CREATE = 0x0001,
			DESTROY = 0x0002,
			MOVE = 0x0003,

			SIZE = 0x0005,

			PAINT = 0x000F,
			CLOSE = 0x0010,

			QUIT = 0x0012,

			ERASEBKGND = 0x0014,

			MOUSEACTIVATE = 0x0021,
	
			GETMINMAXINFO = 0x0024,

			PAINTICON = 0x0026,
			ICONERASEBKGND = 0x0027,
			NEXTDLGCTL = 0x0028,
			SPOOLERSTATUS = 0x002A,
			DRAWITEM = 0x002B,
			MEASUREITEM = 0x002C,
			DELETEITEM = 0x002D,
			VKEYTOITEM = 0x002E,
			CHARTOITEM = 0x002F,
			SETFONT = 0x0030,
			GETFONT = 0x0031,
			SETHOTKEY = 0x0032,
			GETHOTKEY = 0x0033,
			QUERYDRAGICON = 0x0037,
			COMPAREITEM = 0x0039,
			GETOBJECT = 0x003D,

			NOTIFY = 0x004E,

			GETICON = 0x007F,

			NCHITTEST = 0x0084,

			NCMOUSEMOVE = 0x00A0,
			NCLBUTTONDOWN = 0x00A1,
			NCLBUTTONUP = 0x00A2,
			NCLBUTTONDBLCLK = 0x00A3,
			NCRBUTTONDOWN = 0x00A4,
			NCRBUTTONUP = 0x00A5,
			NCRBUTTONDBLCLK = 0x00A6,
			NCMBUTTONDOWN = 0x00A7,
			NCMBUTTONUP = 0x00A8,
			NCMBUTTONDBLCLK = 0x00A9,

			KEYDOWN = 0x0100,
			KEYUP = 0x0101,
			CHAR = 0x0102,
			DEADCHAR = 0x0103,
			SYSKEYDOWN = 0x0104,
			SYSKEYUP = 0x0105,
			SYSCHAR = 0x0106,
			SYSDEADCHAR = 0x0107,

			UNICHAR = 0x0109,

			INITDIALOG = 0x0110,
			COMMAND = 0x0111,
			SYSCOMMAND = 0x0112,
			TIMER = 0x0113,
			HSCROLL = 0x0114,
			VSCROLL = 0x0115,
			INITMENU = 0x0116,
			INITMENUPOPUP = 0x0117,
			MENUSELECT = 0x011F,
			MENUCHAR = 0x0120,
			ENTERIDLE = 0x0121,

			MOUSEFIRST = 0x0200,
			MOUSEMOVE = 0x0200,
			LBUTTONDOWN = 0x0201,
			LBUTTONUP = 0x0202,
			LBUTTONDBLCLK = 0x0203,
			RBUTTONDOWN = 0x204,
			RBUTTONUP = 0x205,
			RBUTTONDBLCLK = 0x206,
			MBUTTONDOWN = 0x0207,
			MBUTTONUP = 0x0208,
			MBUTTONDBLCLK = 0x0209,
			MOUSEWHEEL = 0x20A,
			XBUTTONDOWN = 0x020B,
			XBUTTONUP = 0x020C,
			XBUTTONDBLCLK = 0x020D,
			MOUSEHWHEEL = 0x020E,

			SIZING = 0x0214,
			MOVING = 0x0216,

			ENTERSIZEMOVE = 0x0231,
			EXITSIZEMOVE = 0x0232,

			APP = 0x8000,
		}

		// WINDOWPLACEMENT
		[Flags]
		public enum WPF : int
		{
			SETMINPOSITION = 1,
			RESTORETOMAXIMIZED = 2,
		}

		/*
			public enum ICON : int
			{
				SMALL = 0,
				BIG = 1,
				/// <summary>
				/// Windows XP 以降：
				/// アプリケーションが提供する小さいアイコンを取得します。
				/// アプリケーションがこのアイコンを提供しない場合は、システムが生成したアイコンが使用されます。
				/// </summary>
				SMALL2 = 2,
			}
		*/
		[Flags]
		public enum WS : int
		{
			OVERLAPPED = 0x00000000,
			POPUP = unchecked((int)0x80000000),
			CHILD = 0x40000000,
			MINIMIZE = 0x20000000,
			VISIBLE = 0x10000000,
			DISABLED = 0x08000000,
			CLIPSIBLINGS = 0x04000000,
			CLIPCHILDREN = 0x02000000,
			MAXIMIZE = 0x01000000,
			CAPTION = WS.BORDER | WS.DLGFRAME,
			BORDER = 0x00800000,
			DLGFRAME = 0x00400000,
			VSCROLL = 0x00200000,
			HSCROLL = 0x00100000,
			SYSMENU = 0x00080000,
			THICKFRAME = 0x00040000,
			GROUP = 0x00020000,
			TABSTOP = 0x00010000,
			MINIMIZEBOX = 0x00020000,
			MAXIMIZEBOX = 0x00010000,
			TILED = WS.OVERLAPPED,
			ICONIC = WS.MINIMIZE,
			SIZEBOX = WS.THICKFRAME,
			TILEDWINDOW = WS.OVERLAPPEDWINDOW,
			OVERLAPPEDWINDOW = (WS.OVERLAPPED | WS.CAPTION | WS.SYSMENU | WS.THICKFRAME | WS.MINIMIZEBOX | WS.MAXIMIZEBOX),
			POPUPWINDOW = (WS.POPUP | WS.BORDER | WS.SYSMENU),
			CHILDWINDOW = WS.CHILD,
		}

		[Flags]
		public enum WS_EX : int
		{
			DLGMODALFRAME = 0x00000001,
			NOPARENTNOTIFY = 0x00000004,
			TOPMOST = 0x00000008,
			ACCEPTFILES = 0x00000010,
			TRANSPARENT = 0x00000020,
			MDICHILD = 0x00000040,
			TOOLWINDOW = 0x00000080,
			WINDOWEDGE = 0x00000100,
			CLIENTEDGE = 0x00000200,
			CONTEXTHELP = 0x00000400,
			RIGHT = 0x00001000,
			LEFT = 0x00000000,
			RTLREADING = 0x00002000,
			LTRREADING = 0x00000000,
			LEFTSCROLLBAR = 0x00004000,
			RIGHTSCROLLBAR = 0x00000000,
			CONTROLPARENT = 0x00010000,
			STATICEDGE = 0x00020000,
			APPWINDOW = 0x00040000,
			OVERLAPPEDWINDOW = (WS_EX.WINDOWEDGE | WS_EX.CLIENTEDGE),
			PALETTEWINDOW = (WS_EX.WINDOWEDGE | WS_EX.TOOLWINDOW | WS_EX.TOPMOST),
			LAYERED = 0x00080000,
			NOINHERITLAYOUT = 0x00100000,
			LAYOUTRTL = 0x00400000,
			COMPOSITED = 0x02000000,
			NOACTIVATE = 0x08000000,
		}

		public struct COMBOBOXINFO
		{
			public int cbSize;
			public RECT rcItem;
			public RECT rcButton;
			public int stateButton;
			public IntPtr hwndCombo;
			public IntPtr hwndItem;
			public IntPtr hwndList;
		}

		public struct NMCUSTOMDRAW
		{
			public NMHDR hdr;
			public int dwDrawStage;
			public IntPtr hdc;
			public RECT rc;
			public int dwItemSpec;
			public int uItemState;
			public int lItemlParam;
		}

		public struct NMHDR
		{
			public IntPtr hwndFrom;
			public int idFrom;
			public int code;
		}

		public struct NMTVCUSTOMDRAW
		{
			public NMCUSTOMDRAW nmcd;
			public int clrText;
			public int clrTextBk;
			public int iLevel;
		}

		public struct POINT
		{
			public int x;
			public int y;

			public POINT(int x, int y)
			{
				this.x = x;
				this.y = y;
			}

			public override string ToString()
			{
				return string.Format("POINT{ x={0}, y={1} }", x, y);
			}
		}

		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public RECT(int left, int top, int right, int bottom)
			{
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
			}

			public override string ToString()
			{
				return string.Format("RECT{ left={0}, top={1}, right={2}, bottom={3} }", left, top, right, bottom);
			}
		}

		public struct SIZE
		{
			public int cx;
			public int cy;

			public SIZE(int cx, int cy)
			{
				this.cx = cx;
				this.cy = cy;
			}

			public override string ToString()
			{
				return string.Format("SIZE { cx={0}, cy={1} }", cx, cy);
			}
		}

		public struct WINDOWPLACEMENT
		{
			public int length;
			public WPF flags;
			public SW showCmd;
			public POINT minPosition;
			public POINT maxPosition;
			public RECT normalPosition;
		}

	}
}

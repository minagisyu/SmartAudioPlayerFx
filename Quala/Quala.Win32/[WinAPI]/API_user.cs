using System;
using System.Runtime.InteropServices;
using System.Text;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace Quala.Win32
{
	using HDC = System.IntPtr;
	using HHOOK = System.IntPtr;
	using HWND = System.IntPtr;
	using WPARAM = System.IntPtr;
	using LPARAM = System.IntPtr;
	using HINSTANCE = System.IntPtr;
	using HICON = System.IntPtr;
	using HBRUSH = System.IntPtr;
	using HCURSOR = System.IntPtr;
	using UINT = System.UInt32;
	using LRESULT = System.IntPtr;
	using HANDLE = System.IntPtr;
	using ATOM = System.UInt16;
	using HMENU = System.IntPtr;
	using HMONITOR = System.IntPtr;
	using HKL = System.IntPtr;

	partial class WinAPI
	{
		const string User32 = "user32.dll";

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool AnimateWindow(HWND hwnd, int dwTime, AW dwFlags);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HDC BeginPaint(HWND hWnd, out PAINTSTRUCT lpPaint);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int CallNextHookEx(HWND hhk, HC nCode, uint wParam, int lParam);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int CallNextHookEx(HHOOK hook, HC code, IntPtr wParam, IntPtr lParam);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int CallNextHookEx(HHOOK hook, HC code, WM wParam, ref KBDLLHOOKSTRUCT lParam);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr CreateAcceleratorTable(ACCEL[] pAccel, int cAccel);

		public static IntPtr CreateAcceleratorTable(ACCEL[] pAccel)
		{
			return CreateAcceleratorTable(pAccel, pAccel.Length);
		}

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND CreateWindowEx(
			WS_EX dwExStyle, string lpClassName, string lpWindowName,
			WS dwStyle, int X, int Y, int nWidth, int nHeight,
			HWND hWndParent, HMENU hMenu, HINSTANCE hInstance, IntPtr lpParam);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND CreateWindowEx(
			WS_EX dwExStyle, IntPtr lpClassName, string lpWindowName,
			WS dwStyle, int X, int Y, int nWidth, int nHeight,
			HWND hWndParent, HMENU hMenu, HINSTANCE hInstance, IntPtr lpParam);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern LRESULT DefWindowProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern LRESULT DefWindowProc(HWND hWnd, WM Msg, WPARAM wParam, LPARAM lParam);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int DestroyAcceleratorTable(IntPtr hAccel);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool DestroyIcon(HICON hIcon);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool DestroyWindow(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int DispatchMessage(ref MSG msg);

		[DllImport(User32, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DrawFrameControl(HDC hdc, ref RECT lprc, DFC uType, DFCS uState);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int DrawText(HDC hdc, string lpchText, int cchText, ref RECT lprc, DT format);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool EndPaint(HWND hWnd, ref PAINTSTRUCT lpPaint);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, LPARAM lParam);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool ExitWindowsEx(EWX uFlags, SHTDN_REASON dwReason);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int FillRect(HDC hDC, ref RECT lprc, HBRUSH hbr);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND FindWindow(
			[MarshalAs(UnmanagedType.LPTStr)] string lpszClass,
			[MarshalAs(UnmanagedType.LPTStr)] string lpszWindow);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND FindWindowEx(HWND hwndParent, HWND hwndChildAfter,
			[MarshalAs(UnmanagedType.LPTStr)] string lpszClass,
			[MarshalAs(UnmanagedType.LPTStr)] string lpszWindow);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern short GetAsyncKeyState(int vKey);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetClassName(HWND hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetClassLong(HWND hWnd, GCW nIndex);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetClassLong(HWND hWnd, GCL nIndex);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool GetClientRect(HWND hWnd, out RECT pRect);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetComboBoxInfo(HWND hwndCombo, ref COMBOBOXINFO pcbi);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HDC GetDC(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND GetDesktopWindow();

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND GetForegroundWindow();

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HKL GetKeyboardLayout(uint idThread);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetKeyNameText(
			int lParam,
			[MarshalAs(UnmanagedType.LPStr)] StringBuilder lpString,
			int cchSize);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool GetLayeredWindowAttributes(HWND hwnd, out int pcrKey, out byte pbAlpha, out LWA pdwFlags);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool GetMessage(out MSG msg, IntPtr hWnd, uint msgFillterMin, uint msgFillterMax);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFO lpmi);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetSystemMetrics(SM nIndex);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HBRUSH GetSysColorBrush(COLOR nIndex);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND GetParent(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr GetTopWindow(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND GetWindow(HWND hWnd, GW nCmd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr GetWindowLong(HWND hWnd, GWL nIndex);

		[DllImport(User32)]
		public static extern bool GetWindowPlacement(HWND hWnd, out WINDOWPLACEMENT lpwndpl);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool GetWindowRect(HWND hWnd, out RECT lpRect);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int GetWindowThreadProcessId(HWND hWnd, out int lpdwProcessId);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool InvalidateRect(HWND hWnd, ref RECT lpRect, bool bErase);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool InvalidateRect(HWND hWnd, IntPtr lpRect, bool bErase);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool IsChild(HWND hWndParent, HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool IsWindow(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool IsWindowVisible(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool KillTimer(HWND hWnd, uint uIDEvent);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HCURSOR LoadCursor(HINSTANCE hInstance, IntPtr lpCursorName);

		public static IntPtr MAKEINTRESOURCE(ushort i)
		{
			return (IntPtr)i;
		}

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HANDLE LoadImage(HINSTANCE hInst, string name, IMAGE type, int cx, int cy, LR fuLoad);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, HKL dwhkl);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int MapWindowPoints(HWND hWndFrom, HWND hWndTo, IntPtr lpPoints, int cPoints);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern DlgCmdIDs MessageBox(HWND hWnd, string lpText, string lpCaption, MB uType);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr MonitorFromWindow(HWND hwnd, MONITOR_DEFAULTTO flags);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint msgFilterMin, uint msgFilterMax, PM wRemoveMsg);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern void PostQuitMessage(int nExitCode);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern ATOM RegisterClassEx(ref WNDCLASSEX wcex);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int ReleaseDC(HWND hWnd, HDC hDC);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(HWND hWnd, int msg, IntPtr wParam, IntPtr lParam);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(HWND hWnd, WM msg, IntPtr wParam, IntPtr lParam);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND SetActiveWindow(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND SetFocus(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool SetForegroundWindow(HWND hWnd);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool SetLayeredWindowAttributes(HWND hwnd, int crKey, byte bAlpha, LWA dwFlags);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern HWND SetParent(HWND hWndChild, HWND hWndNewParent);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool SetRect(out RECT lprc, int xLeft, int yTop, int xRight, int yBottom);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern uint GetSysColor(COLOR nIndex);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern uint SetTimer(HWND hWnd, uint nIDEvent, uint uElapse, TIMERPROC lpTimerFunc);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowLong(HWND hWnd, GWL nIndex, IntPtr dwNewLong);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowLong(HWND hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport(User32)]
		public static extern bool SetWindowPlacement(HWND hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int x, int y, int cx, int cy, SWP flags);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowsHookEx(WH idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowsHookEx(WH idHook, HOOKPROC lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern IntPtr SetWindowsHookEx(WH idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool ShowWindow(HWND hWnd, SW nCmdShow);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int SystemParametersInfo(SPI uiAction, int uiParam, ref IntPtr lpvParam, SPIF fWinIni);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int SystemParametersInfo(SPI uiAction, int uiParam, out RECT lpvParam, SPIF fWinIni);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int TrackPopupMenuEx(HMENU hMenu, int fuFlags, int x, int y, HWND hWnd, IntPtr ptpm);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int TrackPopupMenuEx(HandleRef hMenu, int fuFlags, int x, int y, HandleRef hWnd, IntPtr ptpm);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool TrackPopupMenuEx(HMENU hMenu, TPM fuFlags, int x, int y, HWND hWnd, IntPtr ptpm);
		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool TrackPopupMenuEx(HMENU hMenu, TPM fuFlags, int x, int y, HWND hWnd, ref TPMPARAMS ptpm);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern int TranslateAccelerator(IntPtr hWnd, IntPtr hAccTable, ref MSG lpMsg);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool TranslateMessage(ref MSG msg);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool UnhookWindowsHookEx(HHOOK hhk);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool UpdateLayeredWindow(HWND hWnd,
			HDC hdcDst, ref POINT pptDst, ref SIZE psize, HDC hdcSrc,
			ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, ULW dwFlags);

		[DllImport(User32, CharSet = CharSet.Auto)]
		public static extern bool UpdateLayeredWindow(HWND hWnd,
			HDC hdcDst, IntPtr pptDst, IntPtr psize, HDC hdcSrc,
			IntPtr pptSrc, int crKey, ref BLENDFUNCTION pblend, ULW dwFlags);

		public static readonly HWND HWND_TOP = (IntPtr)0;
		public static readonly HWND HWND_BOTTOM = (IntPtr)1;
		public static readonly HWND HWND_TOPMOST = (IntPtr)(-1);
		public static readonly HWND HWND_NOTOPMOST = (IntPtr)(-2);


		public delegate bool WNDENUMPROC(HWND hWnd, LPARAM lParam);
		public delegate int HOOKPROC(HC code, uint wParam, int lParam);
		public delegate int LowLevelKeyboardProc(HC code, WM message, ref KBDLLHOOKSTRUCT state);

		public enum AC : byte
		{
			SRC_OVER = 0x00,
			SRC_ALPHA = 0x01,
		}

		[Flags]
		public enum AccelFlags : byte
		{
			VirtualKey = 0x01,
			NoInvert = 0x02,
			Shoft = 0x04,
			Control = 0x08,
			Alt = 0x10,
		}

		/// <summary>
		/// Color Types
		/// </summary>
		public enum COLOR : int
		{
			CTLCOLOR_MSGBOX = 0,
			CTLCOLOR_EDIT = 1,
			CTLCOLOR_LISTBOX = 2,
			CTLCOLOR_BTN = 3,
			CTLCOLOR_DLG = 4,
			CTLCOLOR_SCROLLBAR = 5,
			CTLCOLOR_STATIC = 6,
			CTLCOLOR_MAX = 7,

			SCROLLBAR = 0,
			BACKGROUND = 1,
			ACTIVECAPTION = 2,
			INACTIVECAPTION = 3,
			MENU = 4,
			WINDOW = 5,
			WINDOWFRAME = 6,
			MENUTEXT = 7,
			WINDOWTEXT = 8,
			CAPTIONTEXT = 9,
			ACTIVEBORDER = 10,
			INACTIVEBORDER = 11,
			APPWORKSPACE = 12,
			HIGHLIGHT = 13,
			HIGHLIGHTTEXT = 14,
			BTNFACE = 15,
			BTNSHADOW = 16,
			GRAYTEXT = 17,
			BTNTEXT = 18,
			INACTIVECAPTIONTEXT = 19,
			BTNHIGHLIGHT = 20,

			_3DDKSHADOW = 21,
			_3DLIGHT = 22,
			INFOTEXT = 23,
			INFOBK = 24,

			HOTLIGHT = 26,
			GRADIENTACTIVECAPTION = 27,
			GRADIENTINACTIVECAPTION = 28,

			MENUHILIGHT = 29,
			MENUBAR = 30,

			DESKTOP = BACKGROUND,
			_3DFACE = BTNFACE,
			_3DSHADOW = BTNSHADOW,
			_3DHIGHLIGHT = BTNHIGHLIGHT,
			_3DHILIGHT = BTNHIGHLIGHT,
			BTNHILIGHT = BTNHIGHLIGHT,
		}

		// Dialog Box Command IDs (IDxxxx)
		public enum DlgCmdIDs : int
		{
			OK = 1,
			CANCEL = 2,
			ABORT = 3,
			RETRY = 4,
			IGNORE = 5,
			YES = 6,
			NO = 7,
			//#if(WINVER >= 0x0400)
			CLOSE = 8,
			HELP = 9,
			//#endif /* WINVER >= 0x0400 */

			//#if(WINVER >= 0x0500)
			TRYAGAIN = 10,
			CONTINUE = 11,
			// #endif /* WINVER >= 0x0500 */

			// #if(WINVER >= 0x0501)
			// #ifndef IDTIMEOUT
			TIMEOUT = 32000,
		}

		/// <summary>
		/// DrawText() Format Flags
		/// </summary>
		[Flags]
		public enum DT : uint
		{
			TOP = 0x00000000,
			LEFT = 0x00000000,
			CENTER = 0x00000001,
			RIGHT = 0x00000002,
			VCENTER = 0x00000004,
			BOTTOM = 0x00000008,
			WORDBREAK = 0x00000010,
			SINGLELINE = 0x00000020,
			EXPANDTABS = 0x00000040,
			TABSTOP = 0x00000080,
			NOCLIP = 0x00000100,
			EXTERNALLEADING = 0x00000200,
			CALCRECT = 0x00000400,
			NOPREFIX = 0x00000800,
			INTERNAL = 0x00001000,

			EDITCONTROL = 0x00002000,
			PATH_ELLIPSIS = 0x00004000,
			END_ELLIPSIS = 0x00008000,
			MODIFYSTRING = 0x00010000,
			RTLREADING = 0x00020000,
			WORD_ELLIPSIS = 0x00040000,
			NOFULLWIDTHCHARBREAK = 0x00080000,
			HIDEPREFIX = 0x00100000,
			PREFIXONLY = 0x00200000,
		}

		[Flags]
		public enum EWX : int
		{
			LOGOFF = 0,
			SHUTDOWN = 0x00000001,
			REBOOT = 0x00000002,
			FORCE = 0x00000004,
			POWEROFF = 0x00000008,
			QUICKRESOLVE = 0x00000020,
			// _WIN32_WINNT >= 0x0500
			FORCEIFHUNG = 0x00000010,
			// _WIN32_WINNT >= 0x0600
			RESTARTAPPS = 0x00000040,
		}

		[Flags]
		public enum FILE_ATTRIBUTE : int
		{
			NORMAL = 0x80,
		}

		/// <summary>
		/// Class field offsets for GetClassLong()
		/// </summary>
		public enum GCL : int
		{
			MENUNAME = (-8),
			HBRBACKGROUND = (-10),
			HCURSOR = (-12),
			HICON = (-14),
			HMODULE = (-16),
			CBWNDEXTRA = (-18),
			CBCLSEXTRA = (-20),
			WNDPROC = (-24),
			STYLE = (-26),
			HICONSM = (-34),
		}
		/// <summary>
		/// Class field offsets for GetClassLong()
		/// </summary>
		public enum GCW : int
		{
			ATOM = (-32),
		}

		/// <summary>
		/// GetWindow() Constants
		/// </summary>
		public enum GW : int
		{
			HWNDFIRST = 0,
			HWNDLAST = 1,
			HWNDNEXT = 2,
			HWNDPREV = 3,
			OWNER = 4,
			CHILD = 5,
			ENABLEDPOPUP = 6,
			MAX = 6,
		}

		/// <summary>
		/// Window field offsets for GetWindowLong()
		/// </summary>
		public enum GWL : int
		{
			WNDPROC = (-4),
			HINSTANCE = (-6),
			HWNDPARENT = (-8),
			STYLE = (-16),
			EXSTYLE = (-20),
			USERDATA = (-21),
			ID = (-12),
		}

		public enum GWLP : int
		{
			WNDPROC = (-4),
			HINSTANCE = (-6),
			HWNDPARENT = (-8),
			USERDATA = (-21),
			ID = (-12),
		}

		public enum HC : int
		{
			// HC_*
			ACTION = 0,
			GETNEXT = 1,
			SKIP = 2,
			NOREMOVE = 3,
			NOREM = NOREMOVE,
			SYSMODALON = 4,
			SYSMODALOFF = 5,

			// HCBT_*
			BT_MOVESIZE = 0,
			BT_MINMAX = 1,
			BT_QS = 2,
			BT_CREATEWND = 3,
			BT_DESTROYWND = 4,
			BT_ACTIVATE = 5,
			BT_CLICKSKIPPED = 6,
			BT_KEYSKIPPED = 7,
			BT_SYSCOMMAND = 8,
			BT_SETFOCUS = 9,
		}

		public enum IMAGE : uint
		{
			BITMAP = 0,
			ICON = 1,
			CURSOR = 2,
			ENHMETAFILE = 3,
		}

		[Flags]
		public enum LR : uint
		{
			DEFAULTCOLOR = 0x00000000,
			MONOCHROME = 0x00000001,
			COLOR = 0x00000002,
			COPYRETURNORG = 0x00000004,
			COPYDELETEORG = 0x00000008,
			LOADFROMFILE = 0x00000010,
			LOADTRANSPARENT = 0x00000020,
			DEFAULTSIZE = 0x00000040,
			VGACOLOR = 0x00000080,
			LOADMAP3DCOLORS = 0x00001000,
			CREATEDIBSECTION = 0x00002000,
			COPYFROMRESOURCE = 0x00004000,
			SHARED = 0x00008000,
		}

		[Flags]
		public enum LWA : int
		{
			COLORKEY = 0x00000001,
			ALPHA = 0x00000002,
		}

		// MessageBox() Flags
		[Flags]
		public enum MB : int
		{
			OK = 0x00000000,
			OKCANCEL = 0x00000001,
			ABORTRETRYIGNORE = 0x00000002,
			YESNOCANCEL = 0x00000003,
			YESNO = 0x00000004,
			RETRYCANCEL = 0x00000005,
			// #if(WINVER >= 0x0500)
			CANCELTRYCONTINUE = 0x00000006,
			// #endif /* WINVER >= 0x0500 */


			ICONHAND = 0x00000010,
			ICONQUESTION = 0x00000020,
			ICONEXCLAMATION = 0x00000030,
			ICONASTERISK = 0x00000040,

			// #if(WINVER >= 0x0400)
			USERICON = 0x00000080,
			ICONWARNING = ICONEXCLAMATION,
			ICONERROR = ICONHAND,
			// #endif /* WINVER >= 0x0400 */

			ICONINFORMATION = ICONASTERISK,
			ICONSTOP = ICONHAND,

			DEFBUTTON1 = 0x00000000,
			DEFBUTTON2 = 0x00000100,
			DEFBUTTON3 = 0x00000200,
			// #if(WINVER >= 0x0400)
			DEFBUTTON4 = 0x00000300,
			// #endif /* WINVER >= 0x0400 */

			APPLMODAL = 0x00000000,
			SYSTEMMODAL = 0x00001000,
			TASKMODAL = 0x00002000,
			// #if(WINVER >= 0x0400)
			HELP = 0x00004000, // Help Button
			// #endif /* WINVER >= 0x0400 */

			NOFOCUS = 0x00008000,
			SETFOREGROUND = 0x00010000,
			DEFAULT_DESKTOP_ONLY = 0x00020000,

			//#if(WINVER >= 0x0400)
			TOPMOST = 0x00040000,
			RIGHT = 0x00080000,
			RTLREADING = 0x00100000,
			// #endif /* WINVER >= 0x0400 */

			// #ifdef _WIN32_WINNT
			// #if (_WIN32_WINNT >= 0x0400)
			SERVICE_NOTIFICATION = 0x00200000,
			//#else
			SERVICE_NOTIFICATION_9X = 0x00040000,
			//#endif
			SERVICE_NOTIFICATION_NT3X = 0x00040000,
			//#endif

			TYPEMASK = 0x0000000F,
			ICONMASK = 0x000000F0,
			DEFMASK = 0x00000F00,
			MODEMASK = 0x00003000,
			MISCMASK = 0x0000C000,
		}

		public struct MINMAXINFO
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}

		public enum MONITOR_DEFAULTTO : uint
		{
			NULL = 0x00000000,
			PRIMARY = 0x00000001,
			NEAREST = 0x00000002,
		}

		[Flags]
		public enum MONITORINFOF : int
		{
			PRIMARY = 0,
		}

		// PeekMessage() Options
		public enum PM : uint
		{
			NOREMOVE = 0x0000,
			REMOVE = 0x0001,
			NOYIELD = 0x0002,
			/*	QS_INPUT = (QS_INPUT << 16),
				QS_POSTMESSAGE = ((QS_POSTMESSAGE | QS_HOTKEY | QS_TIMER) << 16),
				QS_PAINT = (QS_PAINT << 16),
				QS_SENDMESSAGE = (QS_SENDMESSAGE << 16),
			*/
		}

		[Flags]
		public enum SHTDN_REASON : int
		{
			// Flags used by the various UIs.
			FLAG_COMMENT_REQUIRED = 0x01000000,
			FLAG_DIRTY_PROBLEM_ID_REQUIRED = 0x02000000,
			FLAG_CLEAN_UI = 0x04000000,
			FLAG_DIRTY_UI = 0x08000000,

			// Flags that end up in the event log code.
			FLAG_USER_DEFINED = 0x40000000,
			FLAG_PLANNED = unchecked((int)0x80000000),

			// Microsoft major reasons.
			MAJOR_OTHER = 0x00000000,
			MAJOR_NONE = 0x00000000,
			MAJOR_HARDWARE = 0x00010000,
			MAJOR_OPERATINGSYSTEM = 0x00020000,
			MAJOR_SOFTWARE = 0x00030000,
			MAJOR_APPLICATION = 0x00040000,
			MAJOR_SYSTEM = 0x00050000,
			MAJOR_POWER = 0x00060000,
			MAJOR_LEGACY_API = 0x00070000,

			// Microsoft minor reasons.
			MINOR_OTHER = 0x00000000,
			MINOR_NONE = 0x000000ff,
			MINOR_MAINTENANCE = 0x00000001,
			MINOR_INSTALLATION = 0x00000002,
			MINOR_UPGRADE = 0x00000003,
			MINOR_RECONFIG = 0x00000004,
			MINOR_HUNG = 0x00000005,
			MINOR_UNSTABLE = 0x00000006,
			MINOR_DISK = 0x00000007,
			MINOR_PROCESSOR = 0x00000008,
			MINOR_NETWORKCARD = 0x00000009,
			MINOR_POWER_SUPPLY = 0x0000000a,
			MINOR_CORDUNPLUGGED = 0x0000000b,
			MINOR_ENVIRONMENT = 0x0000000c,
			MINOR_HARDWARE_DRIVER = 0x0000000d,
			MINOR_OTHERDRIVER = 0x0000000e,
			MINOR_BLUESCREEN = 0x0000000F,
			MINOR_SERVICEPACK = 0x00000010,
			MINOR_HOTFIX = 0x00000011,
			MINOR_SECURITYFIX = 0x00000012,
			MINOR_SECURITY = 0x00000013,
			MINOR_NETWORK_CONNECTIVITY = 0x00000014,
			MINOR_WMI = 0x00000015,
			MINOR_SERVICEPACK_UNINSTALL = 0x00000016,
			MINOR_HOTFIX_UNINSTALL = 0x00000017,
			MINOR_SECURITYFIX_UNINSTALL = 0x00000018,
			MINOR_MMC = 0x00000019,
			MINOR_SYSTEMRESTORE = 0x0000001a,
			MINOR_TERMSRV = 0x00000020,
			MINOR_DC_PROMOTION = 0x00000021,
			MINOR_DC_DEMOTION = 0x00000022,

			UNKNOWN = MINOR_NONE,
			LEGACY_API = (MAJOR_LEGACY_API | FLAG_PLANNED),

			// This mask cuts out UI flags.
			VALID_BIT_MASK = unchecked((int)0xc0ffffff),
		}

		/// <summary>
		/// GetSystemMetrics() codes
		/// </summary>
		public enum SM : int
		{
			CXSCREEN = 0,
			CYSCREEN = 1,
			CXVSCROLL = 2,
			CYHSCROLL = 3,
			CYCAPTION = 4,
			CXBORDER = 5,
			CYBORDER = 6,
			CXDLGFRAME = 7,
			CYDLGFRAME = 8,
			CYVTHUMB = 9,
			CXHTHUMB = 10,
			CXICON = 11,
			CYICON = 12,
			CXCURSOR = 13,
			CYCURSOR = 14,
			CYMENU = 15,
			CXFULLSCREEN = 16,
			CYFULLSCREEN = 17,
			CYKANJIWINDOW = 18,
			MOUSEPRESENT = 19,
			CYVSCROLL = 20,
			CXHSCROLL = 21,
			DEBUG = 22,
			SWAPBUTTON = 23,
			RESERVED1 = 24,
			RESERVED2 = 25,
			RESERVED3 = 26,
			RESERVED4 = 27,
			CXMIN = 28,
			CYMIN = 29,
			CXSIZE = 30,
			CYSIZE = 31,
			CXFRAME = 32,
			CYFRAME = 33,
			CXMINTRACK = 34,
			CYMINTRACK = 35,
			CXDOUBLECLK = 36,
			CYDOUBLECLK = 37,
			CXICONSPACING = 38,
			CYICONSPACING = 39,
			MENUDROPALIGNMENT = 40,
			PENWINDOWS = 41,
			DBCSENABLED = 42,
			CMOUSEBUTTONS = 43,

			CXFIXEDFRAME = CXDLGFRAME, // ;win40 name change
			CYFIXEDFRAME = CYDLGFRAME,  // ;win40 name change
			CXSIZEFRAME = CXFRAME,    // ;win40 name change
			CYSIZEFRAME = CYFRAME,     // ;win40 name change

			SECURE = 44,
			CXEDGE = 45,
			CYEDGE = 46,
			CXMINSPACING = 47,
			CYMINSPACING = 48,
			CXSMICON = 49,
			CYSMICON = 50,
			CYSMCAPTION = 51,
			CXSMSIZE = 52,
			CYSMSIZE = 53,
			CXMENUSIZE = 54,
			CYMENUSIZE = 55,
			ARRANGE = 56,
			CXMINIMIZED = 57,
			CYMINIMIZED = 58,
			CXMAXTRACK = 59,
			CYMAXTRACK = 60,
			CXMAXIMIZED = 61,
			CYMAXIMIZED = 62,
			NETWORK = 63,
			CLEANBOOT = 67,
			CXDRAG = 68,
			CYDRAG = 69,
			SHOWSOUNDS = 70,
			CXMENUCHECK = 71,  // Use instead of GetMenuCheckMarkDimensions()!
			CYMENUCHECK = 72,
			SLOWMACHINE = 73,
			MIDEASTENABLED = 74,
			MOUSEWHEELPRESENT = 75,
			XVIRTUALSCREEN = 76,
			YVIRTUALSCREEN = 77,
			CXVIRTUALSCREEN = 78,
			CYVIRTUALSCREEN = 79,
			CMONITORS = 80,
			SAMEDISPLAYFORMAT = 81,
			IMMENABLED = 82,
			CXFOCUSBORDER = 83,
			CYFOCUSBORDER = 84,
			TABLETPC = 86,
			MEDIACENTER = 87,
			STARTER = 88,
			SERVERR2 = 89,
			MOUSEHORIZONTALWHEELPRESENT = 91,
			CXPADDEDBORDER = 92,
			// #if (WINVER < 0x0500) && (!defined(_WIN32_WINNT) || (_WIN32_WINNT < 0x0400))
			//	CMETRICS = 76,
			// #elif WINVER == 0x500
			//	CMETRICS = 83,
			// #elif WINVER == 0x501
			//	CMETRICS = 90,
			// else
			CMETRICS = 93,
			REMOTESESSION = 0x1000,
			SHUTTINGDOWN = 0x2000,
			REMOTECONTROL = 0x2001,
			CARETBLINKINGENABLED = 0x2002,
		}

		/// <summary>
		/// SetWindowPos Flags
		/// </summary>
		[Flags]
		public enum SWP : int
		{
			NOSIZE = 0x0001,
			NOMOVE = 0x0002,
			NOZORDER = 0x0004,
			NOREDRAW = 0x0008,
			NOACTIVATE = 0x0010,
			FRAMECHANGED = 0x0020, // The frame changed: send WM_NCCALCSIZE
			SHOWWINDOW = 0x0040,
			HIDEWINDOW = 0x0080,
			NOCOPYBITS = 0x0100,
			NOOWNERZORDER = 0x0200, // Don't do owner Z ordering
			NOSENDCHANGING = 0x0400, // Don't send WM_WINDOWPOSCHANGING

			DRAWFRAME = SWP.FRAMECHANGED,
			NOREPOSITION = SWP.NOOWNERZORDER,

			DEFERERASE = 0x2000,
			ASYNCWINDOWPOS = 0x4000,
		}

		[Flags]
		public enum TPM : int
		{
			LEFTBUTTON = 0x0000,
			RIGHTBUTTON = 0x0002,
			LEFTALIGN = 0x0000,
			CENTERALIGN = 0x0004,
			RIGHTALIGN = 0x0008,
			TOPALIGN = 0x0000,
			VCENTERALIGN = 0x0010,
			BOTTOMALIGN = 0x0020,
			HORIZONTAL = 0x0000,
			VERTICAL = 0x0040,
			RETURNCMD = 0x0100,
		}

		[Flags]
		public enum ULW : int
		{
			COLORKEY = 0x00000001,
			ALPHA = 0x00000002,
			OPAQUE = 0x00000004,
			EX_NORESIZE = 0x00000008,
		}

		public enum WH : int
		{
			MSGFILTER = (-1),
			JOURNALRECORD = 0,
			JOURNALPLAYBACK = 1,
			KEYBOARD = 2,
			GETMESSAGE = 3,
			CALLWNDPROC = 4,
			CBT = 5,
			SYSMSGFILTER = 6,
			MOUSE = 7,
			HARDWARE = 8,
			DEBUG = 9,
			SHELL = 10,
			FOREGROUNDIDLE = 11,
			CALLWNDPROCRET = 12,
			KEYBOARD_LL = 13,
			MOUSE_LL = 14,
		}


		public struct ACCEL
		{
			public AccelFlags fVirt;
			public uint key;
			public ushort cmd;

			public ACCEL(AccelFlags flag, uint key, ushort id)
			{
				this.fVirt = flag;
				this.key = key;
				this.cmd = id;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct BLENDFUNCTION
		{
			/// <summary>
			/// must be AC.SRC_OVER
			/// </summary>
			public AC BlendOp;

			/// <summary>
			/// must be zero
			/// </summary>
			public byte BlendFlags;

			/// <summary>
			/// zero by transparency
			/// </summary>
			public byte SourceConstantAlpha;

			/// <summary
			/// >must be AC.SRC_ALPHA
			/// </summary>
			public AC AlphaFormat;
		}

		public struct DRAWITEMSTRUCT
		{
			public ODT CtlType;
			public UINT CtlID;
			public UINT itemID;
			public UINT itemAction;
			public UINT itemState;
			public HWND hwndItem;
			public HDC hDC;
			public RECT rcItem;
			public IntPtr itemData;
		}

		/// <summary>
		/// Structure used by WH_KEYBOARD_LL
		/// </summary>
		public struct KBDLLHOOKSTRUCT
		{
			public uint vkCode;		// WinForms.Keys compatible?
			public uint scanCode;
			public uint flags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		public struct MEASUREITEMSTRUCT
		{
			public ODT CtlType;
			public UINT CtlID;
			public UINT itemID;
			public UINT itemWidth;
			public UINT itemHeight;
			public IntPtr itemData;
		}

		public struct MONITORINFO
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public MONITORINFOF dwFlags;
		}

		/// <summary>
		/// Owner draw control types
		/// </summary>
		public enum ODT : uint
		{
			MENU = 1,
			LISTBOX = 2,
			COMBOBOX = 3,
			BUTTON = 4,
			STATIC = 5,
		}

		/// <summary>
		/// Owner draw actions
		/// </summary>
		[Flags]
		public enum ODA : uint
		{
			DRAWENTIRE = 0x0001,
			SELECT = 0x0002,
			FOCUS = 0x0004,
		}

		/// <summary>
		/// Owner draw state
		/// </summary>
		[Flags]
		public enum ODS : uint
		{
			SELECTED = 0x0001,
			GRAYED = 0x0002,
			DISABLED = 0x0004,
			CHECKED = 0x0008,
			FOCUS = 0x0010,
			DEFAULT = 0x0020,
			COMBOBOXEDIT = 0x1000,
			HOTLIGHT = 0x0040,
			INACTIVE = 0x0080,
			NOACCEL = 0x0100,
			NOFOCUSRECT = 0x0200,
		}

		public struct PAINTSTRUCT
		{
			public HDC hdc;
			[MarshalAs(UnmanagedType.Bool)]
			public bool fErase;
			public RECT rcPaint;
			[MarshalAs(UnmanagedType.Bool)]
			public bool fRestore;
			[MarshalAs(UnmanagedType.Bool)]
			public bool fIncUpdate;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public byte[] rgbReserved;
		}

		public struct MSG
		{
			public HWND hWnd;
			public int message;
			public IntPtr wParam;
			public IntPtr lParam;
			public int time;
			public POINT pt;
		}

		public struct TPMPARAMS
		{
			public int cbSize;
			public RECT rcExclude;
		}

		public struct WNDCLASSEX
		{
			public UINT cbSize;
			// Win3.x
			public UINT style;
			//	public WNDPROC     lpfnWndProc;
			public IntPtr lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public HINSTANCE hInstance;
			public HICON hIcon;
			public HCURSOR hCursor;
			public HBRUSH hbrBackground;
			public string lpszMenuName;
			public string lpszClassName;
			// Win4.0
			public HICON hIconSm;
		}

		public delegate LRESULT WNDPROC(HWND hWnd, WM msg, WPARAM wParam, LPARAM lParam);
		public delegate void TIMERPROC(HWND hWnd, WM msg, uint idEvent, uint dwTime);

	}
}

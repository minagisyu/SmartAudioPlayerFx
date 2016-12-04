using System;
using System.Runtime.InteropServices;
using System.Security;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace Quala.Win32
{
	partial class WinAPI
	{
		partial class COM
		{
			[ComImport]
			[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			[Guid("000214E4-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IContextMenu
			{
				[PreserveSig]
				int QueryContextMenu(IntPtr hMenu, int indexMenu, int idCmdFirst, int idCmdLast, CMF uFlags);

				void InvokeCommand(ref CMINVOKECOMMANDINFO pici);

				void GetCommandString(int idCmd, GCS uFlags, ref int pReserved,
					[MarshalAs(UnmanagedType.LPStr)] out string Name, int cchMax);
			}


			[Flags]
			public enum CMF : int
			{
				NORMAL = 0x00000000,
				DEFAULTONLY = 0x00000001,
				VERBSONLY = 0x00000002,
				EXPLORE = 0x00000004,
				NOVERBS = 0x00000008,
				CANRENAME = 0x00000010,
				NODEFAULT = 0x00000020,
				INCLUDESTATIC = 0x00000040,
				EXTENDEDVERBS = 0x00000100,
				RESERVED = unchecked((int)0xffff0000),
			}

			[Flags]
			public enum CMIC : int
			{
				MASK_ICON = 0x00000010,
				MASK_HOTKEY = 0x00000020,
				MASK_FLAG_NO_UI = 0x00000400,
				MASK_UNICODE = 0x00004000,
				MASK_NO_CONSOLE = 0x00008000,
				//		CMIC_MASK_HASLINKNAME		= ,
				//		CMIC_MASK_FLAG_SEP_VDM		= ,
				//		CMIC_MASK_HASTITLE			= ,
				MASK_ASYNCOK = 0x00100000,
				MASK_NOZONECHECKS = 0x00800000,
				MASK_FLAG_LOG_USAGE = 0x04000000,
				MASK_SHIFT_DOWN = 0x10000000,
				MASK_PTINVOKE = 0x20000000,
				MASK_CONTROL_DOWN = 0x40000000,
			}

			[Flags]
			public enum GCS : int
			{
				VERBA = 0x00000000,
				HELPTEXTA = 0x00000001,
				VALIDATEA = 0x00000002,
				VERBW = 0x00000004,
				HELPTEXTW = 0x00000005,
				VALIDATEW = 0x00000006,
				UNICODE = 0x00000004,
			}

			public struct CMINVOKECOMMANDINFO
			{
				public int cbSize;
				public CMIC fMask;
				public IntPtr hWnd;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpVerb;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpParameters;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpDirectory;
				public int nShow;
				public int dwHotKey;
				public IntPtr hIcon;
			}

			public struct CMINVOKECOMMANDINFOEX
			{
				public int cbSize;
				public CMIC fMask;
				public IntPtr hWnd;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpVerb;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpParameters;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpDirectory;
				public int nShow;
				public int dwHotKey;
				public IntPtr hIcon;
				[MarshalAs(UnmanagedType.LPStr)]
				public string lpTitle;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpVerbW;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpParametersW;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpDirectoryW;
				[MarshalAs(UnmanagedType.LPWStr)]
				public string lpTitleW;
				public POINT ptInvoke;
			}
		}
	}
}

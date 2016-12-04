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
			[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
			[SuppressUnmanagedCodeSecurity]
			public interface IShellItem
			{
				void BindToHandler(IBindCtx pbc, Guid bhid, Guid riid,
					[MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 2)]
			object ppv);
				void GetParent(out IShellItem ppsi);
				/// <summary>
				/// ppszNameはMarshal.FreeCoTaaskMemで解放すること？
				/// </summary>
				/// <param name="sigdnName"></param>
				/// <param name="ppszName"></param>
				void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
				void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
				void Compare(IShellItem psi, uint hint, out int piOrder);
			}

			public enum SIGDN : uint
			{
				NORMALDISPLAY = 0,
				PARENTRELATIVEPARSING = 0x80018001,
				DESKTOPABSOLUTEPARSING = 0x80028000,
				PARENTRELATIVEEDITING = 0x80031001,
				DESKTOPABSOLUTEEDITING = 0x8004c000,
				FILESYSPATH = 0x80058000,
				URL = 0x80068000,
				PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
				PARENTRELATIVE = 0x80080001,
			}
		}
	}
}

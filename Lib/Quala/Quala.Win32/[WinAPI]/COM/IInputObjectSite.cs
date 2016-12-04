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
			[Guid("F1DB8392-7331-11D0-8C99-00A0C92DBFE8")]
			[SuppressUnmanagedCodeSecurity]
			public interface IInputObjectSite
			{
				[PreserveSig]
				COMRESULT OnFocusChangeIS(
					[MarshalAs(UnmanagedType.IUnknown)]
			object punkObj,
					bool fSetFocus);
			}
		}
	}
}

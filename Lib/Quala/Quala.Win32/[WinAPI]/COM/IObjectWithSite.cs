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
			[Guid("FC4801A3-2BA9-11CF-A229-00AA003D7352")]
			[SuppressUnmanagedCodeSecurity]
			public interface IObjectWithSite
			{
				void SetSite([MarshalAs(UnmanagedType.IUnknown)] object pUnkSite);
				void GetSite(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
			}
		}
	}
}

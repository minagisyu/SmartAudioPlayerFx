using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Quala.Interop.Win32.COM
{
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("0000010C-0000-0000-C000-000000000046")]
	[SuppressUnmanagedCodeSecurity]
	public interface IPersist
	{
		void GetClassID(out Guid pClassID);
	}
}

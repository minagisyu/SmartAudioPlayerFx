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
			[Guid("2659B475-EEB8-48b7-8F07-B378810F48CF")]
			[SuppressUnmanagedCodeSecurity]
			public interface IShellItemFilter
			{
				void IncludeItem(IShellItem psi);
				void GetEnumFlagsForItem(IShellItem psi, out uint pgrfFlags);
			}
		}
	}
}

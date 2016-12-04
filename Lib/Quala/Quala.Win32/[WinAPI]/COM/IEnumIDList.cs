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
			[Guid("000214F2-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IEnumIDList
			{
				void Next(uint celt, out IntPtr rgelt, out int pceltFetched);
				void Skip(uint celt);
				void Reset();
				void Clone(out IEnumIDList ppenum);
			}
		}
	}
}

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
			[Guid("00000101-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IEnumString
			{
				void Next(uint celt,
					[MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 0)]
			out string rgelt,
					out uint pceltFetched);
				void Skip(uint celt);
				void Reset();
				void Clone(out IEnumString ppenum);
			}
		}
	}
}

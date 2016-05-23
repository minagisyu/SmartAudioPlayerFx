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
			[Guid("00000102-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IEnumMoniker
			{
				void Next(uint celt,
					[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			IMoniker rgelt,
					out uint pceltFetched);
				void Skip(uint celt);
				void Reset();
				void Clone(out IEnumMoniker ppenum);
			}
		}
	}
}

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
			[Guid("70629033-e363-4a28-a567-0db78006e6d7")]
			[SuppressUnmanagedCodeSecurity]
			public interface IEnumShellItems
			{
				void Next(uint celt,
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			out IShellItem[] rgelt,
					out uint pceltFetched);
				void Skip(uint celt);
				void Reset();
				void Clone(out IEnumShellItems ppenum);
			}
		}
	}
}

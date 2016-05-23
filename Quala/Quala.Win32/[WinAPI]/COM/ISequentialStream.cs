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
			[Guid("0c733a30-2a1c-11ce-ade5-00aa0044773d")]
			[SuppressUnmanagedCodeSecurity]
			public interface ISequentialStream
			{
				void Read(
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out byte[] pv,
					uint cb,
					out uint pcbRead);
				void Write(
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pv,
					uint cb,
					out uint pcbWritten);
			}
		}
	}
}

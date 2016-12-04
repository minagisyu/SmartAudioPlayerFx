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
			[Guid("9F762FA7-A22E-428d-93C9-AC82F3AAFE5A")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMReaderAllocatorEx
			{
				void AllocateForStreamEx(
					ushort wStreamNum,
					uint cbBuffer,
					out INSSBuffer ppBuffer,
					uint dwFlags,
					ulong cnsSampleTime,
					ulong cnsSampleDuration,
					IntPtr pvContext);
				void AllocateForOutputEx(
					uint dwOutputNum,
					uint cbBuffer,
					out INSSBuffer ppBuffer,
					uint dwFlags,
					ulong cnsSampleTime,
					ulong cnsSampleDuration,
					IntPtr pvContext);
			}
		}
	}
}

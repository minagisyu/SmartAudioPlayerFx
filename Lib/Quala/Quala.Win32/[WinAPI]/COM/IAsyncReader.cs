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
			[Guid("56a868aa-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IAsyncReader
			{
				void RequestAllocator(
					IMemAllocator pPreferred,
					ref ALLOCATOR_PROPERTIES pProps,
					out IMemAllocator ppActual);
				void Request(IMediaSample pSample, IntPtr dwUser);
				void WaitForNext(uint dwTimeout, out IMediaSample ppSample, out IntPtr pdwUser);
				void SyncReadAligned(IMediaSample pSample);
				void SyncRead(long llPosition, int lLength, out byte[] pBuffer);
				void Length(out long pTotal, out long pAvailable);
				void BeginFlush();
				void EndFlush();
			}
		}
	}
}

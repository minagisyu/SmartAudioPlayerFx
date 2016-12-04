using System;
using System.Runtime.InteropServices;
using System.Security;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace Quala.Win32
{
	partial class WinAPI
	{
		partial class COM
		{
			[ComImport]
			[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			[Guid("56a8689c-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMemAllocator
			{
				void SetProperties(
					ref ALLOCATOR_PROPERTIES pRequest,
					out ALLOCATOR_PROPERTIES pActual);
				void GetProperties(out ALLOCATOR_PROPERTIES pProps);
				void Commit();
				void Decommit();
				void GetBuffer(out IMediaSample ppBuffer, ref long pStartTime, ref long pEndTime, uint dwFlags);
				void ReleaseBuffer(IMediaSample pBuffer);
			}

			public struct ALLOCATOR_PROPERTIES
			{
				public int cBuffers;
				public int cbBuffer;
				public int cbAlign;
				public int cbPrefix;
			}
		}
	}
}

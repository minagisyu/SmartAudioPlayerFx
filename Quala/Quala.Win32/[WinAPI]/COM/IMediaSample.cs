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
			[Guid("56a8689a-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMediaSample
			{
				void GetPointer(out byte[] ppBuffer);
				[PreserveSig]
				int GetSize();
				void GetTime(out long pTimeStart, out long pTimeEnd);
				void SetTime(ref long pTimeStart, ref long pTimeEnd);
				[PreserveSig]
				int IsSyncPoint();
				void SetSyncPoint(bool bIsSyncPoint);
				[PreserveSig]
				int IsPreroll();
				[PreserveSig]
				int SetPreroll(bool bIsPreroll);
				[PreserveSig]
				int GetActualDataLength();
				void SetActualDataLength(int __MIDL_0010);
				void GetMediaType(out AM_MEDIA_TYPE ppMediaType);
				void SetMediaType([In, Out] AM_MEDIA_TYPE pMediaType);
				[PreserveSig]
				int IsDiscontinuity();
				void SetDiscontinuity(bool bDiscontinuity);
				void GetMediaTime(out long pTimeStart, out long pTimeEnd);
				void SetMediaTime(ref long pTimeStart, ref long pTimeEnd);
			}
		}
	}
}

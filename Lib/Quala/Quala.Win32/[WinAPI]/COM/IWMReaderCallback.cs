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
			[Guid("96406BD8-2B2B-11d3-B36B-00C04F6108FF")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMReaderCallback
			{
				// IWMStatusCallback
				void OnStatus(
					WMT_STATUS Status,
					int hr,
					WMT_ATTR_DATATYPE dwType,
					IntPtr pValue,
					IntPtr pvContext);

				// IWMReaderCallback
				void OnSample(
					uint dwOutputNum,
					ulong cnsSampleTime,
					ulong cnsSampleDuration,
					uint dwFlags,
					INSSBuffer pSample,
					IntPtr pvContext);
			}
		}
	}
}

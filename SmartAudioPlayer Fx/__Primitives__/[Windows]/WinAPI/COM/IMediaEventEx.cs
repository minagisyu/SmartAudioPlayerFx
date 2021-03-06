using System;
using System.Runtime.InteropServices;
using System.Security;

namespace __Primitives__
{
	partial class WinAPI
	{
		partial class COM
		{
			[ComImport]
			[InterfaceType(ComInterfaceType.InterfaceIsDual)]
			[Guid("56a868c0-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMediaEventEx
			{
				// IMediaEvent
				void GetEventHandle(out IntPtr hEvent);
				[PreserveSig]
				COMRESULT GetEvent(out EC lEventCode, out IntPtr lParam1, out IntPtr lParam2, int msTimeout);
				void WaitForCompletion(int msTimeout, out EC pEvCode);
				void CancelDefaultHandling(EC lEvCode);
				void RestoreDefaultHandling(EC lEvCode);
				void FreeEventParams(EC lEvCode, IntPtr lParam1, IntPtr lParam2);

				// IMediaEventEx
				void SetNotifyWindow(IntPtr hwnd, int lMsg, IntPtr lInstanceData);
				void SetNotifyFlags(int lNoNotifyFlags);
				void GetNotifyFlags(out int lplNoNotifyFlags);
			}
		}
	}
}

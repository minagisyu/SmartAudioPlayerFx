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
			[Guid("56a86897-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IReferenceClock
			{
				void GetTime(out long pTime);
				void AdviseTime(long baseTime, ulong streamTime, SafeHandle hEvent, out IntPtr pdwAdviseCookie);
				void AdvisePeriodic(long startTime, long periodTime, SafeHandle hSemaphore, out IntPtr pdwAdviseCookie);
				void Unadvise(IntPtr dwAdviseCookie);
			}
		}
	}
}

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
			[Guid("36b73880-c2c8-11cf-8b46-00805f6cef60")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMediaSeeking
			{
				void GetCapabilities(out uint pCapabilities);
				void CheckCapabilities(ref uint pCapabilities);
				void IsFormatSupported(ref Guid pFormat);
				void QueryPreferredFormat(out Guid pFormat);
				void GetTimeFormat(out Guid pFormat);
				void IsUsingTimeFormat(ref Guid pFormat);
				void SetTimeFormat(ref Guid pFormat);
				void GetDuration(out long pDuration);
				void GetStopPosition(out long pStop);
				void GetCurrentPosition(out long pCurrent);
				void ConvertTimeFormat(out long pTarget, ref Guid pTargetFormat, long Source, Guid pSourceFormat);
				void SetPositions(ref long pCurrent, uint dwCurrentFlags, ref long pStop, uint dwStopFlags);
				void GetPositions(out long pCurrent, out long pStop);
				void GetAvailable(out long pEarliest, out long pLatest);
				void SetRate(double dRate);
				void GetRate(out double pdRate);
				void GetPreroll(out long pllPreroll);
			}
		}
	}
}

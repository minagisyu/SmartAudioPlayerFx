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
			[Guid("56a86899-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMediaFilter
			{
				// IPersist
				void GetClassID(out Guid pClassID);

				// IMediaFilter
				void Stop();
				void Pause();
				void Run(Int64 tStart);
				void GetState(uint dwMilliSecsTimeout, out FILTER_STATE State);
				void SetSyncSource(IReferenceClock pClock);
				void GetSyncSource(out IReferenceClock pClock);
			}

			public enum FILTER_STATE
			{
				State_Stopped = 0,
				State_Paused = State_Stopped + 1,
				State_Running = State_Paused + 1
			}
		}
	}
}

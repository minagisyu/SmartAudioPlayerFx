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
			[Guid("56a86895-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IBaseFilter
			{
				// IPersist
				void GetClassID(out Guid pClassID);

				// IMediaFilter
				void Stop();
				void Pause();
				void Run(Int64 tStart);
				void GetState(uint dwMilliSecsTimeout, out FILTER_STATE state);
				void SetSyncSource(IReferenceClock pClock);
				void GetSyncSource(out IReferenceClock pClock);

				// IBaseFilter
				void EnumPins(out IEnumPins ppEnum);
				void FindPin(string Id, out IPin ppPin);
				void QueryFilterInfo(out FILTER_INFO pInfo);
				void JoinFilterGraph(IFilterGraph pGraph, string pName);
				void QueryVendorInfo(out string pVendorInfo);
			}
		}
	}
}

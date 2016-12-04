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
			[Guid("56a8689f-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IFilterGraph
			{
				void AddFilter(IBaseFilter pFilter, string pName);
				void RemoveFilter(IBaseFilter pFilter);
				void EnumFilters(out IEnumFilters ppEnum);
				void FindFilterByName(string pName, out IBaseFilter ppFilter);
				void ConnectDirect(IPin ppinOut, IPin ppinIn, [In, Out] AM_MEDIA_TYPE pmt);
				void Reconnect(IPin ppin);
				void Disconnect(IPin ppin);
				void SetDefaultSyncSource();
			}
		}
	}
}

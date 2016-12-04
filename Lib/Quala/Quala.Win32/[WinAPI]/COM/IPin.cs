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
			[Guid("56a86891-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IPin
			{
				void Connect(IPin pReceivePin, ref AM_MEDIA_TYPE pmt);
				void ReceiveConnection(IPin pConnector, ref AM_MEDIA_TYPE pmt);
				void Disconnect();
				void ConnectedTo(out IPin pPin);
				void ConnectionMediaType(out AM_MEDIA_TYPE pmt);
				void QueryPinInfo(out PIN_INFO pInfo);
				void QueryDirection(out PIN_DIRECTION pPinDir);
				void QueryId(out string Id);
				void QueryAccept(ref AM_MEDIA_TYPE pmt);
				void EnumMediaTypes(out IEnumMediaTypes ppEnum);
				void QueryInternalConnections(out IPin apPin, out uint nPin);
				void EndOfStream();
				void BeginFlush();
				void EndFlush();
				void NewSegment(Int64 tStart, Int64 tStop, double dRate);
			}
		}
	}
}

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
			[Guid("96406BEA-2B2B-11d3-B36B-00C04F6108FF")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMReaderAdvanced
			{
				void SetUserProvidedClock(bool fUserClock);
				void GetUserProvidedClock(out bool pfUserClock);
				void DeliverTime(ulong cnsTime);
				void SetManualStreamSelection(bool fSelection);
				void GetManualStreamSelection(out bool pfSelection);
				void SetStreamsSelected(ushort cStreamCount, short pwStreamNumbers, WMT_STREAM_SELECTION pSelections);
				void GetStreamSelected(ushort wStreamNum, out WMT_STREAM_SELECTION pSelection);
				void SetReceiveSelectionCallbacks(bool fGetCallbacks);
				void GetReceiveSelectionCallbacks(out bool pfGetCallbacks);
				void SetReceiveStreamSamples(ushort wStreamNum, bool fReceiveStreamSamples);
				void GetReceiveStreamSamples(ushort wStreamNum, out bool pfReceiveStreamSamples);
				void SetAllocateForOutput(uint dwOutputNum, bool fAllocate);
				void GetAllocateForOutput(uint dwOutputNum, out bool pfAllocate);
				void SetAllocateForStream(ushort wStreamNum, bool fAllocate);
				void GetAllocateForStream(ushort dwSreamNum, out bool pfAllocate);
				void GetStatistics(ref WM_READER_STATISTICS pStatistics);
				void SetClientInfo(ref WM_READER_CLIENTINFO pClientInfo);
				void GetMaxOutputSampleSize(uint dwOutput, out uint pcbMax);
				void GetMaxStreamSampleSize(ushort wStream, out uint pcbMax);
				void NotifyLateDelivery(ulong cnsLateness);
			}

			public enum WMT_STREAM_SELECTION
			{
				WMT_OFF = 0,
				WMT_CLEANPOINT_ONLY = 1,
				WMT_ON = 2
			}

			public struct WM_READER_STATISTICS
			{
				public uint cbSize;
				public uint dwBandwidth;
				public uint cPacketsReceived;
				public uint cPacketsRecovered;
				public uint cPacketsLost;
				public ushort wQuality;
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct WM_READER_CLIENTINFO
			{
				public uint cbSize;
				public string wszLang;
				public string wszBrowserUserAgent;
				public string wszBrowserWebPage;
				public ulong qwReserved;
				public IntPtr pReserved;
				public string wszHostExe;
				public ulong qwHostVersion;
				public string wszPlayerUserAgent;

			}
		}
	}
}

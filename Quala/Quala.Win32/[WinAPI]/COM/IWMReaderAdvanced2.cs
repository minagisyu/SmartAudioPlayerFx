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
			[Guid("ae14a945-b90c-4d0d-9127-80d665f7d73e")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMReaderAdvanced2
			{
				// IWMReaderAdvanced
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
				void GetMaxStreamSampleSize(short wStream, out uint pcbMax);
				void NotifyLateDelivery(ulong cnsLateness);

				// IWMReaderAdvanced2
				void SetPlayMode(WMT_PLAY_MODE Mode);
				void GetPlayMode(out WMT_PLAY_MODE pMode);
				void GetBufferProgress(out uint pdwPercent, out ulong pcnsBuffering);
				void GetDownloadProgress(out uint pdwPercent, out ulong pqwBytesDownloaded, out ulong pcnsDownload);
				void GetSaveAsProgress(out uint pdwPercent);
				void SaveFileAs(string pwszFilename);
				void GetProtocolName(out string pwszProtocol, out uint pcchProtocol);
				void StartAtMarker(short wMarkerIndex, ulong cnsDuration, float fRate, IntPtr pvContext);
				void GetOutputSetting(uint dwOutputNum, string pszName, out WMT_ATTR_DATATYPE pType, out byte[] pValue, ref short pcbLength);
				void SetOutputSetting(uint dwOutputNum, string pszName, WMT_ATTR_DATATYPE Type, byte[] pValue, short cbLength);
				void Preroll(ulong cnsStart, ulong cnsDuration, float fRate);
				void SetLogClientID(bool fLogClientID);
				void GetLogClientID(out bool pfLogClientID);
				void StopBuffering();
				void OpenStream(IStream pStream, IWMReaderCallback pCallback, IntPtr pvContext);
			}

			public enum WMT_PLAY_MODE
			{
				WMT_PLAY_MODE_AUTOSELECT = 0,
				WMT_PLAY_MODE_LOCAL = 1,
				WMT_PLAY_MODE_DOWNLOAD = 2,
				WMT_PLAY_MODE_STREAMING = 3
			}
		}
	}
}

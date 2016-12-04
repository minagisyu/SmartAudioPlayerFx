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
			[Guid("faed3d21-1b6b-4af7-8cb6-3e189bbc187b")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMSyncReader2
			{
				// IWMSyncReader
				void Open(string pwszFilename);
				void Close();
				void SetRange(ulong cnsStartTime, long cnsDuration);
				void SetRangeByFrame(
					ushort wStreamNum,
					ulong qwFrameNumber,
					long cFramesToRead);
				void GetNextSample(
					ushort wStreamNum,
					out INSSBuffer ppSample,
					out ulong pcnsSampleTime,
					out ulong pcnsDuration,
					out uint pdwFlags,
					out uint pdwOutputNum,
					out ushort pwStreamNum);
				void SetStreamsSelected(
					ushort cStreamCount,
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			ushort[] pwStreamNumbers,
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			WMT_STREAM_SELECTION[] pSelections);
				void GetStreamSelected(
					ushort wStreamNum,
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			out WMT_STREAM_SELECTION[] pSelection);
				void SetReadStreamSamples(ushort wStreamNum, bool fCompressed);
				void GetReadStreamSamples(ushort wStreamNum, out bool pfCompressed);
				void GetOutputSetting(
					uint dwOutputNum,
					string pszName,
					out WMT_ATTR_DATATYPE pType,
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
			byte[] pValue,
					ref ushort pcbLength);
				void SetOutputSetting(
					uint dwOutputNum,
					string pszName,
					WMT_ATTR_DATATYPE Type,
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]
			byte[] pValue,
					ushort cbLength);
				void GetOutputCount(out uint pcOutputs);
				void GetOutputProps(uint dwOutputNum, out IWMOutputMediaProps ppOutput);
				void SetOutputProps(uint dwOutputNum, IWMOutputMediaProps pOutput);
				void GetOutputFormatCount(uint dwOutputNum, out uint pcFormats);
				void GetOutputFormat(uint dwOutputNum, uint dwFormatNum, out IWMOutputMediaProps ppProps);
				void GetOutputNumberForStream(ushort wStreamNum, out uint pdwOutputNum);
				void GetStreamNumberForOutput(uint dwOutputNum, out ushort pwStreamNum);
				void GetMaxOutputSampleSize(uint dwOutput, out uint pcbMax);
				void GetMaxStreamSampleSize(ushort wStream, out uint pcbMax);
				void OpenStream(IStream pStream);

				// IWMSyncReader2
				void SetRangeByTimecode(
					ushort wStreamNum,
					ref WMT_TIMECODE_EXTENSION_DATA pStart,
					ref WMT_TIMECODE_EXTENSION_DATA pEnd);
				void SetRangeByFrameEx(
					ushort wStreamNum,
					ulong qwFrameNumber,
					long cFramesToRead,
					out ulong pcnsStartTime);
				void SetAllocateForOutput(uint dwOutputNum, IWMReaderAllocatorEx pAllocator);
				void GetAllocateForOutput(uint dwOutputNum, out IWMReaderAllocatorEx ppAllocator);
				void SetAllocateForStream(ushort wStreamNum, IWMReaderAllocatorEx pAllocator);
				void GetAllocateForStream(ushort dwSreamNum, out IWMReaderAllocatorEx ppAllocator);
			}

			[StructLayout(LayoutKind.Sequential, Pack = 2)]
			public struct WMT_TIMECODE_EXTENSION_DATA
			{
				public ushort wRange;
				public uint dwTimecode;
				public uint dwUserbits;
				public uint dwAmFlags;
			}
		}
	}
}

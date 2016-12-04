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
			[Guid("9397F121-7705-4dc9-B049-98B698188414")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMSyncReader
			{
				void Open(string pwszFilename);
				void Close();
				void SetRange(ulong cnsStartTime, long cnsDuration);
				void SetRangeByFrame(ushort wStreamNum, ulong qwFrameNumber, long cFramesToRead);
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
				void GetOutputFormat(
					uint dwOutputNum,
					uint dwFormatNum,
					out IWMOutputMediaProps ppProps);
				void GetOutputNumberForStream(ushort wStreamNum, out uint pdwOutputNum);
				void GetStreamNumberForOutput(uint dwOutputNum, out ushort pwStreamNum);
				void GetMaxOutputSampleSize(uint dwOutput, out uint pcbMax);
				void GetMaxStreamSampleSize(ushort wStream, out uint pcbMax);
				void OpenStream(IStream pStream);
			}
		}
	}
}

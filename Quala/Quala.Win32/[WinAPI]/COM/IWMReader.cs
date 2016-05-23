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
			[Guid("96406BD6-2B2B-11d3-B36B-00C04F6108FF")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMReader
			{
				void Open(string pwszURL, IWMReaderCallback pCallback, IntPtr pvContext);
				void Close();
				void GetOutputCount(out uint pcOutputs);
				void GetOutputProps(uint dwOutputNum, out IWMOutputMediaProps ppOutput);
				void SetOutputProps(uint dwOutputNum, IWMOutputMediaProps pOutput);
				void GetOutputFormatCount(uint dwOutputNumber, out uint pcFormats);
				void GetOutputFormat(uint dwOutputNumber, uint dwFormatNumber, out IWMOutputMediaProps ppProps);
				void Start(ulong cnsStart, ulong cnsDuration, float fRate, IntPtr pvContext);
				void Stop();
				void Pause();
				void Resume();
			}
		}
	}
}

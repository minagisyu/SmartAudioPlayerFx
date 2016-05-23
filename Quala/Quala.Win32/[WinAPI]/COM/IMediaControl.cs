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
			[InterfaceType(ComInterfaceType.InterfaceIsDual)]
			[Guid("56a868b1-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMediaControl
			{
				void Run();
				void Pause();
				void Stop();
				void GetState(int msTimeout, out int pfs);
				void RenderFile(string strFilename);
				void AddSourceFilter(
					string strFilename,
					[MarshalAs(UnmanagedType.IDispatch)]
			out object ppUnk);
				[PreserveSig]
				int get_FilterCollection([MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);
				[PreserveSig]
				int get_RegFilterCollection([MarshalAs(UnmanagedType.IDispatch)] out object ppUnk);
				void StopWhenReady();
			}
		}
	}
}

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
			[Guid("56a868b3-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IBasicAudio
			{
				void put_Volume(int lVolume);
				void get_Volume(out int plVolume);
				void put_Balance(int lBalance);
				void get_Balance(out int plBalance);
			}
		}
	}
}

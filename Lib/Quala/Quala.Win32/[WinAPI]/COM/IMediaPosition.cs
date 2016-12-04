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
			[Guid("56a868b2-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMediaPosition
			{
				void get_Duration(out double plength);
				void put_CurrentPosition(double llTime);
				void get_CurrentPosition(out double pllTime);
				void get_StopTime(out double pllTime);
				void put_StopTime(double llTime);
				void get_PrerollTime(out double pllTime);
				void put_PrerollTime(double llTime);
				void put_Rate(double dRate);
				void get_Rate(out double pdRate);
				void CanSeekForward(out int pCanSeekForward);
				void CanSeekBackward(out int pCanSeekBackward);
			}
		}
	}
}

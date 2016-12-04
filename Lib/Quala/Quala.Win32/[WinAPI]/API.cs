using System;
using System.Security;

namespace Quala.Win32
{
	[SuppressUnmanagedCodeSecurity]
	public static partial class WinAPI
	{
		public const int TRUE = 1;
		public const int FALSE = 0;
		public const uint UINT_MAX = 0xffffffff;
		public const short WHEEL_DELTA = 120;
		public const uint WHEEL_PAGESCROLL = UINT_MAX;

		public static short GET_WHEEL_DELTA_WPARAM(IntPtr wParam)
		{
			return (short)((((int)wParam) & 0xffff0000) >> 16);
		}

		public static COM.COMRESULT MAKE_HRESULT(COM.COMRESULT severity, int fac, int code)
		{
			return (COM.COMRESULT)(((int)(severity) << 31) | (fac << 16) | (code));
		}

	}
}

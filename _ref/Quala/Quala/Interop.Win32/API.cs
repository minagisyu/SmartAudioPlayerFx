using System;
using System.Security;
using Quala.Interop.Win32.COM;

namespace Quala.Interop.Win32
{
	[SuppressUnmanagedCodeSecurity]
	public static partial class API
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

		public static COMRESULT MAKE_HRESULT(COMRESULT severity, int fac, int code)
		{
			return (COMRESULT)(((int)(severity) << 31) | (fac << 16) | (code));
		}

	}
}

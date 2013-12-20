using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Quala.Interop.Win32.COM
{
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("00000114-0000-0000-C000-000000000046")]
	[SuppressUnmanagedCodeSecurity]
	public interface IOleWindow
	{
		void GetWindow(out IntPtr phWnd);
		void ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool fEnterMode);
	}
}

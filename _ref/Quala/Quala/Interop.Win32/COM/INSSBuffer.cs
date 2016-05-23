using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Quala.Interop.Win32.COM
{
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("E1CD3524-03D7-11d2-9EED-006097D2D7CF")]
	[SuppressUnmanagedCodeSecurity]
	public interface INSSBuffer
	{
		void GetLength(out uint pdwLength);
		void SetLength(uint dwLength);
		void GetMaxLength(out uint pdwLength);
		void GetBuffer(out IntPtr ppdwBuffer);
		void GetBufferAndLength(out IntPtr ppdwBuffer, out uint pdwLength);
	}
}

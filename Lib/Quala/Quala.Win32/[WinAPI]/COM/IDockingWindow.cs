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
			[Guid("012DD920-7B26-11D0-8CA9-00A0C92DBFE8")]
			[SuppressUnmanagedCodeSecurity]
			public interface IDockingWindow
			{
				// IOleWindow
				void GetWindow(out IntPtr phWnd);
				void ContextSensitiveHelp(bool fEnterMode);

				// IDockingWindow
				void ShowDW(bool fShow);
				void CloseDW(int dwReserved);
				void ResizeBorderDW(
					ref RECT prcBorder,
					[MarshalAs(UnmanagedType.IUnknown)]
			object punkToolbarSite,
					bool fReserved);
			}
		}
	}
}

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
			[Guid("68284FAA-6A48-11D0-8C78-00C04FD918B4")]
			[SuppressUnmanagedCodeSecurity]
			public interface IInputObject
			{
				void UIActivateIO([MarshalAs(UnmanagedType.Bool)] bool fActivate, ref MSG msg);
				[PreserveSig]
				COMRESULT HasFocusIO();
				[PreserveSig]
				COMRESULT TranslateAcceleratorIO(ref MSG msg);
			}
		}
	}
}

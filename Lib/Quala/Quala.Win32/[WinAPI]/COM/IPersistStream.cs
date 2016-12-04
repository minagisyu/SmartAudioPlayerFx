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
			[Guid("00000109-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IPersistStream
			{
				// IPersist
				void GetClassID(out Guid pClassID);

				// IPersistStream
				[PreserveSig]
				COMRESULT IsDirty();
				void Load([MarshalAs(UnmanagedType.Interface)] object pStream);
				void Save([MarshalAs(UnmanagedType.Interface)] object pStream,
					[MarshalAs(UnmanagedType.Bool)] bool fClearDirty);
				void GetSizeMax(out long pcbSize);
			}
		}
	}
}

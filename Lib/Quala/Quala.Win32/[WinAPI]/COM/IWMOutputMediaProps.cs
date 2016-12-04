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
			[Guid("96406BD7-2B2B-11d3-B36B-00C04F6108FF")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMOutputMediaProps
			{
				// IWMMediaProps
				void GetType(out Guid pguidType);
				void GetMediaType(out WM_MEDIA_TYPE pType, ref uint pcbType);
				void SetMediaType(ref WM_MEDIA_TYPE pType);

				// IWMOutputMediaProps
				void GetStreamGroupName(out string pwszName, ref short pcchName);
				void GetConnectionName(out string pwszName, ref short pcchName);
			}
		}
	}
}

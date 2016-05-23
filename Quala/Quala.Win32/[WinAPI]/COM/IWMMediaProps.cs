using System;
using System.Runtime.InteropServices;
using System.Security;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace Quala.Win32
{
	partial class WinAPI
	{
		partial class COM
		{
			[ComImport]
			[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			[Guid("96406BCE-2B2B-11d3-B36B-00C04F6108FF")]
			[SuppressUnmanagedCodeSecurity]
			public interface IWMMediaProps
			{
				void GetType(out Guid pguidType);
				void GetMediaType(out WM_MEDIA_TYPE pType, ref uint pcbType);
				void SetMediaType(ref WM_MEDIA_TYPE pType);
			}

			public struct WM_MEDIA_TYPE
			{
				public Guid majortype;
				public Guid subtype;
				public bool bFixedSizeSamples;
				public bool bTemporalCompression;
				public uint lSampleSize;
				public Guid formattype;
				public IntPtr pUnk;
				public uint cbFormat;
				public IntPtr pbFormat;
			}
		}
	}
}

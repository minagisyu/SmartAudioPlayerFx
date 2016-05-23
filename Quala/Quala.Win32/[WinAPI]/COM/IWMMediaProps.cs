using System;
using System.Runtime.InteropServices;
using System.Security;

// �uwarning CS0649: �t�B�[���h 'xxx' �͊��蓖�Ă��܂���B��Ɋ���l ���g�p���܂��B�v�̗}���B
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

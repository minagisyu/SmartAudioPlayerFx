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
			[Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
			[SuppressUnmanagedCodeSecurity]
			public interface IShellItemArray
			{
				void BindToHandler(IBindCtx pbc, Guid rbhid, Guid riid,
					[MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 2)]
			object ppvOut);
				void GetPropertyStore(GETPROPERTYSTOREFLAGS flags, Guid riid,
					[MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)]
			out object ppv);
				void GetPropertyDescriptionList(ref PROPERTYKEY keyType, Guid riid,
					[MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)]
			out object ppv);
				void GetAttributes(SIATTRIBFLAGS dwAttribFlags,
					uint sfgaoMask, out uint psfgaoAttribs);
				void GetCount(out uint pdwNumItems);
				void GetItemAt(uint dwIndex, out IShellItem ppsi);
				void EnumItems(out IEnumShellItems ppenumShellItems);
			}

			[Flags]
			public enum GETPROPERTYSTOREFLAGS : int
			{
				DEFAULT = 0,
				HANDLERPROPERTIESONLY = 0x1,
				READWRITE = 0x2,
				TEMPORARY = 0x4,
				FASTPROPERTIESONLY = 0x8,
				OPENSLOWITEM = 0x10,
				DELAYCREATION = 0x20,
				BESTEFFORT = 0x40,
				MASK_VALID = 0x7f
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct PROPERTYKEY
			{
				public Guid fmtid;
				public uint pid;
			}

			public enum SIATTRIBFLAGS
			{
				AND = 0x1,
				OR = 0x2,
				APPCOMPAT = 0x3,
				MASK = 0x3
			}
		}
	}
}

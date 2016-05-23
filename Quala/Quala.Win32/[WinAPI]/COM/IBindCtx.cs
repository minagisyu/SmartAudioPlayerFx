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
			[Guid("0000000e-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IBindCtx
			{
				void RegisterObjectBound([MarshalAs(UnmanagedType.IUnknown)] object punk);
				void RevokeObjectBound([MarshalAs(UnmanagedType.IUnknown)] object punk);
				void ReleaseBoundObjects();
				void SetBindOptions(ref BIND_OPTS pbindopts);
				void GetBindOptions(ref BIND_OPTS pbindopts);
				void GetRunningObjectTable(out IRunningObjectTable pprot);
				void RegisterObjectParam(string pszKey,
					[MarshalAs(UnmanagedType.IUnknown)] object punk);
				void GetObjectParam(string pszKey,
					[MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
				void EnumObjectParam(out IEnumString ppenum);
				void RevokeObjectParam(string pszKey);
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct BIND_OPTS
			{
				public uint cbStruct;
				public uint grfFlags;
				public uint grfMode;
				public uint dwTickCountDeadline;
			}
		}
	}
}

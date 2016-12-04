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
			[Guid("00000010-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IRunningObjectTable
			{
				void Register(uint grfFlags,
					[MarshalAs(UnmanagedType.IUnknown)]
			object punkObject,
					IMoniker pmkObjectName, out uint pdwRegister);
				void Revoke(uint dwRegister);
				void IsRunning(IMoniker pmkObjectName);
				void GetObject(IMoniker pmkObjectName,
					[MarshalAs(UnmanagedType.IUnknown)]
			out object ppunkObject);
				void NoteChangeTime(uint dwRegister, ref FILETIME pfiletime);
				void GetTimeOfLastChange(IMoniker pmkObjectName, out FILETIME pfiletime);
				void EnumRunning(out IEnumMoniker ppenumMoniker);
			}
		}
	}
}

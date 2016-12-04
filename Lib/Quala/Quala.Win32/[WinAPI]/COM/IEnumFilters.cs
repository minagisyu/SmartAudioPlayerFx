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
			[Guid("56a86893-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IEnumFilters
			{
				void Next(
					uint cFilters,
					[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			IBaseFilter[] ppFilter,
					out uint pcFetched);
				void Skip(uint cFilters);
				void Reset();
				void Clone(out IEnumFilters ppEnum);
			}
		}
	}
}

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
			[Guid("56a86892-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IEnumPins
			{
				void Next(
					uint cPins,
					[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			IPin[] ppPins,
					out uint pcFetched);
				void Skip(uint cPins);
				void Reset();
				void Clone(out IEnumPins ppEnum);
			}
		}
	}
}

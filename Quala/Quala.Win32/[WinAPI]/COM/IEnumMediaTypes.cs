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
			[Guid("89c31040-846b-11ce-97d3-00aa0055595a")]
			[SuppressUnmanagedCodeSecurity]
			public interface IEnumMediaTypes
			{
				void Next(
					uint cMediaTypes,
					[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			AM_MEDIA_TYPE[] ppMediaTypes,
					out uint pcFetched);
				void Skip(uint cMediaTypes);
				void Reset();
				void Clone(out IEnumMediaTypes ppEnum);
			}
		}
	}
}

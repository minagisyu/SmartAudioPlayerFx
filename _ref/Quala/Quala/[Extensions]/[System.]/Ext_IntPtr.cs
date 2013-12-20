using System;
using System.Runtime.InteropServices;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// memcmp
		/// </summary>
		/// <param name="l"></param>
		/// <param name="r"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static bool MemCmp(this IntPtr left, IntPtr right, uint size)
		{
			for(int n = 0; n < size; n++)
				if(Marshal.ReadIntPtr(left, n) != Marshal.ReadIntPtr(right, n))
					return false;
			return true;
		}

		/// <summary>
		/// memcpy
		/// </summary>
		/// <param name="source"></param>
		/// <param name="dest"></param>
		/// <param name="size"></param>
		public static void MemCpy(this IntPtr source, IntPtr dest, uint size)
		{
			for(int n = 0; n < size; n++)
				Marshal.WriteIntPtr(dest, n, Marshal.ReadIntPtr(source, n));
		}
	}
}

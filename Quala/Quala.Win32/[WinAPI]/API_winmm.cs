using System;
using System.Runtime.InteropServices;
using System.Text;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace Quala.Win32
{
	partial class WinAPI
	{
		const string Winmm = "winmm.dll";

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern MMRESULT mmioAscend(IntPtr hmmio, ref MMCKINFO pmmcki, MMIO_Ascend fuAscend);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern MMRESULT mmioClose(IntPtr hmmio, MMIO_Close fuClose);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern MMRESULT mmioDescend(IntPtr hmmio, ref MMCKINFO pmmcki, ref MMCKINFO pmmckiParent, MMIO_Descend fuDescend);

		public static uint mmioFOURCC(string fourcc)
		{
			byte[] ch = Encoding.ASCII.GetBytes(fourcc);
			return (uint)((ch[0]) | (ch[1] << 8) | (ch[2] << 16) | (ch[3] << 24));
		}

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern MMRESULT mmioDescend(IntPtr hmmio, ref MMCKINFO pmmcki, IntPtr pmmckiParent, MMIO_Descend fuDescend);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern IntPtr mmioOpen(string pszFileName, ref MMIOINFO pmmioinfo, MMIO_SHAREMODE fdwOpen);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern int mmioRead(IntPtr hmmio, IntPtr pch, int cch);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern int mmioRead(IntPtr hmmio, StringBuilder pch, int cch);

		public static int mmioRead(IntPtr hmmio, out byte[] pch, int cch)
		{
			pch = new byte[cch];
			GCHandle handle = GCHandle.Alloc(pch, GCHandleType.Pinned);
			int ret;
			try { ret = mmioRead(hmmio, handle.AddrOfPinnedObject(), cch); }
			finally { handle.Free(); }
			return ret;
		}

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern int mmioSeek(IntPtr hmmio, int lOffset, MMIO_Seek iOrigin);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern MMRESULT timeBeginPeriod(uint period);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern MMRESULT timeEndPeriod(uint period);

		[DllImport(Winmm, CharSet = CharSet.Auto)]
		public static extern MMRESULT timeGetDevCaps(out TIMECAPS caps, uint size);

		public static MMRESULT timeGetDevCaps(out TIMECAPS caps)
		{
			return timeGetDevCaps(out caps, (uint)Marshal.SizeOf(typeof(TIMECAPS)));
		}

		public delegate IntPtr MMIOPROC(string lpmmioinfo, uint uMsg, IntPtr lParam1, IntPtr lParam2);

		[Flags]
		public enum MMIO_Ascend
		{
			None = 0,
		}

		[Flags]
		public enum MMIO_Close
		{
			None = 0,
			FHOPEN = 0x0010,
		}

		[Flags]
		public enum MMIO_Descend
		{
			None = 0,
			FINDCHUNK = 0x0010,
			FINDRIFF = 0x0020,
			FINDLIST = 0x0040,
		}

		public enum MMIO_Seek
		{
			SET = 0,
			CUR = 1,
			END = 2,
		}

		[Flags]
		public enum MMIO_SHAREMODE
		{
			COMPAT = 0x00000000,
			EXCLUSIVE = 0x00000010,
			DENYWRITE = 0x00000020,
			DENYREAD = 0x00000030,
			DENYNONE = 0x00000040,
		}

		public struct MMCKINFO
		{
			public uint ckid;
			public uint cksize;
			public uint fccType;
			public uint dwDataOffset;
			public uint dwFlags;
		}

		public struct MMIOINFO
		{
			public uint dwFlags;
			public uint fccIOProc;
			public MMIOPROC pIOProc;
			public uint wErrorRet;
			public IntPtr htask;
			public int cchBuffer;
			public IntPtr pchBuffer;
			public IntPtr pchNext;
			public IntPtr pchEndRead;
			public IntPtr pchEndWrite;
			public int lBufOffset;
			public int lDiskOffset;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public uint[] adwInfo;
			public uint dwReserved1;
			public uint dwReserved2;
			public IntPtr hmmio;
		}

		public struct TIMECAPS
		{
			public uint wPeriodMin;
			public uint wPeriodMax;
		}
	}
}

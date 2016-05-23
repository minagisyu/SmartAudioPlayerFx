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
			[Guid("0000000c-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IStream
			{
				// ISequentialStream
				void Read(
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] out byte[] pv,
					uint cb,
					out uint pcbRead);
				void Write(
					[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pv,
					uint cb,
					out uint pcbWritten);

				// IStream
				void Seek(
					ulong dlibMove,
					uint dwOrigin,
					out ulong plibNewPosition);
				void SetSize(ulong libNewSize);
				void CopyTo(
					IStream pstm,
					ulong cb,
					out ulong pcbRead,
					out ulong pcbWritten);
				void Commit(uint grfCommitFlags);
				void Revert();
				void LockRegion(ulong libOffset, ulong cb, uint dwLockType);
				void UnlockRegion(ulong libOffset, ulong cb, uint dwLockType);
				void Stat(out STATSTG pstatstg, uint grfStatFlag);
				void Clone(out IStream ppstm);
			}

			public struct FILETIME
			{
				public uint dwLowDateTime;
				public uint dwHighDateTime;
			}

			public struct STATSTG
			{
				public string pwcsName;
				public uint type;
				public ulong cbSize;
				public FILETIME mtime;
				public FILETIME ctime;
				public FILETIME atime;
				public uint grfMode;
				public uint grfLocksSupported;
				public Guid clsid;
				public uint grfStateBits;
				public uint reserved;
			}
		}
	}
}

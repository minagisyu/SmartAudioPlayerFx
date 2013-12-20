using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace Quala.IO
{
	public enum CompressMode : short
	{
		None = 0,
		Default = 1,
		Lznt1 = 2,
	}
	public class FileCompressModeSetting
	{
		[DllImport("kernel32.dll", EntryPoint = "DeviceIoControl")]
		public static extern bool SetCompressMode(
		  IntPtr fileHandle, int code, ref CompressMode mode,
		  int bufSize, IntPtr notUsed1, int notUsed2,
		  out int returnedSize, IntPtr overlapped);
		[DllImport("kernel32.dll", EntryPoint = "DeviceIoControl")]
		public static extern bool GetCompressMode(
		  IntPtr fileHandle, int code, IntPtr notUsed1,
		  int notUsed2, out CompressMode mode, int bufSize,
		  out int returnedSize, IntPtr overlapped);
		public static bool SetFileCompressMode(string filePath,
											   CompressMode mode)
		{
			const int setCompress = 639040;
			if(!Enum.IsDefined(typeof(CompressMode), mode))
				throw new InvalidEnumArgumentException(
				  "mode", (int)mode, typeof(CompressMode));
			using(FileStream fs
				   = new FileStream(filePath, FileMode.Open,
									FileAccess.ReadWrite))
			{
				int returned;
				//.NET 2.0以降では、
				// ハンドルはより安全に扱い得るようになった。
				SafeHandle safeHandle = fs.SafeFileHandle;
				//SafeHandle.DangerousAddRefで加算した参照カウントを
				// Releaseしなければならないかどうか
				bool mustRelease = false;
				//続くtry-finallyをCERに指定。
				//CERは、ThreadAbortException等でも
				//問題なくfinally句を実行する効果を含む。
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					//SafeHandleの参照カウントをインクリメント。
					//trueが返る場合、DangerousReleaseするまで
					//ReleaseHandleが遅延される。
					safeHandle.DangerousAddRef(ref mustRelease);
					IntPtr handle = safeHandle.DangerousGetHandle();
					return SetCompressMode(
					  handle, setCompress, ref mode, 2,
					  IntPtr.Zero, 0, out returned, IntPtr.Zero);
				}
				finally
				{
					//必要な場合SafeHandleの参照カウントをデクリメント。
					if(mustRelease)
						safeHandle.DangerousRelease();
				}
			}
		}
		public static CompressMode GetFileCompressMode(string filePath)
		{
			const int getCompress = 589884;
			using(FileStream fs
					 = new FileStream(filePath, FileMode.Open,
									  FileAccess.Read))
			{
				CompressMode mode;
				int returned;

				//SafeHandleについてはSetFileCompressMode参照。
				SafeHandle safeHandle = fs.SafeFileHandle;
				bool mustRelease = false;
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					safeHandle.DangerousAddRef(ref mustRelease);
					IntPtr handle = safeHandle.DangerousGetHandle();
					GetCompressMode(handle, getCompress, IntPtr.Zero, 0,
									out mode, 2, out returned, IntPtr.Zero);
				}
				finally
				{
					if(mustRelease)
						safeHandle.DangerousRelease();
				}
				return mode;
			}
		}
	}
}

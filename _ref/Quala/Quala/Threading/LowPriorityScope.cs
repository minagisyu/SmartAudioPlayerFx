using System;
using Quala.Interop.Win32;

namespace Quala.Threading
{
	public sealed class LowPriorityScope : IDisposable
	{
		IntPtr handle = IntPtr.Zero;

		public LowPriorityScope()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
				Environment.OSVersion.Version.Major >= 6)
			{
				handle = API.GetCurrentThread();
				API.SetThreadPriority(handle, THREAD_PRIORITY.THREAD_MODE_BACKGROUND_BEGIN);
			}
		}

		~LowPriorityScope() { Dispose(false); }
		public void Dispose() { Dispose(true); }

		void Dispose(bool disposing)
		{
			if (handle != IntPtr.Zero)
			{
				API.SetThreadPriority(handle, THREAD_PRIORITY.THREAD_MODE_BACKGROUND_END);
				handle = IntPtr.Zero;
			}
			if (disposing)
			{
				GC.SuppressFinalize(this);
			}
		}
	}
}

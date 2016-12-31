using System;
using System.Threading;
using static OpenAL.ALC10;

namespace SmartAudioPlayer.MediaProcessor.Audio
{
	public abstract class ALSource : IDisposable
	{
		static int device_refcount = 0;
		static IntPtr pDevice;
		static IntPtr pContext;
		protected bool Disposed { get; private set; }
		public ALSourceFormat SourceFormat { get; }

		protected ALSource()
		{
			if (device_refcount <= 0)
			{
				pDevice = alcOpenDevice("OpenAL Soft");
				pContext = alcCreateContext(pDevice, null);
				alcMakeContextCurrent(pContext);
				device_refcount = 0;
			}
			Interlocked.Increment(ref device_refcount);
			SourceFormat = new ALSourceFormat();
		}

		#region Dispose

		~ALSource() => Dispose(false);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		protected virtual void Dispose(bool disposing)
		{
			if (Disposed) return;

			if (disposing)
			{
				// マネージリソースの破棄
			}

			// アンマネージリソースの破棄
			if (Interlocked.Decrement(ref device_refcount) >= 0)
			{
				alcMakeContextCurrent(IntPtr.Zero);
				alcDestroyContext(pContext);
				pContext = IntPtr.Zero;
				alcCloseDevice(pDevice);
				pDevice = IntPtr.Zero;
			}

			Disposed = true;
		}
	}
}

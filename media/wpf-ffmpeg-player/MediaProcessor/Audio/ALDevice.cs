using System;
using System.Reactive.Disposables;
using static OpenAL.ALC10;

namespace SmartAudioPlayer.MediaProcessor.Audio
{
	public unsafe sealed partial class ALDevice : IDisposable
	{
		// Device, Context, Listener
		IntPtr device, context;
		CompositeDisposable sources = new CompositeDisposable();
		bool disposed = false;

		public ALDevice()
		{
			device = alcOpenDevice(null/*"OpenAL Soft"*/);
			context = alcCreateContext(device, null);
			alcMakeContextCurrent(context);
		}

		#region Dispose

		~ALDevice() => Dispose(false);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		void Dispose(bool disposing)
		{
			if (disposed) return;

			if (disposing)
			{
				// マネージリソースの破棄
				sources.Dispose();
			}

			// アンマネージリソースの破棄
			alcMakeContextCurrent(IntPtr.Zero);
			alcDestroyContext(context);
			alcCloseDevice(device);

			disposed = true;
		}

		public StreamingSource CreateStreamingSource(int buffer_num)
		{
			var source = new StreamingSource(this, buffer_num);
			sources.Add(source);
			return source;
		}
	}
}

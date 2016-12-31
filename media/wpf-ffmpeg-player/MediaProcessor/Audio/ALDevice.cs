using System;
using static OpenAL.ALC10;

namespace SmartAudioPlayer.MediaProcessor.Audio
{
	public unsafe static partial class ALDevice
	{
		// Device, Context, Listener
		static IntPtr device, context;

		public static void Initialize()
		{
			if (device == IntPtr.Zero)
			{
				device = alcOpenDevice("OpenAL Soft");
			}
			if (context == IntPtr.Zero)
			{
				context = alcCreateContext(device, null);
				alcMakeContextCurrent(context);
			}
		}

		public static void Dispose()
		{
			// アンマネージリソースの破棄
			if (context != IntPtr.Zero)
			{
				alcMakeContextCurrent(IntPtr.Zero);
				alcDestroyContext(context);
				context = IntPtr.Zero;
			}
			if (device != IntPtr.Zero)
			{
				alcCloseDevice(device);
				device = IntPtr.Zero;
			}
		}
	}
}

using OpenAL;
using System;
using static OpenAL.AL10;
using static OpenAL.ALC10;

namespace SmartAudioPlayerFx.Sound
{
	sealed class ALSoundOut : IDisposable
	{
		IntPtr _device, _context;

		public ALSoundOut()
		{
			_device = alcOpenDevice(null);
			_context = alcCreateContext(_device, null);
			alcMakeContextCurrent(_context);
		}

		public void Dispose()
		{
			alcMakeContextCurrent(IntPtr.Zero);
			alcDestroyContext(_context);
			alcCloseDevice(_device);
		}

	}
}

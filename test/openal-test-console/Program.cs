using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static OpenAL.AL10;
using static OpenAL.ALC10;

class Program
{

    static void Main(string[] args)
    {
		// create
		var device = alcOpenDevice("OpenAL Soft");
		var context = alcCreateContext(device, null);
		alcMakeContextCurrent(context);

		// drive
		alGenBuffers(1, out var buffer_id);
		alGenSources(1, out var source_id);

		var isfloat32 = alIsExtensionPresent("AL_EXT_FLOAT32");
		var mcext = alIsExtensionPresent("AL_EXT_MCFORMATS");
		var fmt = alGetEnumValue("AL_FORMAT_51CHN32");

		//var data = File.ReadAllBytes("test-51float.wav");
		var data = File.ReadAllBytes("test-02u16.wav");
		//alBufferData(buffer_id, fmt, data, 24*100000, 44000);
		alBufferData(buffer_id, AL_FORMAT_STEREO16, data, data.Length, 44000);
		if (alGetError() != AL_NO_ERROR)
		{
			Console.WriteLine("Error Buffer :(");
		}

		alGetSourcei(source_id, AL_BUFFERS_PROCESSED, out var val);

		var buffer = default(uint);
		alSourceUnqueueBuffers(source_id, 1, ref buffer);

		alSourceQueueBuffers(source_id, 1, ref buffer_id);

		alGetSourcei(source_id, AL_BUFFERS_PROCESSED, out val);

		buffer = default(uint);
		alSourceUnqueueBuffers(source_id, 1, ref buffer);

		alSourcePlay(source_id);
		if (alGetError() != AL_NO_ERROR)
		{
			Console.WriteLine("Error starting.");
		}
		else
		{
			Console.WriteLine("Playing..");
		}
		Console.ReadLine();

		// destroy
		alcMakeContextCurrent(IntPtr.Zero);
		alcDestroyContext(context);
		alcCloseDevice(device);

	}
}
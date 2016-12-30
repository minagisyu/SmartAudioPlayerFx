using FFmpeg.AutoGen;
using System;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public sealed class AudioFrame
		{
			public int sample_rate;
			public int channel;
			public AVSampleFormat format;
			public IntPtr data;
			public int data_size;
		}
	}
}

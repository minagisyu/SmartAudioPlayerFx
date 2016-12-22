using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

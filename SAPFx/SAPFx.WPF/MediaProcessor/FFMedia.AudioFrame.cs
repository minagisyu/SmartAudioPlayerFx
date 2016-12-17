using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAPFx.WPF.Sound;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public sealed class AudioFrame
		{
			int sample_rate;
			int channel;
			AVSampleFormat format;
			IntPtr data;
		}
	}
}

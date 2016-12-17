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
		public sealed class VideoFrame
		{
			int width;
			int height;
			AVPixelFormat format;
			IntPtr data;
			int stride;
		}
	}
}

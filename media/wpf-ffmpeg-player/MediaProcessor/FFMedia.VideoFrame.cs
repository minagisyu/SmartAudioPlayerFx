using FFmpeg.AutoGen;
using System;

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

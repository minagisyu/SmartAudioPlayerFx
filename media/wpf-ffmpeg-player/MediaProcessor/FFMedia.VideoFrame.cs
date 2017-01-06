using FFmpeg.AutoGen;
using System;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public sealed class VideoFrame
		{
			public int width;
			public int height;
			public AVPixelFormat format;
			public IntPtr data;
			public int data_size;
			public int stride;
		}
	}
}

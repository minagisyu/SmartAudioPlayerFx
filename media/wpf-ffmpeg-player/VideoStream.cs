using System;
using FFmpeg.AutoGen;

namespace wpf_ffmpeg_player
{
	partial class FFMedia
	{
		public class VideoStream : IDisposable
		{
			FFMedia media;

			public VideoStream(FFMedia media)
			{
				this.media = media;
			}
			~VideoStream()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
			}
			void Dispose(bool disposing)
			{
			}
		}
	}
}
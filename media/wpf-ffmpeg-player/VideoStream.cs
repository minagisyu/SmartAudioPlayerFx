using System;
using FFmpeg.AutoGen;

namespace wpf_ffmpeg_player
{
	internal class VideoStream
	{
		internal unsafe AVPacket* flush_pkt;

		public AudioStream Audio { get; internal set; }

		internal void InitAV()
		{
			throw new NotImplementedException();
		}

		internal unsafe bool Start(AVFormatContext* pFormatCtx, int videoStream)
		{
			throw new NotImplementedException();
		}

		internal void End()
		{
			throw new NotImplementedException();
		}
	}
}
using System;
using FFmpeg.AutoGen;

namespace wpf_ffmpeg_player
{
	internal class AudioStream
	{
		internal unsafe AVPacket* flush_pkt;

		internal void InitAV()
		{
			throw new NotImplementedException();
		}

		internal unsafe bool Start(AVFormatContext* pFormatCtx, int audioStreamId)
		{
			throw new NotImplementedException();
		}
	}
}
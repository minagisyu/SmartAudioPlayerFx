using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;

namespace SmartAudioPlayer.MediaProcessor
{
	unsafe partial class FFMedia
	{
		readonly int? video_SID, audio_SID;


		void GetDefaultSID(out int? video_SID, out int? audio_SID)
		{
			video_SID = null;
			audio_SID = null;
			// Find the first video and audio streams
			for (int i = 0; i < pFormatCtx->nb_streams; i++)
			{
				if (pFormatCtx->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO && video_SID == null)
					video_SID = i;
				else if (pFormatCtx->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO && audio_SID == null)
					audio_SID = i;
			}
		}

	}
}

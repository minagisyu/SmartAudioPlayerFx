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
		public unsafe sealed class VideoDecoder : DecoderBase
		{
			readonly AVStream* stream;
			long current_pts_time;
			double frame_timer;
			double frame_last_delay = 40e-3;
			SwsContext* pSwscaleCtx = null;
			int width, height;

			public VideoDecoder(FFMedia media, int sid) : base(media, sid)
			{
				current_pts_time = av_gettime_impl();
				frame_timer = (double)current_pts_time / 1000000.0;

				width = pCodecCtx->width;
				height = pCodecCtx->height;
				if (pCodecCtx->sample_aspect_ratio.num != 0 &&
					pCodecCtx->sample_aspect_ratio.den != 0)
				{
					var aspect_ratio = av_q2d_impl(pCodecCtx->sample_aspect_ratio);
					if (aspect_ratio >= 1.0)
						width = (int)(width * aspect_ratio + 0.5);
					else if (aspect_ratio > 0.0)
						height = (int)(height / aspect_ratio + 0.5);
				}
			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);

				if (disposing)
				{
					// マネージリソースの破棄
				}

				// アンマネージリソースの破棄
			}

			void DecodeProcess()
			{
			}

		}
	}
}

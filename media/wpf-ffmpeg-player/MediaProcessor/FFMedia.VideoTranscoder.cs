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
		public unsafe sealed class VideoTranscoder : Transcoder
		{
			long current_pts_time;
			double frame_timer;
			double frame_last_delay = 40e-3;

			int width, height;
			AVFrame* pFrame = null;		// YUV-frame
			AVFrame* pFrameRGB = null;	// RGB-frame
			sbyte* pBuffer = null;		// RGB-frame buffer
			AVPixelFormat dstFmt = AVPixelFormat.AV_PIX_FMT_RGB24;
			SwsContext* pSwscaleCtx = null;

			public VideoTranscoder(FFMedia media) : base(media, AVMediaType.AVMEDIA_TYPE_VIDEO)
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

				// video init
				pFrame = av_frame_alloc();
				pFrameRGB = av_frame_alloc();
				var numBytes = avpicture_get_size(dstFmt, width, height);
				pBuffer = (sbyte*)av_malloc((ulong)numBytes * sizeof(byte));
				avpicture_fill((AVPicture*)pFrameRGB, pBuffer, dstFmt, width, height);

				pSwscaleCtx = sws_getContext(
					pCodecCtx->width, pCodecCtx->height, pCodecCtx->pix_fmt,
					width, height, dstFmt,
					SWS_BILINEAR, null, null, null);

			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);

				if (disposing)
				{
					// マネージリソースの破棄
				}

				// アンマネージリソースの破棄
				av_free(pBuffer);
				fixed (AVFrame** @ref = &pFrameRGB)
				{
					av_frame_free(@ref);
				}
				fixed (AVFrame** @ref = &pFrame)
				{
					av_frame_free(@ref);
				}
			}

			void DecodeProcess()
			{
			}

		}
	}
}

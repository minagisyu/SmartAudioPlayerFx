using FFmpeg.AutoGen;
using System;
using static FFmpeg.AutoGen.ffmpeg;

namespace SmartAudioPlayer.MediaProcessor
{
	public unsafe sealed class VideoDecoder : DecoderBase
	{
		bool disposed = false;
		int width, height;
		AVFrame* pFrame = null;     // YUV-frame
		AVFrame* pFrameRGB = null;  // RGB-frame
		sbyte* pBuffer = null;      // RGB-frame buffer
		AVPixelFormat dstFmt = AVPixelFormat.AV_PIX_FMT_RGB24;
		SwsContext* pSwscaleCtx = null;
		int buffer_size = 0;

		public VideoDecoder(AVFormatContext* pFormatCtx, int sid) : base(pFormatCtx, sid, AVMediaType.AVMEDIA_TYPE_VIDEO)
		{
			width = pCodecCtx->width;
			height = pCodecCtx->height;
			if (pCodecCtx->sample_aspect_ratio.num != 0 &&
				pCodecCtx->sample_aspect_ratio.den != 0)
			{
				var aspect_ratio = FFMedia.av_q2d_impl(pCodecCtx->sample_aspect_ratio);
				if (aspect_ratio >= 1.0)
					width = (int)(width * aspect_ratio + 0.5);
				else if (aspect_ratio > 0.0)
					height = (int)(height / aspect_ratio + 0.5);
			}

			// video init
			pFrame = av_frame_alloc();
			pFrameRGB = av_frame_alloc();
			buffer_size = avpicture_get_size(dstFmt, width, height);
			pBuffer = (sbyte*)av_malloc((ulong)buffer_size * sizeof(byte));
			avpicture_fill((AVPicture*)pFrameRGB, pBuffer, dstFmt, width, height);

			pSwscaleCtx = sws_getContext(
				pCodecCtx->width, pCodecCtx->height, pCodecCtx->pix_fmt,
				width, height, dstFmt,
				SWS_BILINEAR, null, null, null);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposed) return;
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

			disposed = true;
		}

		public static VideoDecoder Create(AVFormatContext* pFormatCtx, int sid)
		{
			try { return new VideoDecoder(pFormatCtx, sid); }
			catch { }
			return null;
		}

		public bool TakeFrame(PacketReader reader, ref VideoFrame frame)
		{
			while (true)
			{
				// TakeVideoFrame() == falseはストリーム終了
				if (reader.TakeVideoFrame(out var reading_packet) == false)
					return false;

				// reading_packet == nullはデコードできないがストリーム終了ではない
				if (reading_packet.HasValue == false)
				{
					frame.width = 0;
					frame.height = 0;
					frame.format = AVPixelFormat.AV_PIX_FMT_NONE;
					frame.data = IntPtr.Zero;
					frame.stride = 0;
					return true;
				}

				AVPacket packet = reading_packet.Value;
				int frameFinished;
				avcodec_decode_video2(pCodecCtx, pFrame, &frameFinished, &packet);

				if ((ulong)packet.dts != AV_NOPTS_VALUE)
				{
					frame.pts = av_frame_get_best_effort_timestamp(pFrame);
				}
				else
				{
					frame.pts = 0;
				}
				frame.pts *= FFMedia.av_q2d_impl(stream->time_base);
				av_free_packet(&packet);

				if (frameFinished != 0)
				{
					sws_scale(pSwscaleCtx,
						&pFrame->data0, pFrame->linesize,
						0, pCodecCtx->height,
						&pFrameRGB->data0, pFrameRGB->linesize);

					frame.width = width;
					frame.height = height;
					frame.format = dstFmt;
					frame.data = (IntPtr)pFrameRGB->data0;
					frame.data_size = pFrameRGB->linesize[0] * pCodecCtx->height;
					frame.stride = pFrameRGB->linesize[0];
					return true;
				}
			}
		}

	}
}

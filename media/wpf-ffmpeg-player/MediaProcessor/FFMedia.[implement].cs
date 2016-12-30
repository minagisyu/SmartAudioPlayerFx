using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;

namespace SmartAudioPlayer.MediaProcessor
{
	unsafe partial class FFMedia
	{
		static long av_gettime_impl()
			=> DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond / 1000);

		static readonly AVRational AV_TIME_BASE_Q_impl = new AVRational() { num = 1, den = AV_TIME_BASE };

		static void CreateAVPacket(out AVPacket packet)
		{
			fixed (AVPacket* @ref = &packet)
			{
				av_init_packet(@ref);
			}
		}
		static void FreeAVPacket(ref AVPacket packet)
		{
			fixed (AVPacket* @ref = &packet)
			{
				av_free_packet(@ref);
			}
		}

		static bool ReadFrame(FFMedia media, ref AVPacket packet)
		{
			fixed (AVPacket* @ref = &packet)
			{
				return av_read_frame(media.pFormatCtx, @ref) >= 0;
			}
		}

		static double av_q2d_impl(AVRational a)
			=> a.num / (double)a.den;

		static void memset(byte* tgt, byte val, int bytesize)
		{
			byte* sentinel = tgt + bytesize;
			while (tgt < sentinel)
			{
				tgt[0] = val;
				tgt++;
			}
		}

		static void memcpy(byte* src, byte* dst, int bytesize)
		{
			byte* sentinel = src + bytesize;
			while (src < sentinel)
			{
				dst[0] = src[0];
				src++;
				dst++;
			}
		}

	}
}

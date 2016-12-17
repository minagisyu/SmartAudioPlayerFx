using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reactive.Disposables;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		static long av_gettime_impl()
			=> DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond / 1000);

		static AVRational AV_TIME_BASE_Q_impl { get; } = new AVRational() { num = 1, den = AV_TIME_BASE };

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
		{
			return a.num / (double)a.den;
		}

	}
}

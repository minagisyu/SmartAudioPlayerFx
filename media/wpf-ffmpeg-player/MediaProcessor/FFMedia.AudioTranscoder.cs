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
		public unsafe sealed class AudioTranscoder : Transcoder
		{
			const double AUDIO_DIFF_AVG_NB = 20;
			readonly double diff_avg_coef = Math.Exp(Math.Log(0.01) / AUDIO_DIFF_AVG_NB);
			readonly double diff_threshold = 2.0 * 0.050; // 50ms
			readonly int audioFormat;

			SwrContext* pSwrContext = null;
			AVSampleFormat dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;


			public AudioTranscoder(FFMedia media) : base(media, AVMediaType.AVMEDIA_TYPE_AUDIO)
			{
				// Set up SWR context once you've got codec information
				int dstRate = pCodecCtx->sample_rate;
				int dstNbChannels = 0;
				ulong dstChLayout = AV_CH_LAYOUT_STEREO;

				// buffer is going to be directly written to a rawaudio file, no alignment
				dstNbChannels = av_get_channel_layout_nb_channels(dstChLayout);

				pSwrContext = swr_alloc();
				if (pSwrContext == null)
					throw new FFMediaException("failed alloc SwrContext.");

				// チャンネルレイアウトがわからない場合は、チャンネル数から取得する。
				if (pCodecCtx->channel_layout == 0)
					pCodecCtx->channel_layout = (ulong)av_get_default_channel_layout(pCodecCtx->channels);
				//
				av_opt_set_int(pSwrContext, "in_channel_layout", (long)pCodecCtx->channel_layout, 0);
				av_opt_set_int(pSwrContext, "out_channel_layout", (long)dstChLayout, 0);
				av_opt_set_int(pSwrContext, "in_sample_rate", pCodecCtx->sample_rate, 0);
				av_opt_set_int(pSwrContext, "out_sample_rate", dstRate, 0);
				av_opt_set_sample_fmt(pSwrContext, "in_sample_fmt", pCodecCtx->sample_fmt, 0);
				av_opt_set_sample_fmt(pSwrContext, "out_sample_fmt", dstSampleFmt, 0);
				/*	// to 5.1ch
					dstChLayout = AV_CH_LAYOUT_5POINT1;
					dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_FLT;
					dstNbChannels = av_get_channel_layout_nb_channels(dstChLayout);
					av_opt_set_int(swr, "out_channel_layout", (long)dstChLayout, 0);
					av_opt_set_int(swr, "out_sample_rate", dstRate, 0);
					av_opt_set_sample_fmt(swr, "out_sample_fmt", dstSampleFmt, 0);
				*/
				if (swr_init(pSwrContext) < 0)
					throw new FFMediaException("failed SWR initialize.");
			}

			internal void TakeFrame()
			{
				throw new NotImplementedException();
			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);

				if (disposing)
				{
					// マネージリソースの破棄
				}

				// アンマネージリソースの破棄
				fixed (SwrContext** @ref = &pSwrContext)
				{
					swr_free(@ref);
				}
			}


		}
	}
}

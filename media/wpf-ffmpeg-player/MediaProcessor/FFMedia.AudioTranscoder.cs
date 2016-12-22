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
			int dstRate = 0;
			int dstNbChannels = 0;
			int dstChLayout = AV_CH_LAYOUT_STEREO;


			public AudioTranscoder(FFMedia media) : base(media, AVMediaType.AVMEDIA_TYPE_AUDIO)
			{
				// Set up SWR context once you've got codec information
				dstRate = pCodecCtx->sample_rate;

				// buffer is going to be directly written to a rawaudio file, no alignment
				dstNbChannels = av_get_channel_layout_nb_channels((ulong)dstChLayout);

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
				/*	// to 5.1ch?
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


			public bool TakeFrame(ref AudioFrame frame)
			{
				if (media.reader.TakeFrame(sid, out var reading_packet) == false) return false;

				byte* audioBuf = stackalloc byte[192000];
				var decoded_size = decode(&audioBuf[0], 192000, reading_packet);
				return true;
			}

			int decode(byte* buf, int bufSize, AVPacket* packet)
			{
				uint bufIndex = 0;
				uint dataSize = 0;

				AVFrame* frame = av_frame_alloc();
				if (frame == null)
				{
					// Error allocating the frame
					av_free_packet(packet);
					return 0;
				}

				while (packet->size > 0)
				{
					// デコードする。
					int gotFrame = 0;
					int result = avcodec_decode_audio4(pCodecCtx, frame, &gotFrame, packet);

					if (result >= 0 && gotFrame != 0)
					{
						packet->size -= result;
						packet->data += result;

						// 変換後のデータサイズを計算する。
						long dstNbSamples = av_rescale_rnd(frame->nb_samples, dstRate, pCodecCtx->sample_rate, AVRounding.AV_ROUND_UP);
						sbyte** dstData = null;
						int dstLineSize;
						if (av_samples_alloc_array_and_samples(&dstData, &dstLineSize, dstNbChannels, (int)dstNbSamples, dstSampleFmt, 0) < 0)
						{
							// Could not allocate destination samples
							dataSize = 0;
							break;
						}

						// デコードしたデータをSwrContextに指定したフォーマットに変換する。
						int ret = swr_convert(pSwrContext, dstData, (int)dstNbSamples, (sbyte**)frame->extended_data, frame->nb_samples);
						if (ret < 0)
						{
							// Error while converting
							dataSize = 0;
							break;
						}

						// 変換したデータのサイズを計算する。
						int dstBufSize = av_samples_get_buffer_size(&dstLineSize, dstNbChannels, ret, dstSampleFmt, 1);
						if (dstBufSize < 0)
						{
							// Error
							dataSize = 0;
							break;
						}

						if (dataSize + dstBufSize > bufSize)
						{
							// dataSize + dstBufSize > bufSize
							dataSize = 0;
							break;
						}

						// 変換したデータをバッファに書き込む
						//	memcpy(buf + bufIndex, (byte*)dstData[0], dstBufSize);
						memcpy((byte*)dstData[0], buf + bufIndex, dstBufSize);

						bufIndex += (uint)dstBufSize;
						dataSize += (uint)dstBufSize;

						if (dstData != null)
						{
							av_freep(&dstData[0]);
						}
						av_freep(&dstData);
					}
					else
					{
						packet->size = 0;
						packet->data = null;
					}
				}

				av_free_packet(packet);
				av_free(frame);

				return (int)dataSize;
			}


		}
	}
}

using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Runtime.InteropServices;
using static OpenAL.AL10;
using static OpenAL.ALC10;
using SmartAudioPlayer.MediaProcessor.Audio;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe sealed class AudioTranscoder : Transcoder
		{
			const int AUDIO_BUFFER_TIME = 100; // in milliseconds, pre-buffer
			const double AUDIO_DIFF_AVG_NB = 20;
			readonly double diff_avg_coef = Math.Exp(Math.Log(0.01) / AUDIO_DIFF_AVG_NB);
			readonly double diff_threshold = 2.0 * 0.050; // 50ms
			readonly int audioFormat;

			SwrContext* pSwrContext = null;
			AVSampleFormat dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
			int frame_size = 0;
			int dstRate = 0;
			int dstNbChannels = 0;
			int dstChLayout = AV_CH_LAYOUT_STEREO;
			IntPtr audioBuf;
			ALStreamingSource alStream;
			int buffer_len;
			public int dstALFormat = AL_NONE;


			public AudioTranscoder(FFMedia media) : base(media, AVMediaType.AVMEDIA_TYPE_AUDIO)
			{
				audioBuf = Marshal.AllocCoTaskMem(512000);
				alStream = new ALStreamingSource(8);

				// Set up SWR context once you've got codec information
				dstRate = pCodecCtx->sample_rate;

				// buffer is going to be directly written to a rawaudio file, no alignment
				dstNbChannels = av_get_channel_layout_nb_channels((ulong)dstChLayout);

				pSwrContext = swr_alloc();
				if (pSwrContext == null)
					throw new FFMediaException("failed alloc SwrContext.");

				dstALFormat = SetBestALFormat();

				if (swr_init(pSwrContext) < 0)
					throw new FFMediaException("failed SWR initialize.");
			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);

				if (disposing)
				{
					// マネージリソースの破棄
					alStream.Dispose();
				}

				// アンマネージリソースの破棄
				if (audioBuf != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(audioBuf);
					audioBuf = IntPtr.Zero;
				}
				fixed (SwrContext** @ref = &pSwrContext)
				{
					swr_free(@ref);
				}
			}

			public bool TakeFrame(ref AudioFrame frame)
			{
				// TakeAudioFrame() == falseはストリーム終了
				if (media.reader.TakeAudioFrame(out var reading_packet) == false)
					return false;

				// reading_packet == nullはデコードできないがストリーム終了ではない
				if (reading_packet.HasValue == false)
				{
					frame.sample_rate = 0;
					frame.channel = 0;
					frame.format = AVSampleFormat.AV_SAMPLE_FMT_NONE;
					frame.data = IntPtr.Zero;
					frame.data_size = 0;
					return true;
				}

				AVPacket packet = reading_packet.Value;
				var decoded_size = decode((byte*)audioBuf, 512000, &packet);

				frame.sample_rate = dstRate;
				frame.channel = dstNbChannels;
				frame.format = dstSampleFmt;
				frame.data = audioBuf;
				frame.data_size = decoded_size;
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


			int SetBestALFormat()
			{
				int format = AL_NONE;

				if (pCodecCtx->channel_layout == 0)
					pCodecCtx->channel_layout = (ulong)av_get_default_channel_layout(pCodecCtx->channels);

				if (pCodecCtx->sample_fmt == AVSampleFormat.AV_SAMPLE_FMT_U8 ||
					pCodecCtx->sample_fmt == AVSampleFormat.AV_SAMPLE_FMT_U8P)
				{
					dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_U8;
					frame_size = 1;
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_7POINT1 &&
						alStream.SourceFormat.IsAllow_MCFORMATS &&
						alStream.SourceFormat.MULTI_71CH_N8 > 0)
					{
						dstChLayout = AV_CH_LAYOUT_7POINT1;
						dstNbChannels = 8;
						frame_size *= 8;
						format = alStream.SourceFormat.MULTI_71CH_N8;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_5POINT1 &&
						alStream.SourceFormat.IsAllow_MCFORMATS &&
						alStream.SourceFormat.MULTI_51CH_N8 > 0)
					{
						dstChLayout = AV_CH_LAYOUT_5POINT1;
						dstNbChannels = 6;
						frame_size *= 6;
						format = alStream.SourceFormat.MULTI_51CH_N8;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_MONO)
					{
						dstChLayout = AV_CH_LAYOUT_MONO;
						dstNbChannels = 1;
						frame_size *= 1;
						format = alStream.SourceFormat.MONO_8;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_STEREO)
					{
						dstChLayout = AV_CH_LAYOUT_STEREO;
						dstNbChannels = 2;
						frame_size *= 2;
						format = alStream.SourceFormat.STEREO_8;
					}
				}
				if ((pCodecCtx->sample_fmt == AVSampleFormat.AV_SAMPLE_FMT_FLT ||
					pCodecCtx->sample_fmt == AVSampleFormat.AV_SAMPLE_FMT_FLTP) &&
					alStream.SourceFormat.IsAllow_FLOAT32)
				{
					dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_FLT;
					frame_size = 4;
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_7POINT1 &&
						alStream.SourceFormat.IsAllow_MCFORMATS &&
						alStream.SourceFormat.MULTI_71CH_N32 > 0)
					{
						dstChLayout = AV_CH_LAYOUT_7POINT1;
						dstNbChannels = 8;
						frame_size *= 8;
						format = alStream.SourceFormat.MULTI_71CH_N32;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_5POINT1 &&
						alStream.SourceFormat.IsAllow_MCFORMATS &&
						alStream.SourceFormat.MULTI_51CH_N32 > 0)
					{
						dstChLayout = AV_CH_LAYOUT_5POINT1;
						dstNbChannels = 6;
						frame_size *= 6;
						format = alStream.SourceFormat.MULTI_51CH_N32;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_MONO)
					{
						dstChLayout = AV_CH_LAYOUT_MONO;
						dstNbChannels = 1;
						frame_size *= 1;
						format = alStream.SourceFormat.MONO_32;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_STEREO)
					{
						dstChLayout = AV_CH_LAYOUT_STEREO;
						dstNbChannels = 2;
						frame_size *= 2;
						format = alStream.SourceFormat.STEREO_32;
					}
				}
				if (format == AL_NONE)
				{
					dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
					frame_size = 2;
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_7POINT1 &&
						alStream.SourceFormat.IsAllow_MCFORMATS &&
						alStream.SourceFormat.MULTI_71CH_N16 > 0)
					{
						dstChLayout = AV_CH_LAYOUT_7POINT1;
						dstNbChannels = 8;
						frame_size *= 8;
						format = alStream.SourceFormat.MULTI_71CH_N16;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_5POINT1 &&
						alStream.SourceFormat.IsAllow_MCFORMATS &&
						alStream.SourceFormat.MULTI_51CH_N16 > 0)
					{
						dstChLayout = AV_CH_LAYOUT_5POINT1;
						dstNbChannels = 6;
						frame_size *= 6;
						format = alStream.SourceFormat.MULTI_51CH_N16;
					}
					if (pCodecCtx->channel_layout == AV_CH_LAYOUT_MONO)
					{
						dstChLayout = AV_CH_LAYOUT_MONO;
						dstNbChannels = 1;
						frame_size *= 1;
						format = alStream.SourceFormat.MONO_16;
					}
					if (format == AL_NONE)
					{
						dstChLayout = AV_CH_LAYOUT_STEREO;
						dstNbChannels = 2;
						frame_size *= 2;
						format = alStream.SourceFormat.STEREO_16;
					}
				}

				buffer_len = AUDIO_BUFFER_TIME * pCodecCtx->sample_rate / 1000 * frame_size;

				av_opt_set_int(pSwrContext, "in_channel_layout", (long)pCodecCtx->channel_layout, 0);
				av_opt_set_int(pSwrContext, "out_channel_layout", dstChLayout, 0);
				av_opt_set_int(pSwrContext, "in_sample_rate", pCodecCtx->sample_rate, 0);
				av_opt_set_int(pSwrContext, "out_sample_rate", dstRate, 0);
				av_opt_set_sample_fmt(pSwrContext, "in_sample_fmt", pCodecCtx->sample_fmt, 0);
				av_opt_set_sample_fmt(pSwrContext, "out_sample_fmt", dstSampleFmt, 0);

				return format;
			}

		}
	}
}

using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Runtime.InteropServices;
using static OpenAL.AL10;
using static OpenAL.ALC10;
using SmartAudioPlayer.MediaProcessor.Audio;

namespace SmartAudioPlayer.MediaProcessor
{
	public unsafe sealed class AudioDecoder : DecoderBase
	{
		readonly int audioFormat;

		SwrContext* pSwrContext = null;
		AVSampleFormat dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
		int frame_size = 0;
		int dstRate = 0;
		int dstNbChannels = 0;
		int dstChLayout = AV_CH_LAYOUT_STEREO;
		IntPtr audioBuf;
		int buffer_len;
		public int dstALFormat = AL_NONE;

		public AudioDecoder(AVFormatContext* pFormatCtx, int sid) : base(pFormatCtx, sid, AVMediaType.AVMEDIA_TYPE_AUDIO)
			{
			audioBuf = Marshal.AllocCoTaskMem(5120_000);

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

		public static AudioDecoder Create(AVFormatContext* pFormatCtx, int sid)
		{
			try { return new AudioDecoder(pFormatCtx, sid); }
			catch { }
			return null;
		}

		public bool TakeFrame(PacketReader reader, ref AudioFrame frame)
		{
			// TakeAudioFrame() == falseはストリーム終了
			if (reader.TakeAudioFrame(out var reading_packet) == false)
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
			var decoded_size = decode((byte*)audioBuf, 5120_000, &packet);

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
					FFMedia.memcpy((byte*)dstData[0], buf + bufIndex, dstBufSize);

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

			dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
			frame_size = 2;
			if (pCodecCtx->channel_layout == AV_CH_LAYOUT_MONO)
			{
				dstChLayout = AV_CH_LAYOUT_MONO;
				dstNbChannels = 1;
				frame_size *= 1;
				format = AL_FORMAT_MONO16;
			}
			if (format == AL_NONE)
			{
				dstChLayout = AV_CH_LAYOUT_STEREO;
				dstNbChannels = 2;
				frame_size *= 2;
				format = AL_FORMAT_STEREO16;
			}

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

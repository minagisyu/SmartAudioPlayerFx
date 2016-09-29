using FFmpeg.AutoGen;
using OpenTK;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace openal_ffmpeg
{
	class Program
	{
		static unsafe int decode(
			byte* buf, int bufSize,
			AVPacket* packet, AVCodecContext* codecContext, SwrContext* swr,
			int dstRate, int dstNbChannels,
			AVSampleFormat* dstSampleFmt)
		{

			uint bufIndex = 0;
			uint dataSize = 0;

			AVFrame* frame = ffmpeg.av_frame_alloc();
			if (frame == null)
			{
				Console.WriteLine("Error allocating the frame");

				ffmpeg.av_free_packet(packet);
				return 0;
			}


			while (packet->size > 0)
			{
				// デコードする。
				int gotFrame = 0;
				int result = ffmpeg.avcodec_decode_audio4(codecContext, frame, &gotFrame, packet);

				if (result >= 0 && gotFrame != 0)
				{

					packet->size -= result;
					packet->data += result;

					// 変換後のデータサイズを計算する。
					long dstNbSamples = ffmpeg.av_rescale_rnd(frame->nb_samples, dstRate, codecContext->sample_rate, AVRounding.AV_ROUND_UP);
					sbyte** dstData = null;
					int dstLineSize;
					if (ffmpeg.av_samples_alloc_array_and_samples(&dstData, &dstLineSize, dstNbChannels, (int)dstNbSamples, *dstSampleFmt, 0) < 0)
					{
						Console.WriteLine("Could not allocate destination samples");
						dataSize = 0;
						break;
					}

					// デコードしたデータをSwrContextに指定したフォーマットに変換する。
					int ret = ffmpeg.swr_convert(swr, dstData, (int)dstNbSamples, (sbyte**)frame->extended_data, frame->nb_samples);
					//int ret = swr_convert(swr, dstData, *dstNbSamples, (const uint8_t **)frame->data, frame->nb_samples);
					if (ret < 0)
					{
						Console.WriteLine("Error while converting");
						dataSize = 0;
						break;
					}

					// 変換したデータのサイズを計算する。
					int dstBufSize = ffmpeg.av_samples_get_buffer_size(&dstLineSize, dstNbChannels, ret, *dstSampleFmt, 1);
					if (dstBufSize < 0)
					{
						Console.WriteLine("Error av_samples_get_buffer_size()");
						dataSize = 0;
						break;
					}

					if (dataSize + dstBufSize > bufSize)
					{
						Console.WriteLine("dataSize + dstBufSize > bufSize");
						dataSize = 0;
						break;
					}

					// 変換したデータをバッファに書き込む
				//	memcpy(buf + bufIndex, (byte*)dstData[0], dstBufSize);
					memcpy((byte*)dstData[0], buf + bufIndex, dstBufSize);

					bufIndex += (uint)dstBufSize;
					dataSize += (uint)dstBufSize;

					if (dstData != null)
						ffmpeg.av_freep(&dstData[0]);

					ffmpeg.av_freep(&dstData);

				}
				else
				{

					packet->size = 0;
					packet->data = null;

				}
			}

			ffmpeg.av_free_packet(packet);
			ffmpeg.av_free(frame);

			return (int)dataSize;
		}

		static unsafe void memcpy(byte* src, byte* dst, int bytesize)
		{
			byte* sentinel = src + bytesize;
			while (src < sentinel)
			{
				dst[0] = src[0];
				src++;
				dst++;
			}
		}

		static unsafe void Main(string[] args)
		{
			// FFmpegのログレベルをQUIETに設定する。（何も出力しない）
			ffmpeg.av_log_set_level(0);// AV_LOG_QUIET);

			// FFmpegを初期化
			ffmpeg.av_register_all();

			AVFormatContext* formatContext = null;
		//	string file = @"E:\Music\[USB]\bms\2MB medley(plus original).mp3"; // args[1]
			string file = @"E:\Music\[_old_]\[お気に入り]\アルトネリコOVAのアレ.ac3"; // args[1]
			if (ffmpeg.avformat_open_input(&formatContext, file, null, null) != 0)
			{
				Console.WriteLine("Error opening the file");
				return;
			}

			if (ffmpeg.avformat_find_stream_info(formatContext, null) < 0)
			{
				ffmpeg.avformat_close_input(&formatContext);
				Console.WriteLine("Error finding the stream info");
				return;
			}

			//
			AVCodec* cdc = null;
			int streamIndex = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &cdc, 0);
			if (streamIndex < 0)
			{
				ffmpeg.avformat_close_input(&formatContext);
				Console.WriteLine("Could not find any audio stream in the file");
				return;
			}

			AVStream* audioStream = formatContext->streams[streamIndex];
			AVCodecContext* codecContext = audioStream->codec;
			codecContext = audioStream->codec;
			codecContext->codec = cdc;

			if (ffmpeg.avcodec_open2(codecContext, codecContext->codec, null) != 0)
			{
				ffmpeg.avformat_close_input(&formatContext);
				Console.WriteLine("Couldn't open the context with the decoder");
				return;
			}

			Console.WriteLine($"This stream has {codecContext->channels} channels and a sample rate of {codecContext->sample_rate} Hz");
			Console.WriteLine($"The data is in the format {/*ffmpeg.av_get_sample_fmt_name(*/codecContext->sample_fmt.ToString()/*)*/}");


			//-----------------------------------------------------------------//


			// Set up SWR context once you've got codec information
			int dstRate = codecContext->sample_rate;
			int dstNbChannels = 0;
			ulong dstChLayout = 0x00000001 | 0x00000002;// AV_CH_LAYOUT_STEREO;
			AVSampleFormat dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;


			// buffer is going to be directly written to a rawaudio file, no alignment
			dstNbChannels = ffmpeg.av_get_channel_layout_nb_channels(dstChLayout);

			SwrContext* swr = ffmpeg.swr_alloc();
			if (swr == null)
			{

				Console.WriteLine("Could not allocate resampler context");

				ffmpeg.avcodec_close(codecContext);

				ffmpeg.avformat_close_input(&formatContext);
				return;
			}

			// チャンネルレイアウトがわからない場合は、チャンネル数から取得する。
			if (codecContext->channel_layout == 0)
				codecContext->channel_layout = (ulong)ffmpeg.av_get_default_channel_layout(codecContext->channels);

			//
			ffmpeg.av_opt_set_int(swr, "in_channel_layout", (long)codecContext->channel_layout, 0);
			ffmpeg.av_opt_set_int(swr, "out_channel_layout", (long)dstChLayout, 0);
			ffmpeg.av_opt_set_int(swr, "in_sample_rate", codecContext->sample_rate, 0);
			ffmpeg.av_opt_set_int(swr, "out_sample_rate", dstRate, 0);
			ffmpeg.av_opt_set_sample_fmt(swr, "in_sample_fmt", codecContext->sample_fmt, 0);
			ffmpeg.av_opt_set_sample_fmt(swr, "out_sample_fmt", dstSampleFmt, 0);
			if (ffmpeg.swr_init(swr) < 0)
			{
				Console.WriteLine("Failed to initialize the resampling context");

				ffmpeg.avcodec_close(codecContext);

				ffmpeg.avformat_close_input(&formatContext);
				return;
			}


			//-----------------------------------------------------------------//

			IntPtr dev;
			ContextHandle ctx;

			dev = Alc.OpenDevice(null);
			if (dev == IntPtr.Zero)
			{

				Console.WriteLine("Oops");
				ffmpeg.swr_free(&swr);

				ffmpeg.avcodec_close(codecContext);

				ffmpeg.avformat_close_input(&formatContext);
				return;
			}

			ctx = Alc.CreateContext(dev, (int*)null);
			Alc.MakeContextCurrent(ctx);
			if (ctx.Handle == IntPtr.Zero)
			{

				Console.WriteLine("Oops2");

				ffmpeg.swr_free(&swr);

				ffmpeg.avcodec_close(codecContext);

				ffmpeg.avformat_close_input(&formatContext);
				return;
			}

			const int NUM_BUFFERS = 3;
			int source;
			int[] buffers;

			buffers = AL.GenBuffers(NUM_BUFFERS);
			AL.GenSources(1, out source);
			if (AL.GetError() != ALError.NoError)
			{

				Console.WriteLine("Error generating :(");

				ffmpeg.swr_free(&swr);

				ffmpeg.avcodec_close(codecContext);

				ffmpeg.avformat_close_input(&formatContext);
				return;
			}


			// PacketQueueを使い
			// 一旦音楽データをパケットに分解して保持する。
			PacketQueue pktQueue;
			initPacketQueue(&pktQueue);

			AVPacket readingPacket;
			ffmpeg.av_init_packet(&readingPacket);

			while (ffmpeg.av_read_frame(formatContext, &readingPacket) >= 0)
			{

				// Is this a packet from the video stream?
				if (readingPacket.stream_index == audioStream->index)
				{


					pushPacketToPacketQueue(&pktQueue, &readingPacket);

				}
				else
				{

					ffmpeg.av_free_packet(&readingPacket);
				}
			}


			byte* audioBuf = stackalloc byte[192000];//AVCODEC_MAX_AUDIO_FRAME_SIZE];
			int audioBufSize = 0;

			// NUM_BUFFERSの数だけ、あらかじめバッファを準備する。
			int i;
			for (i = 0; i < NUM_BUFFERS; i++)
			{

				// パケットキューにたまっているパケットを取得する。
				AVPacket decodingPacket;
				if (popPacketFromPacketQueue(&pktQueue, &decodingPacket) < 0)
				{
					Console.WriteLine("error.");
					break;
				}

				// 取得したパケットをデコードして、希望のフォーマットに変換する。
				audioBufSize = decode(&audioBuf[0], 192000, &decodingPacket,
						  codecContext, swr, dstRate, dstNbChannels, &dstSampleFmt);

					// デコード、変換したデータを、OpenALのバッファに書き込む。
					AL.BufferData(buffers[i], ALFormat.Stereo16, (IntPtr)audioBuf, audioBufSize, dstRate);
				if (AL.GetError() != ALError.NoError)
				{
					Console.WriteLine("Error Buffer :(");

					ffmpeg.av_free_packet(&decodingPacket);
					continue;
				}


				ffmpeg.av_free_packet(&decodingPacket);
			}


			// データが入ったバッファをキューに追加して、再生を開始する。
			AL.SourceQueueBuffers(source, NUM_BUFFERS, buffers);
			AL.SourcePlay(source);

			bool playing = false;
			if (AL.GetError() != ALError.NoError)
			{
				Console.WriteLine("Error starting.");
				return;
			}
			else
			{
				Console.WriteLine("Playing..");
				playing = true;
			}


			// パケットキューがなくなるまで、繰り返す。
			while (pktQueue.numOfPackets > 0 && playing)
			{

				// 使用済みバッファの数を取得する。
				// 使用済みバッファがない場合は、できるまで繰り返す。
				int val;

				AL.GetSource(source, ALGetSourcei.BuffersProcessed, out val);
				if (val <= 0)
				{
					// 少しスリープさせて処理を減らす。
					/*   struct timespec ts = {0, 1 * 1000000}; // 1msec

					   nanosleep(&ts, NULL);
					  */
					Thread.Sleep(1);
					continue;
				}

				AVPacket decodingPacket;
				if (popPacketFromPacketQueue(&pktQueue, &decodingPacket) < 0)
				{
					Console.WriteLine("error.");
					break;
				}

				audioBufSize = decode(&audioBuf[0], 192000, &decodingPacket,
						  codecContext, swr, dstRate, dstNbChannels, &dstSampleFmt);

				if (audioBufSize <= 0)
				{
					continue;
				}

				// 再生済みのバッファをデキューする。
				int[] buffer = new int[1];

				AL.SourceUnqueueBuffers(source, 1, buffer);

				// デキューしたバッファに、新しい音楽データを書き込む。
				AL.BufferData(buffer[0], ALFormat.Stereo16, (IntPtr)audioBuf, audioBufSize, dstRate);
				if (AL.GetError() != ALError.NoError)
				{

					Console.WriteLine("Error Buffer :(");
					return;
				}

				// 新しいデータを書き込んだバッファをキューする。
				AL.SourceQueueBuffers(source, 1, buffer);
				if (AL.GetError() != ALError.NoError)
				{

					Console.WriteLine("Error buffering :(");
					return;
				}

				// もし再生が止まっていたら、再生する。
				AL.GetSource(source, ALGetSourcei.SourceState, out val);
				if ((ALSourceState)val != ALSourceState.Playing)

					AL.SourcePlay(source);

				// 掃除
				ffmpeg.av_free_packet(&decodingPacket);
			}


			// 未処理のパケットが残っている場合は、パケットを解放する。
			while (pktQueue.numOfPackets > 0)
			{
				AVPacket decodingPacket;
				if (popPacketFromPacketQueue(&pktQueue, &decodingPacket) < 0)
					continue;

				ffmpeg.av_free_packet(&decodingPacket);
			}


			Console.WriteLine("End.");


			ffmpeg.swr_free(&swr);

			ffmpeg.avcodec_close(codecContext);
			ffmpeg.avformat_close_input(&formatContext);
			ffmpeg.avformat_free_context(formatContext);

			AL.DeleteSources(1, ref source);
			AL.DeleteBuffers(buffers);

			Alc.MakeContextCurrent(ContextHandle.Zero);
			Alc.DestroyContext(ctx);
			Alc.CloseDevice(dev);

		}


		static unsafe void initPacketQueue(PacketQueue* q)
		{
			memset((byte*)q, 0, sizeof(PacketQueue));
		}

		static unsafe int pushPacketToPacketQueue(PacketQueue* pPktQ, AVPacket* pPkt)
		{
			AVPacketList* pPktList;

			if (ffmpeg.av_dup_packet(pPkt) > 0)
			{
				return -1;
			}

			pPktList = (AVPacketList*)ffmpeg.av_malloc((ulong)sizeof(AVPacketList));
			if (pPktList == null)
			{
				return -1;
			}

			pPktList->pkt = *pPkt;
			pPktList->next = null;

			// 
			if (pPktQ->pLastPkt == null)
			{
				pPktQ->pFirstPkt = pPktList;
			}
			else
			{
				pPktQ->pLastPkt->next = pPktList;
			}



			pPktQ->pLastPkt = pPktList;

			// 
			pPktQ->numOfPackets++;
			return 0;
		}

		static unsafe int popPacketFromPacketQueue(PacketQueue* pPQ, AVPacket* pPkt)
		{
			AVPacketList* pPktList;

			//
			pPktList = pPQ->pFirstPkt;

			if (pPktList != null)
			{
				pPQ->pFirstPkt = pPktList->next;
				if (pPQ->pFirstPkt == null)
				{
					pPQ->pLastPkt = null;
				}
				pPQ->numOfPackets--;

				*pPkt = pPktList->pkt;

				ffmpeg.av_free(pPktList);

				return 0;
			}

			return -1;
		}


		static unsafe void memset(byte* tgt, byte val, int bytesize)
		{
			byte* sentinel = tgt + bytesize;
			while (tgt < sentinel)
			{
				tgt[0] = val;
				tgt++;
			}
		}

	}


	unsafe struct PacketQueue
	{
		public AVPacketList* pFirstPkt;
		public AVPacketList* pLastPkt;
		public int numOfPackets;

	}

}

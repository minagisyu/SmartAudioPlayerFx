using FFmpeg.AutoGen;
using OpenTK;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static FFmpeg.AutoGen.ffmpeg;

namespace wpf_ffmpeg_player
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			image.Source = null;
		}

		unsafe struct PacketQueue
		{
			public AVPacketList* pFirstPkt;
			public AVPacketList* pLastPkt;
			public int numOfPackets;

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

		unsafe AVFormatContext* pFormatCtx = null;
		int videoId, audioId;
		unsafe AVCodecContext* pVideoCodecCtx, pAudioCodecCtx = null;
		unsafe SwrContext* swr;
		unsafe SwsContext* sws_ctx;
		PacketQueue vPktQueue, aPktQueue;
		AVSampleFormat dstSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;

		private unsafe void button_Click(object sender, RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				// 破棄処理を楽にする...
				Action closingAction = null;
				try
				{
					av_register_all();

					//= Format Context

					// open media file
					string file = @"V:\__Video(old)\1995-2010\_etc\AT-X ロゴ (AT-X 640x480_x264 [2009Q3]).mp4";
					file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x720p Hi10P).mp4";
					//	file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x1080i Hi10P).mp4";
					fixed (AVFormatContext** @ref = &pFormatCtx)
					{
						if (avformat_open_input(@ref, file, null, null) != 0)
							return;
					}
					closingAction += () =>
					{
						fixed (AVFormatContext** @ref = &pFormatCtx)
						{
							avformat_close_input(@ref);
						}
					};

					// retrive stream infomation 
					if (avformat_find_stream_info(pFormatCtx, null) < 0)
						return;

					// Find the first video stream
					// Get a pointer to the codec context for the video stream
					AVCodec* vCodec, aCodec = null;
					AVStream* vStream, aStream = null;
					videoId = av_find_best_stream(pFormatCtx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &vCodec, 0);
					audioId = av_find_best_stream(pFormatCtx, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &aCodec, 0);
					if (videoId < 0)
						return;
					else
					{
						vStream = pFormatCtx->streams[videoId];
						pVideoCodecCtx = vStream->codec;
						pVideoCodecCtx->codec = vCodec;
						closingAction += () => avcodec_close(pVideoCodecCtx);
					}
					if (audioId < 0)
						return;
					else
					{
						aStream = pFormatCtx->streams[audioId];
						pAudioCodecCtx = aStream->codec;
						pAudioCodecCtx->codec = aCodec;
						closingAction += () => avcodec_close(pAudioCodecCtx);
					}

					// Open codec
					if (avcodec_open2(pVideoCodecCtx, pVideoCodecCtx->codec, null) < 0)
						return;
					if (avcodec_open2(pAudioCodecCtx, pAudioCodecCtx->codec, null) < 0)
						return;

					// PacketQueueを使い
					// 一旦データをパケットに分解して保持する。
					fixed (PacketQueue* @ref = &vPktQueue) { initPacketQueue(@ref); }
					fixed (PacketQueue* @ref = &aPktQueue) { initPacketQueue(@ref); }

					AVPacket readingPacket;
					av_init_packet(&readingPacket);
					while (av_read_frame(pFormatCtx, &readingPacket) >= 0)
					{
						if (readingPacket.stream_index == videoId)
						{
							fixed (PacketQueue* @ref = &vPktQueue)
							{
								pushPacketToPacketQueue(@ref, &readingPacket);
							}

						}
						else if (readingPacket.stream_index == audioId)
						{
							fixed (PacketQueue* @ref = &aPktQueue)
							{
								pushPacketToPacketQueue(@ref, &readingPacket);
							}
						}
						else
						{
							ffmpeg.av_free_packet(&readingPacket);
						}
					}

					// audio init
					{
						// Set up SWR context once you've got codec information
						int dstRate = pAudioCodecCtx->sample_rate;
						int dstNbChannels = 0;
						ulong dstChLayout = AV_CH_LAYOUT_STEREO;

						// buffer is going to be directly written to a rawaudio file, no alignment
						dstNbChannels = av_get_channel_layout_nb_channels(dstChLayout);

						swr = swr_alloc();
						if (swr == null)
							return;

						// チャンネルレイアウトがわからない場合は、チャンネル数から取得する。
						if (pAudioCodecCtx->channel_layout == 0)
							pAudioCodecCtx->channel_layout = (ulong)av_get_default_channel_layout(pAudioCodecCtx->channels);
						//
						av_opt_set_int(swr, "in_channel_layout", (long)pAudioCodecCtx->channel_layout, 0);
						av_opt_set_int(swr, "out_channel_layout", (long)dstChLayout, 0);
						av_opt_set_int(swr, "in_sample_rate", pAudioCodecCtx->sample_rate, 0);
						av_opt_set_int(swr, "out_sample_rate", dstRate, 0);
						av_opt_set_sample_fmt(swr, "in_sample_fmt", pAudioCodecCtx->sample_fmt, 0);
						av_opt_set_sample_fmt(swr, "out_sample_fmt", dstSampleFmt, 0);
						if (swr_init(swr) < 0)
							return;

						closingAction += () =>
						{
							fixed (SwrContext** @ref = &swr)
							{
								swr_free(@ref);
							}
						};

						IntPtr dev;
						ContextHandle ctx;

						dev = Alc.OpenDevice(null);
						if (dev == IntPtr.Zero)
							return;
						else
						{
							closingAction += () =>
							{
								Alc.CloseDevice(dev);
							};
						}

						ctx = Alc.CreateContext(dev, (int*)null);
						Alc.MakeContextCurrent(ctx);
						if (ctx.Handle == IntPtr.Zero)
							return;
						else
						{
							closingAction += () =>
							{
								Alc.MakeContextCurrent(ContextHandle.Zero);
								Alc.DestroyContext(ctx);
							};
						}

						const int NUM_BUFFERS = 3;
						int source;
						int[] buffers;

						buffers = AL.GenBuffers(NUM_BUFFERS);
						AL.GenSources(1, out source);
						if (AL.GetError() != ALError.NoError)
							return;

						closingAction += () =>
						{
							AL.DeleteSources(1, ref source);
							AL.DeleteBuffers(buffers);
						};

						Task.Run(() =>
						{
							byte* audioBuf = stackalloc byte[192000];//AVCODEC_MAX_AUDIO_FRAME_SIZE];
							int audioBufSize = 0;

							// NUM_BUFFERSの数だけ、あらかじめバッファを準備する。
							int i;
							for (i = 0; i < NUM_BUFFERS; i++)
							{
								// パケットキューにたまっているパケットを取得する。
								AVPacket decodingPacket;
								fixed (PacketQueue* @ref = &aPktQueue)
								{
									if (popPacketFromPacketQueue(@ref, &decodingPacket) < 0)
									{
										Console.WriteLine("error.");
										break;
									}
								}

								// 取得したパケットをデコードして、希望のフォーマットに変換する。
								fixed (AVSampleFormat* @ref = &dstSampleFmt)
								{
									audioBufSize = decode(&audioBuf[0], 192000, &decodingPacket,
										  pAudioCodecCtx, swr, dstRate, dstNbChannels, @ref);
								}

								// デコード、変換したデータを、OpenALのバッファに書き込む。
								AL.BufferData(buffers[i], ALFormat.Stereo16, (IntPtr)audioBuf, audioBufSize, dstRate);
								if (AL.GetError() != ALError.NoError)
								{
									ffmpeg.av_free_packet(&decodingPacket);
									continue;
								}


								av_free_packet(&decodingPacket);
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
							while (aPktQueue.numOfPackets > 0 && playing)
							{

								// 使用済みバッファの数を取得する。
								// 使用済みバッファがない場合は、できるまで繰り返す。
								int val;

								AL.GetSource(source, ALGetSourcei.BuffersProcessed, out val);
								if (val <= 0)
								{
									// 少しスリープさせて処理を減らす。
									//	Thread.Sleep(1);
									continue;
								}

								AVPacket decodingPacket;
								fixed (PacketQueue* @ref = &aPktQueue)
								{
									if (popPacketFromPacketQueue(@ref, &decodingPacket) < 0)
									{
										Console.WriteLine("error.");
										break;
									}
								}

								fixed (AVSampleFormat* @ref = &dstSampleFmt)
								{
									audioBufSize = decode(&audioBuf[0], 192000, &decodingPacket,
									  pAudioCodecCtx, swr, dstRate, dstNbChannels, @ref);
									if (audioBufSize <= 0)
									{
										continue;
									}
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
							while (aPktQueue.numOfPackets > 0)
							{
								AVPacket decodingPacket;
								fixed (PacketQueue* @ref = &aPktQueue)
								{
									if (popPacketFromPacketQueue(@ref, &decodingPacket) < 0)
										continue;
								}

								ffmpeg.av_free_packet(&decodingPacket);
							}
						});
					}

					// video init
					{
						// Allocate an AVFrame structure
						AVFrame* pFrameRGB = av_frame_alloc();
						if (pFrameRGB == null)
							return;

						// Determine required buffer size and allocate buffer
						int numBytes = avpicture_get_size(AVPixelFormat.AV_PIX_FMT_RGB24, pVideoCodecCtx->width,
									pVideoCodecCtx->height);
						byte* buffer = (byte*)av_malloc((ulong)numBytes * sizeof(byte));

						// Assign appropriate parts of buffer to image planes in pFrameRGB
						// Note that pFrameRGB is an AVFrame, but AVFrame is a superset
						// of AVPicture
						avpicture_fill((AVPicture*)pFrameRGB, (sbyte*)buffer, AVPixelFormat.AV_PIX_FMT_RGB24,
						   pVideoCodecCtx->width, pVideoCodecCtx->height);
						WriteableBitmap wb = null;
						Dispatcher.Invoke(() => this.image.Source = wb = new WriteableBitmap(pVideoCodecCtx->width, pVideoCodecCtx->height, 96, 96, PixelFormats.Rgb24, null));

						// initialize SWS context for software scaling
						sws_ctx = sws_getContext(pVideoCodecCtx->width,
								 pVideoCodecCtx->height,
								 pVideoCodecCtx->pix_fmt,
								 pVideoCodecCtx->width,
								 pVideoCodecCtx->height,
								 AVPixelFormat.AV_PIX_FMT_RGB24,
								 SWS_BILINEAR,
								 null,
								 null,
								 null
								 );

						// Allocate video frame
						AVFrame* pFrame = av_frame_alloc();
						// Read frames and save first five frames to disk
						int i = 0;
						int frameFinished = 0;
						double pts;
						Stopwatch sw = Stopwatch.StartNew();
						while (vPktQueue.numOfPackets > 0)
						{
							AVPacket decodingPacket;
							fixed (PacketQueue* @ref = &vPktQueue)
							{
								if (popPacketFromPacketQueue(@ref, &decodingPacket) < 0)
								{
									Console.WriteLine("error.");
									break;
								}
							}

							pts = 0;

							// Decode video frame
							avcodec_decode_video2(pVideoCodecCtx, pFrame, &frameFinished, &decodingPacket);

							if((ulong)decodingPacket.dts != AV_NOPTS_VALUE)
							{
								pts = av_frame_get_best_effort_timestamp(pFrame);
							}
							else
							{
								pts = 0;
							}
							pts *= av_q2d(vStream->time_base);

							// Did we get a video frame?
							if (frameFinished != 0)
							{
								var diff = sw.Elapsed.TotalSeconds - pts;
								Dispatcher.Invoke(new Action(() =>
								{
									this.Title = $"{diff}";
								}));
								// Convert the image from its native format to RGB
								sws_scale(sws_ctx, &pFrame->data0,
							  pFrame->linesize, 0, pVideoCodecCtx->height,
							  &pFrameRGB->data0, pFrameRGB->linesize);

								// Save the frame to disk
								SaveFrame(wb, pFrameRGB,
									pVideoCodecCtx->width, pVideoCodecCtx->height, i, diff);
							}

							// Free the packet that was allocated by av_read_frame
							av_free_packet(&decodingPacket);
						}

						// 未処理のパケットが残っている場合は、パケットを解放する。
						while (vPktQueue.numOfPackets > 0)
						{
							AVPacket decodingPacket;
							fixed (PacketQueue* @ref = &vPktQueue)
							{
								if (popPacketFromPacketQueue(@ref, &decodingPacket) < 0)
									continue;
							}

							ffmpeg.av_free_packet(&decodingPacket);
						}
						// Free the RGB image
						av_free(buffer);
						av_frame_free(&pFrameRGB);

						// Free the YUV frame
						av_frame_free(&pFrame);
					}
				}
				finally
				{
					var dels = closingAction?.GetInvocationList();
					for (var i = dels.Length - 1; i >= 0; i--)
						dels[i].DynamicInvoke(null);
				}
			});
		}

		unsafe void SaveFrame(
			WriteableBitmap wb, AVFrame* pFrame,
			int width, int height, int iFrame, double diff)
		{
			if (diff < 0)
			{
				Dispatcher.Invoke(() =>
				{
					wb.WritePixels(new Int32Rect(0, 0, width, height), (IntPtr)pFrame->data0, pFrame->linesize[0] * height, pFrame->linesize[0]);
				});
			}

			if (diff < 0)
			{
				Task.Delay(TimeSpan.FromSeconds(-diff)).Wait();
			//	Task.Delay(32).Wait();
			}
		}

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

		static double av_q2d(AVRational a)
		{
			return a.num / (double)a.den;
		}

	}
}

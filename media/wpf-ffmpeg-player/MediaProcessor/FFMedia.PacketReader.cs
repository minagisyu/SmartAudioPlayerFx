using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public sealed unsafe class PacketReader : IDisposable
		{
			readonly FFMedia media;
			bool disposed = false;

			// Video
			const int MAX_VIDEOQ_SIZE = (5 * 256 * 1024);
			volatile int video_sid = -1;
			volatile int video_packetq_size = 0;
			ConcurrentQueue<AVPacket> video_queue = new ConcurrentQueue<AVPacket>();

			// Audio
			const int MAX_AUDIOQ_SIZE = (5 * 16 * 1024);
			volatile int audio_sid = -1;
			volatile int audio_packetq_size = 0;
			ConcurrentQueue<AVPacket> audio_queue = new ConcurrentQueue<AVPacket>();

			// BufferFiller
			readonly CancellationTokenSource BufferFillTask_CTS = new CancellationTokenSource();
			readonly Task BufferFillTask;

			public PacketReader(FFMedia media)
			{
				this.media = media;
				BufferFillTask = Task.Factory.StartNew(
					BufferFiller, BufferFillTask_CTS.Token,
					TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}

			#region Dispose

			~PacketReader() => Dispose(false);

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion

			void Dispose(bool disposing)
			{
				if (disposed) return;

				if (disposing)
				{
					// マネージリソースの破棄
					try
					{
						BufferFillTask_CTS.Cancel();
						BufferFillTask.Wait();
					}
					catch (AggregateException) { }
				}

				// アンマネージリソースの破棄
				AVPacket packet;
				while (video_queue.TryDequeue(out packet))
				{
					av_free_packet(&packet);
				}
				while (audio_queue.TryDequeue(out packet))
				{
					av_free_packet(&packet);
				}

				disposed = true;
			}

			void SelectSIDInternal(AVMediaType mediaType, int sid)
			{
				// SIDが-1以下なら無効
				if (sid < 0)
					throw new ArgumentException("Invalid SID", nameof(sid));

				// SIDをセット、既存のキューを破棄
				ConcurrentQueue<AVPacket> queue =
					(mediaType == AVMediaType.AVMEDIA_TYPE_VIDEO) ? video_queue :
					(mediaType == AVMediaType.AVMEDIA_TYPE_AUDIO) ? audio_queue :
					null;
				while (queue?.TryDequeue(out var packet) ?? false)
				{
					av_free_packet(&packet);
				}

				if (mediaType == AVMediaType.AVMEDIA_TYPE_VIDEO)
					video_sid = sid;
				else if (mediaType == AVMediaType.AVMEDIA_TYPE_AUDIO)
					audio_sid = sid;
			}
			public void SelectVideoSID(int sid)
				=> SelectSIDInternal(AVMediaType.AVMEDIA_TYPE_VIDEO, sid);
			public void SelectAudioSID(int sid)
				=> SelectSIDInternal(AVMediaType.AVMEDIA_TYPE_AUDIO, sid);

			void BufferFiller()
			{
				while (true)
				{
					// キャンセルされてる？
					if (BufferFillTask_CTS.Token.IsCancellationRequested)
					{
						// フラッシュ処理
						return;
					}

					// SIDが設定されていないなら読み込まない
					if (video_sid < 0 && audio_sid < 0)
					{
						Task.Delay(50).Wait();
						continue;
					}

					// バッファに空きがないなら読み込まない
					if (video_packetq_size >= MAX_VIDEOQ_SIZE ||
						audio_packetq_size >= MAX_AUDIOQ_SIZE)
					{
						Task.Delay(50).Wait();
						continue;
					}

					// フレームを一つ読み込む
					AVPacket reading_packet;
					av_init_packet(&reading_packet);
					if (av_read_frame(media.pFormatCtx, &reading_packet) < 0)
					{
						// 失敗、フラッシュ処理をする...
						return;
					}

					// キープするSIDならキューに追加、次のフレームを読み込む
					if (reading_packet.stream_index == video_sid)
					{
						av_dup_packet(&reading_packet);
						video_queue.Enqueue(reading_packet);
						lock (video_queue)
						{
							video_packetq_size += reading_packet.size;
						}
					}
					else if (reading_packet.stream_index == audio_sid)
					{
						av_dup_packet(&reading_packet);
						audio_queue.Enqueue(reading_packet);
						lock (audio_queue)
						{
							audio_packetq_size += reading_packet.size;
						}
					}
					else
					{
						// 不要なので解放
						av_free_packet(&reading_packet);
					}
				}
			}

			bool TakeFrameInternal(AVMediaType mediaType, out AVPacket? result)
			{
				ConcurrentQueue<AVPacket> queue =
					(mediaType == AVMediaType.AVMEDIA_TYPE_VIDEO) ? video_queue :
					(mediaType == AVMediaType.AVMEDIA_TYPE_AUDIO) ? audio_queue :
					null;

				// キューがあるならそれを返す
				if (queue != null && queue.TryDequeue(out var packet))
				{
					lock (queue)
					{
						result = packet;
						if (mediaType == AVMediaType.AVMEDIA_TYPE_VIDEO)
							video_packetq_size -= packet.size;
						else if (mediaType == AVMediaType.AVMEDIA_TYPE_AUDIO)
							audio_packetq_size -= packet.size;
					}
					return true;
				}

				// BufferFillerがまだ動いてるならtrueを返す？
				result = null;
				if (BufferFillTask.IsCompleted == false)
					return true;
				else
					return false;
			}
			public bool TakeVideoFrame(out AVPacket? result)
				=> TakeFrameInternal(AVMediaType.AVMEDIA_TYPE_VIDEO, out result);
			public bool TakeAudioFrame(out AVPacket? result)
				=> TakeFrameInternal(AVMediaType.AVMEDIA_TYPE_AUDIO, out result);

		}
	}
}

using FFmpeg.AutoGen;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using static FFmpeg.AutoGen.ffmpeg;

namespace SmartAudioPlayer.MediaProcessor
{
	public delegate void CodecFlushRequestDelegate(long pts);

	public sealed unsafe class PacketReader : IDisposable
	{
		readonly AVFormatContext* pFormatCtx;
		readonly int audio_sid, video_sid;
		bool disposed = false;

		public PacketReader(AVFormatContext* pFormatCtx, int? audio_sid, int? video_sid, bool video_read_skip)
		{
			this.pFormatCtx = pFormatCtx;
			this.audio_sid = audio_sid ?? -1;
			this.video_sid = video_sid ?? -1;
			this.video_read_skip = video_read_skip;

			if (audio_sid < 0 && video_sid < 0)
				throw new FFMediaException("no reading packet, invalid audio and video sid.");

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
				BufferFillTask_CTS.Cancel();
				BufferFillTask.Wait();
			}

			// アンマネージリソースの破棄
			ClearAudioPacketQueue();
			ClearVideoPacketQueue();

			disposed = true;
		}

		void ClearAudioPacketQueue()
		{
			AVPacket packet;
			while (audio_queue.TryDequeue(out packet))
			{
				av_free_packet(&packet);
			}
			audio_packetq_size = 0;
		}
		void ClearVideoPacketQueue()
		{
			AVPacket packet;
			while (video_queue.TryDequeue(out packet))
			{
				av_free_packet(&packet);
			}
			video_packetq_size = 0;
		}

		// Video
		const int MAX_VIDEOQ_SIZE = (5 * 256 * 1024);
		volatile int video_packetq_size = 0;
		ConcurrentQueue<AVPacket> video_queue = new ConcurrentQueue<AVPacket>();

		// Audio
		const int MAX_AUDIOQ_SIZE = (5 * 16 * 1024);
		volatile int audio_packetq_size = 0;
		ConcurrentQueue<AVPacket> audio_queue = new ConcurrentQueue<AVPacket>();

		// BufferFiller
		readonly CancellationTokenSource BufferFillTask_CTS = new CancellationTokenSource();
		Task BufferFillTask;
		volatile bool flushing = false;

		// clock, seek, position
		volatile bool seek_req = false;
		long seek_pos = 0;
		volatile bool paused = false;
		volatile bool video_read_skip = true;
		public event CodecFlushRequestDelegate FlushRequest;

		void BufferFiller()
		{
			while (true)
			{
				// シーク処理
				if (seek_req)
				{
					long seek_target = seek_pos;
					// Audio Stream優先でシーク処理をする
					int sid =
						(audio_sid >= 0) ? audio_sid :
						(video_sid >= 0) ? video_sid :
						-1;

					var timestamp =
						(sid >= 0) ? av_rescale_q(seek_target, FFMedia.AV_TIME_BASE_Q_impl, pFormatCtx->streams[sid]->time_base) :
						seek_target;

					av_seek_frame(pFormatCtx, sid, timestamp, 0);
					ClearAudioPacketQueue();
					ClearVideoPacketQueue();
					var pts = av_rescale_q(seek_target, FFMedia.AV_TIME_BASE_Q_impl, pFormatCtx->streams[sid]->time_base);
					FlushRequest?.Invoke(pts);
					seek_req = false;
					paused = false;
				}

				// キャンセルされてる？
				if (BufferFillTask_CTS.Token.IsCancellationRequested)
				{
					// フラッシュ処理
					ClearAudioPacketQueue();
					ClearVideoPacketQueue();
					paused = true;
					return;
				}

				// SIDが設定されていないなら読み込まない
				// 来ることはないと思うが念のため
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

				// 停止状態なら読み込まない
				if (paused)
				{
					Task.Delay(50).Wait();
					continue;
				}

				// フレームを一つ読み込む
				AVPacket reading_packet;
				av_init_packet(&reading_packet);
				if (av_read_frame(pFormatCtx, &reading_packet) < 0)
				{
					// 失敗 or ファイル終了
					// -> 停止状態へ (シーク後再生があり得るため)
					paused = true;
					return;
				}

				// キープするSIDならキューに追加、次のフレームを読み込む
				if ((reading_packet.stream_index == video_sid) && (video_read_skip == false))
				{
					av_dup_packet(&reading_packet);
					video_queue.Enqueue(reading_packet);
					Interlocked.Add(ref video_packetq_size, reading_packet.size);
				}
				else if (reading_packet.stream_index == audio_sid)
				{
					av_dup_packet(&reading_packet);
					audio_queue.Enqueue(reading_packet);
					Interlocked.Add(ref audio_packetq_size, reading_packet.size);
				}
				else
				{
					// 不要なので解放
					av_free_packet(&reading_packet);
				}
			}
		}

		public bool TakeVideoFrame(out AVPacket? result)
			=> TakeFrameInternal(AVMediaType.AVMEDIA_TYPE_VIDEO, out result);
		public bool TakeAudioFrame(out AVPacket? result)
			=> TakeFrameInternal(AVMediaType.AVMEDIA_TYPE_AUDIO, out result);
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
			// paused は EOFかキャンセル時にしかtrueにならない
			result = null;
			if (paused == false)
				return true;
			else
				return false;
		}

		long external_clock_base = FFMedia.av_gettime_impl();
		double get_master_clock()
		{
			// とりあえず適当...
			return (FFMedia.av_gettime_impl() - external_clock_base) / 1000000.0;
		}
		public void Seek(double incr)
		{
			if (seek_req) return;

			var newtime = get_master_clock() + incr;
			if (newtime <= 0.0)
			{
				seek_pos = 0;
			}
			else
			{
				seek_pos = (long)(newtime * AV_TIME_BASE);
			}
			seek_req = true;
		}

		public void SetVideoReadSkip(bool value)
			=> video_read_skip = value;
	}
}

using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe sealed class Demuxer : IDisposable
		{
			bool disposed = false;
			readonly CompositeDisposable disposable;
			readonly FFMedia media;
			readonly int? video_SID, audio_SID;
			long external_clock_base;
			volatile bool seek_req;
			long seek_pos;
			volatile bool direct_req;

			public Demuxer(FFMedia media)
			{
				disposable = new CompositeDisposable();
				this.media = media;
				GetDefaultSID(out video_SID, out audio_SID);

				external_clock_base = av_gettime_impl();
			}

			public AudioDecoder CreateAudioDecoder()
			{
				var dec = new AudioDecoder(media, audio_SID ?? -1);
				disposable.Add(dec);
				return dec;
			}
			public VideoDecoder CreateVideoDecoder()
			{
				var dec = new VideoDecoder(media, audio_SID ?? -1);
				disposable.Add(dec);
				return dec;
			}

			#region Dispose

			~Demuxer() => Dispose(false);

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
				}

				// アンマネージリソースの破棄
				disposable.Dispose();

				disposed = true;
			}

			void GetDefaultSID(out int? video_SID, out int? audio_SID)
			{
				video_SID = null;
				audio_SID = null;
				// Find the first video and audio streams
				for (int i = 0; i < media.pFormatCtx->nb_streams; i++)
				{
					if (media.pFormatCtx->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO && video_SID == null)
						video_SID = i;
					else if (media.pFormatCtx->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO && audio_SID == null)
						audio_SID = i;
				}
			}

			async Task<IEnumerable<int>> PacketParserAsync()
			{
				while (!media.quit)
				{
					if (seek_req)
					{
						var seek_target = seek_pos;

						// Prefer seeking on the video stream
						var sid = video_SID ?? audio_SID;

						// Get a seek timestamp for the appropriate stream
						var timestamp = seek_target;
						if (sid.HasValue)
							timestamp = av_rescale_q(seek_target, AV_TIME_BASE_Q_impl, media.pFormatCtx->streams[sid.Value]->time_base);

						if (av_seek_frame(media.pFormatCtx, sid.Value, timestamp, 0) < 0)
						{
							throw new ApplicationException($"{media.filename}: error while seeking");
						}
						else
						{
							// Seek successful, clear the packet queues and send a special
							// 'flush' packet with the new stream clock time

							// packet queue clear.
							// flushpkt.pts = timebase(rescaled)

							external_clock_base = av_gettime_impl() - seek_target;
						}
						seek_req = false;
					}

					// packet queue size >= MAX_xxxQ_SIZE to Delay...
					// continue.

					AVPacket packet;
					av_init_packet(&packet);
					if (av_read_frame(media.pFormatCtx, &packet) < 0)
					{
						// queue flush
						break;
					}

					// Place the packet in the queue it's meant for, or discard it
					if (packet.stream_index == (video_SID ?? -1))
					{
						//packet_queue_put(&movState->video.q, packet);
					}
					else if (packet.stream_index == (audio_SID ?? -1))
					{
						// packet_queue_put(&movState->audio.q, packet);
					}
					else
					{
						av_free_packet(&packet);
					}

					// all done - wait for it
					while (!media.quit)
					{
						// packet q not empty, processing wait...
						await Task.Delay(100);
					}

					// fail or dispose
					media.quit = true;
					// packet q flush
					// dispose?
				}
			}


			IEnumerable<AVPacket> PacketParser2Async()
			{
				while (!media.quit)
				{
					CreateAVPacket(out var packet);
					if (!FFMedia.ReadFrame(media, ref packet))
					{
						// queue flush
						break;
					}

					// Place the packet in the queue it's meant for, or discard it
					if (packet.stream_index == (video_SID ?? -1))
					{
						//packet_queue_put(&movState->video.q, packet);
						yield return packet;
					}
					else if (packet.stream_index == (audio_SID ?? -1))
					{
						// packet_queue_put(&movState->audio.q, packet);
						yield return packet;
					}
					else
					{
						FreeAVPacket(ref packet);
					}

				}
			}
		}
	}
}

using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Collections.Concurrent;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public sealed unsafe class PacketReader : IDisposable
		{
			readonly FFMedia media;
			AVPacket reading_packet;
			readonly ConcurrentDictionary<int, ConcurrentQueue<IntPtr>> packetq; // <sid, queue<(AVPacket)>>
			bool disposed = false;

			public PacketReader(FFMedia media)
			{
				this.media = media;
				fixed (AVPacket* @ref = &reading_packet)
				{
					av_init_packet(@ref);
				}
				packetq = new ConcurrentDictionary<int, ConcurrentQueue<IntPtr>>();
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
				}

				// アンマネージリソースの破棄
				packetq.Keys.ToList().ForEach(sid => IgnoreSID(sid));

				disposed = true;
			}

			public void KeepSID(int sid)
				=> packetq.GetOrAdd(sid, (_) => new ConcurrentQueue<IntPtr>());

			public void IgnoreSID(int sid)
			{
				if (packetq.TryRemove(sid, out var queue) == false) return;

				while(queue.TryDequeue(out var packet))
				{
					av_free_packet((AVPacket*)packet);
				}
			}

			public bool TakeFrame(int sid, out AVPacket* result)
			{
				// ・av_read_frameして返す
				// ・不要なSIDはav_free_packetする
				// ・av_read_frameで欲しいSIDが来ないが、必要なSIDならキューに保存する
				// ・SIDのキューがある場合はそちらを先に返し、av_read_frameは呼ばない


				// KeepSIDされてて、キューがあるならそれを返す
				var is_sid_keep = packetq.TryGetValue(sid, out var queue);
				if (is_sid_keep)
				{
					if (queue.TryDequeue(out var packet))
					{
						result = (AVPacket*)packet;
						return true;
					}
				}

				// フレームをひとつ読み込む
				fixed (AVPacket* @ref = &reading_packet)
				{
					while (av_read_frame(media.pFormatCtx, @ref) >= 0)
					{
						// 目的のSIDパケットなら複製して返す
						if (reading_packet.stream_index == sid)
						{
							av_dup_packet(@ref);
							result = @ref;
							return true;
						}
						// キープするSIDならキューに追加、次のフレームを読み込む
						else if (is_sid_keep)
						{
							av_dup_packet(@ref);
							queue.Enqueue((IntPtr)@ref);
						}
						// 不要なので解放して次のパケットを読み込む
						else
						{
							ffmpeg.av_free_packet(@ref);
						}
					}
				}

				// 読み込み失敗 or EOFでwhileを抜けてくるはず...
				result = null;
				return false;
			}
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

	}
}

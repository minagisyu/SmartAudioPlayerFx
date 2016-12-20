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
		public sealed unsafe class PacketQueue : IDisposable
		{
			AVPacket reading_packet;
			ConcurrentDictionary<int, ConcurrentQueue<AVPacket>> packetq; // <sid, queue>

			public PacketQueue()
			{
				fixed (AVPacket* @ref = &reading_packet)
				{
					av_init_packet(@ref);
				}
				packetq = new ConcurrentDictionary<int, ConcurrentQueue<AVPacket>>();
			}

			public void Dispose()
			{
			}

			public void KeepSID(int sid)
				=> packetq.GetOrAdd(sid, (_) => new ConcurrentQueue<AVPacket>());

			public void IgnoreSID(int sid)
				=> packetq.TryRemove(sid, out var _);

			public void TakeFrame(int sid)
			{
				if (packetq.TryGetValue(sid, out var queue) == false) return;

				//
				int read_result = 0;
				fixed (AVPacket* @ref = &reading_packet)
				{
					read_result = av_read_frame(pFormatCtx, @ref);
				}

				if (read_result < 0)
				{
					// fail or EOF.
					// flush_packet...
				}


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

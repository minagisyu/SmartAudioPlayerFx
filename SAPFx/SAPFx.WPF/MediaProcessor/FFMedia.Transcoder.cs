using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe abstract class Transcoder : IDisposable
		{
			bool disposed = false;
			protected readonly FFMedia media;
			protected readonly int sid;
			protected readonly AVStream* stream;
			protected AVCodecContext* pCodecCtx = null;
			protected AVCodec* pCodec = null;

			public Transcoder(FFMedia media, int sid)
			{
				this.media = media;
				this.sid = sid;

				if (sid < 0 || (uint)sid >= media.pFormatCtx->nb_streams)
					throw new ArgumentOutOfRangeException(nameof(sid));

				stream = media.pFormatCtx->streams[sid];
				pCodecCtx = stream->codec;
				pCodec = avcodec_find_decoder(pCodecCtx->codec_id);
				if(pCodec == null || avcodec_open2(pCodecCtx, pCodec, null) < 0)
					throw new ApplicationException("Unsupported codec");
			}

			#region Dispose

			~Transcoder() => Dispose(false);

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion

			protected virtual void Dispose(bool disposing)
			{
				if (disposed) return;
				if (disposing)
				{
					// マネージリソースの破棄
				}

				// アンマネージリソースの破棄

				disposed = true;
			}

		}
	}
}

using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe abstract class Transcoder : IDisposable
		{
			bool disposed = false;
			protected readonly AVFormatContext* pFormatCtx;
			protected readonly AVMediaType mediaType;
			public readonly int sid;
			protected readonly AVStream* stream;
			protected AVCodec* pCodec = null;
			protected AVCodecContext* pCodecCtx = null;

			protected Transcoder(AVFormatContext* pFormatCtx, AVMediaType mediaType)
			{
				this.pFormatCtx = pFormatCtx;
				this.mediaType = mediaType;

				fixed (AVCodec** @ref = &pCodec)
				{
					sid = av_find_best_stream(pFormatCtx, mediaType, -1, -1, @ref, 0);
					if(sid < 0)
						throw new FFMediaException($"can't find beat stream. (MediaType:{mediaType})");
				}

				stream = pFormatCtx->streams[sid];
				pCodecCtx = stream->codec;
				pCodecCtx->codec = pCodec;

				// open codec
				if(avcodec_open2(pCodecCtx, pCodecCtx->codec, null) < 0)
					throw new FFMediaException($"failed open codec.");
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
				if (pCodecCtx != null)
				{
					avcodec_close(pCodecCtx);
					pCodecCtx = null;
				}
				disposed = true;
			}
		}
	}
}

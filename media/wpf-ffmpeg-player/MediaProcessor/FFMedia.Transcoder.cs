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
			protected readonly AVMediaType mediaType;
			protected readonly int sid;
			protected readonly AVStream* stream;
			protected AVCodec* pCodec = null;
			protected AVCodecContext* pCodecCtx = null;

			public Transcoder(FFMedia media, AVMediaType mediaType)
			{
				this.media = media;
				this.mediaType = mediaType;

				fixed (AVCodec** @ref = &pCodec)
				{
					sid = av_find_best_stream(media.pFormatCtx, mediaType, -1, -1, @ref, 0);
					if(sid < 0)
						throw new FFMediaException($"can't find beat stream. (MediaType:{mediaType})");
				}

				if(mediaType == AVMediaType.AVMEDIA_TYPE_VIDEO)
					media.reader.SelectVideoSID(sid);
				else if(mediaType == AVMediaType.AVMEDIA_TYPE_AUDIO)
					media.reader.SelectAudioSID(sid);
				stream = media.pFormatCtx->streams[sid];
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
				avcodec_close(pCodecCtx);

				disposed = true;
			}

		}
	}
}

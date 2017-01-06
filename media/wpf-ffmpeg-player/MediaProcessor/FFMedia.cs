using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace SmartAudioPlayer.MediaProcessor
{
	// FFMedia
	// - PacketReader, VideoTranscoder, AudioTranscoder,
	//
	// xxxTranscoder
	// - DecodeLoop, Decoded RAW Buffer, TakeFrame(xxxFrame)
	//
	// MediaPlayback
	// - Open/Close, ALAudio, WPFVideo
	// - Play/Pause/Seek/Duration/Volume, PTS_Sync

	// [MovieContext]
	// FormatContext, SID決定, PacketReader, 
	public unsafe sealed partial class FFMedia : IDisposable
	{
		static FFMedia()
		{
			av_register_all();
			av_log_set_level(AV_LOG_QUIET);
		}
		public static void LibraryInitialize() { /* static constractor caller */}

		bool disposed = false;

		public FFMedia(string filename)
		{
			if (Open(filename) == false)
				throw new FFMediaException($"Failed to open: {filename}");
		}

		#region Dispose

		~FFMedia() => Dispose(false);

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
				packet_reader?.Dispose();
				audio_dec?.Dispose();
				video_dec?.Dispose();
			}

			// アンマネージリソースの破棄
			if (pFormatCtx != null)
			{
				fixed (AVFormatContext** @ref = &pFormatCtx)
				{
					avformat_close_input(@ref);
				}
				pFormatCtx = null;
			}

			disposed = true;
		}

		string filename;
		AVFormatContext* pFormatCtx = null;
		public AudioTranscoder audio_dec = null;
		public VideoTranscoder video_dec = null;
		public PacketReader packet_reader = null;

		bool Open(string filename)
		{
			this.filename = filename;
			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				if (avformat_open_input(@ref, filename, null, null) != 0)
					return false;
			}

			// retrive stream infomation
			if (avformat_find_stream_info(pFormatCtx, null) < 0)
				return false;

			audio_dec = AudioTranscoder.Create(pFormatCtx);
			video_dec = VideoTranscoder.Create(pFormatCtx);
			packet_reader = new PacketReader(pFormatCtx, audio_dec?.sid, video_dec?.sid);
			packet_reader.FlushRequest += pts =>
			{
				// audiodec, videodec, Flush
			};

			return true;
		}

		//=[ PacketReader ]======


	}
}

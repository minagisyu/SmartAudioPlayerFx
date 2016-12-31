using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;

namespace SmartAudioPlayer.MediaProcessor
{
	public unsafe sealed partial class FFMedia : IDisposable
	{
		static FFMedia()
		{
			av_register_all();
			av_log_set_level(AV_LOG_QUIET);
		}
		public static void LibraryInitialize() { /* static constractor caller */}

		readonly string filename;
		AVFormatContext* pFormatCtx = null;
		bool disposed = false;
		//
		public readonly PacketReader reader;

		public FFMedia(string filename)
		{
			this.filename = filename;

			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				if (avformat_open_input(@ref, filename, null, null) != 0)
					throw new FFMediaException($"Failed to open: {filename}");
			}

			// retrive stream infomation
			if (avformat_find_stream_info(pFormatCtx, null) < 0)
				throw new FFMediaException($"Failed to find stream info: {filename}");

			reader = new PacketReader(this);
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
				reader.Dispose();
			}

			// アンマネージリソースの破棄
			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				avformat_close_input(@ref);
			}

			disposed = true;
		}




	}
}

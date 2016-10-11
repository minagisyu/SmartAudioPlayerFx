using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FFmpeg.AutoGen.ffmpeg;

namespace wpf_ffmpeg_player
{
	unsafe partial class FFMedia : IDisposable
	{
		static bool library_initialized = false;
		public static void Initialize()
		{
			if (library_initialized) return;

			av_register_all();
			library_initialized = true;
		}

		AVFormatContext* pFormatCtx = null;
		bool failed = false;

		public FFMedia(string file)
		{
			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				if (avformat_open_input(@ref, file, null, null) != 0)
				{
					failed = true;
					return;
				}
			}

			// retrive stream infomation 
			if (avformat_find_stream_info(pFormatCtx, null) < 0)
			{
				failed = true;
				return;
			}
		}

		~FFMedia() { Dispose(false); }
		public void Dispose() { Dispose(true); }

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{

			}

			if (pFormatCtx != null)
			{
				fixed (AVFormatContext** @ref = &pFormatCtx)
				{
					avformat_close_input(@ref);
				}
				pFormatCtx = null;
			}
		}

	}
}

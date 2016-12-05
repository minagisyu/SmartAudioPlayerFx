using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reactive.Disposables;

namespace SmartAudioPlayer.MediaProcessor
{
	public unsafe sealed partial class FFMedia : IDisposable
	{
		static FFMedia()
		{
			// register all formats and codecs
			av_register_all();
		}

		string filename;
		AVFormatContext* pFormatCtx = null;
		volatile bool quit;
		GCHandle _interrupt_cb_handle;
		CompositeDisposable demuxers = new CompositeDisposable();
		bool disposed = false;

		public FFMedia(string filename)
		{
			this.filename = filename;

			var interrupt_cb_delegate = new Func<IntPtr, int>(interrupt_cb_func);
			_interrupt_cb_handle = GCHandle.Alloc(interrupt_cb_delegate, GCHandleType.Pinned);

			pFormatCtx = avformat_alloc_context();
			pFormatCtx->interrupt_callback = new AVIOInterruptCB();
			pFormatCtx->interrupt_callback.callback = _interrupt_cb_handle.AddrOfPinnedObject();
			pFormatCtx->interrupt_callback.opaque = null;

			if(avio_open2(&pFormatCtx->pb, filename, AVIO_FLAG_READ, &pFormatCtx->interrupt_callback, null) != 0)
			{
				throw new ApplicationException($"Failed to open: {filename}");
			}

			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				if (avformat_open_input(@ref, filename, null, null) != 0)
					throw new ApplicationException($"Failed to open: {filename}");
			}

			// retrive stream infomation
			if (avformat_find_stream_info(pFormatCtx, null) < 0)
			{
				throw new ApplicationException($"Failed to find stream info: {filename}");
			}

			// Dump information about file onto standard error
			av_dump_format(pFormatCtx, 0, filename, 0);
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
				demuxers.Dispose();
			}

			// アンマネージリソースの破棄
			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				avformat_close_input(@ref);
			}

			_interrupt_cb_handle.Free();
		}

		int interrupt_cb_func(IntPtr ctx)
		{
			return quit ? 1 : 0;
		}

		public Demuxer CreateDemuxer()
		{
			var demuxer = new Demuxer(this);
			demuxers.Add(demuxer);
			return demuxer;
		}

		static long av_gettime_impl()
			=> DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond / 1000);

		static AVRational AV_TIME_BASE_Q_impl { get; } = new AVRational() { num = 1, den = AV_TIME_BASE };

		static void CreateAVPacket(out AVPacket packet)
		{
			fixed (AVPacket* @ref = &packet)
			{
				av_init_packet(@ref);
			}
		}
		static void FreeAVPacket(ref AVPacket packet)
		{
			fixed (AVPacket* @ref = &packet)
			{
				av_free_packet(@ref);
			}
		}

		static bool ReadFrame(FFMedia media, ref AVPacket packet)
		{
			fixed (AVPacket* @ref = &packet)
			{
				return av_read_frame(media.pFormatCtx, @ref) >= 0;
			}
		}

		static double av_q2d_impl(AVRational a)
		{
			return a.num / (double)a.den;
		}

	}
}

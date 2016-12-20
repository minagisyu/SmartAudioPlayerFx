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
		CompositeDisposable disposables = new CompositeDisposable();
		bool disposed = false;

		delegate int Interrupt_cb_delegate(IntPtr ctx);

		partial void Init_implement();

		public FFMedia(string filename)
		{
			Init_implement();

			this.filename = filename;

			var interrupt_cb_delegate = new Interrupt_cb_delegate(Interrupt_cb_func);
			_interrupt_cb_handle = GCHandle.Alloc(interrupt_cb_delegate);

			pFormatCtx = avformat_alloc_context();
			pFormatCtx->interrupt_callback = new AVIOInterruptCB()
			{
				callback = Marshal.GetFunctionPointerForDelegate(interrupt_cb_delegate),
				opaque = null
			};
			if (avio_open2(&pFormatCtx->pb, filename, AVIO_FLAG_READ, &pFormatCtx->interrupt_callback, null) != 0)
			{
				throw new FFMediaException($"Failed to open: {filename}");
			}

			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				if (avformat_open_input(@ref, filename, null, null) != 0)
					throw new FFMediaException($"Failed to open: {filename}");
			}

			// retrive stream infomation
			if (avformat_find_stream_info(pFormatCtx, null) < 0)
			{
				throw new FFMediaException($"Failed to find stream info: {filename}");
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
				disposables.Dispose();
			}

			// アンマネージリソースの破棄
			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				avformat_close_input(@ref);
			}

			_interrupt_cb_handle.Free();
		}

		int Interrupt_cb_func(IntPtr ctx)
		{
			return quit ? 1 : 0;
		}

	}

}

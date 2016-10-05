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
	unsafe class FFMedia : IDisposable
	{
		AVFormatContext* pFormatCtx = null;
		int videoStreamId = -1;
		int audioStreamId = -1;
		AVPacket packet;
		AVPacket flush_pkt; // seekしたときにこれまで蓄積したパケットを破棄する指示を送る

		bool seekRequest = false;
		long seekPosition = 0;
		bool seekFlag = false;

		double duration = 0.0;
		VideoStream video = null;
		AudioStream audio = null;
		string filename = null;
		bool quit = false;
		int NumLoop = -1;
		bool isRunning = false;

		public FFMedia()
		{
			fixed (AVPacket* @ref = &packet) { av_init_packet(@ref); }
			fixed (AVPacket* @ref = &flush_pkt) { av_init_packet(@ref); }
			flush_pkt.data = (sbyte*)Marshal.StringToHGlobalAnsi("FLUSH");
			fixed (AVPacket* @ref = &flush_pkt) { video.flush_pkt = audio.flush_pkt = @ref; }
		}

		~FFMedia() { Dispose(false); }
		public void Dispose() { Dispose(true); }

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{

			}

			if((IntPtr)flush_pkt.data != IntPtr.Zero)
			{
				Marshal.FreeHGlobal((IntPtr)flush_pkt.data);
				flush_pkt.data = (sbyte*)IntPtr.Zero;
			}
		}

		void InitAV()
		{
			pFormatCtx = null;
			seekRequest = false;
			seekPosition = 0;
			seekFlag = false;
			duration = 0;
			video.InitAV();
			audio.InitAV();
			quit = false;
		}

		public bool Start(bool flag)
		{
			if (string.IsNullOrEmpty(filename)) return false;
			if (isRunning) return true;

			InitAV();

			// フォーマットコンテキスト  動画ファイルを開く
			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				if (avformat_open_input(@ref, filename, null, null) != 0)
					return false;
			}

			// ストリーム情報を取得する
			if (avformat_find_stream_info(pFormatCtx, null) < 0)
			{
				fixed (AVFormatContext** @ref = &pFormatCtx) { avformat_close_input(@ref); }
				return false; // Couldn't find stream information
			}

			// 標準エラー出力へ ファイル に関する情報をダンプします
			av_dump_format(pFormatCtx, 0, filename, 0);

			// ストリームを検索
			videoStreamId = -1;
			audioStreamId = -1;
			for (var u = 0; u < pFormatCtx->nb_streams; u++)
			{
				if (pFormatCtx->streams[u]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO && videoStreamId == -1 && flag == false)
				{
					videoStreamId = u;
				}
				if (pFormatCtx->streams[u]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO && audioStreamId == -1)
				{
					audioStreamId = u;
				}
			}

			if (videoStreamId == -1 // ビデオ ストリームを見つけられませんでした
					&&
				audioStreamId == -1 // オーディオ ストリームを見つけられませんでした
			   )
			{
				fixed (AVFormatContext** @ref = &pFormatCtx) { avformat_close_input(@ref); }
				return false;
			}

			// 総時間 ?
			duration = pFormatCtx->duration / (double)AV_TIME_BASE;
			if (duration < 0.0)
				duration = pFormatCtx->streams[videoStreamId]->duration * av_q2d(pFormatCtx->streams[videoStreamId]->codec->time_base);
			if (duration < 0.0)
				duration = pFormatCtx->streams[audioStreamId]->duration * av_q2d(pFormatCtx->streams[audioStreamId]->codec->time_base);

			// ビデオスレッドを始める
			if (videoStreamId >= 0)
			{
				if (audioStreamId >= 0)
				{
					video.Audio = audio; // オーディオクロックと同期
				}
				if (!video.Start(pFormatCtx, videoStreamId))
				{
					return false;
				}
			}
			// オーディオスレッドを始める
			if (audioStreamId >= 0)
			{
				if (!audio.Start(pFormatCtx, audioStreamId))
				{
					video.End();
					return false;
				}
			}
			// 動画ファイルを読むスレッドを始める run()
			start();

			return true;
		}

		private void start()
		{
			throw new NotImplementedException();
		}

		static double av_q2d(AVRational a)
		{
			return a.num / (double)a.den;
		}
	}
}

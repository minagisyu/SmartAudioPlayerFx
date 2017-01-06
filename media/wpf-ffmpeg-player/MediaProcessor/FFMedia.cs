﻿using FFmpeg.AutoGen;
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

		public FFMedia(string filename, bool video_read_skip = true)
		{
			if (Open(filename, video_read_skip) == false)
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
		bool video_read_skip = true;
		AVFormatContext* pFormatCtx = null;
		public AudioDecoder audio_dec = null;
		public VideoDecoder video_dec = null;
		public PacketReader packet_reader = null;

		bool Open(string filename, bool video_read_skip)
		{
			this.filename = filename;
			this.video_read_skip = video_read_skip;
			fixed (AVFormatContext** @ref = &pFormatCtx)
			{
				if (avformat_open_input(@ref, filename, null, null) != 0)
					return false;
			}

			// retrive stream infomation
			if (avformat_find_stream_info(pFormatCtx, null) < 0)
				return false;

			// best SID
			int audio_sid = av_find_best_stream(pFormatCtx, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);
			int video_sid = av_find_best_stream(pFormatCtx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
			audio_dec = AudioDecoder.Create(pFormatCtx, audio_sid);
			video_dec = VideoDecoder.Create(pFormatCtx, video_sid);
			packet_reader = new PacketReader(pFormatCtx, audio_dec?.sid, video_dec?.sid, video_read_skip);
			packet_reader.FlushRequest += pts =>
			{
				// audiodec, videodec, Flush
			};

			return true;
		}

	}
}

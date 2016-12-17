using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAPFx.WPF.Sound;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe sealed class AudioTranscoder : Transcoder
		{
			const double AUDIO_DIFF_AVG_NB = 20;
			readonly double diff_avg_coef = Math.Exp(Math.Log(0.01) / AUDIO_DIFF_AVG_NB);
			readonly double diff_threshold = 2.0 * 0.050; // 50ms
			readonly int audioFormat;

			public AudioTranscoder(FFMedia media, int sid) : base(media, sid)
			{
				// alGenBuffers(BUFFER_Q_SIZE=8)
				// alGenSources(1)
				// alSourcei(AL_SOURCE_RELATIVE, AL_TRUE)
				// alSourcei(AL_ROLLOFF_FACTOR, 0)

				audioFormat = FindSuitableAudioFormat();
			}

			internal void TakeFrame()
			{
				throw new NotImplementedException();
			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);

				if (disposing)
				{
					// マネージリソースの破棄
				}

				// アンマネージリソースの破棄
			}

			int FindSuitableAudioFormat()
			{
				var audioFormat = ALDevice.SourceFormat.NONE;
				var dst_sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_NONE;

				switch (pCodecCtx->sample_fmt)
				{
					case AVSampleFormat.AV_SAMPLE_FMT_U8:
					case AVSampleFormat.AV_SAMPLE_FMT_U8P:
						
						break;

					case AVSampleFormat.AV_SAMPLE_FMT_FLT:
					case AVSampleFormat.AV_SAMPLE_FMT_FLTP:
						break;

					default:
						break;
				}


				return audioFormat;
			}

		}
	}
}

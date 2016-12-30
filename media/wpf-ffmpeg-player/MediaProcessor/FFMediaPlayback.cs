using SmartAudioPlayer.MediaProcessor.Audio;
using System;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	class FFMediaPlayback : IDisposable
	{
		FFMedia media;
		FFMedia.VideoTranscoder vtrans;
		FFMedia.AudioTranscoder atrans;
		ALDevice alDev;
		ALDevice.StreamingSource alStream;

		public FFMediaPlayback()
		{
			FFMedia.LibraryInitialize();
			alDev = new ALDevice();
			alStream = alDev.CreateStreamingSource(8);
		}

		public async Task PlayAsync(string file, bool enableVideo = false)
		{
			media = new FFMedia(file);
			vtrans = enableVideo ? new FFMedia.VideoTranscoder(media) : null;
			atrans = new FFMedia.AudioTranscoder(media);

			//	vtrans.TakeFrame();

			var aFrame = new FFMedia.AudioFrame();
			while (atrans.TakeFrame(ref aFrame))
			{
				if (aFrame.data_size > 0)
				{
					await alStream.WriteDataAsync(ALDevice.SourceFormat.STEREO_16, aFrame.data, aFrame.data_size, aFrame.sample_rate);
				}
				else
				{
					await Task.Delay(100);
				}
			}
		}

		public void Dispose()
		{
			alStream.Dispose();
			alDev.Dispose();
			vtrans?.Dispose();
			atrans.Dispose();
			media.Dispose();
		}

	}
}

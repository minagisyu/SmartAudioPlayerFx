using SmartAudioPlayer.MediaProcessor.Audio;
using System;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	public sealed class MediaPlayback : IDisposable
	{
		FFMedia media;
		FFMedia.VideoTranscoder vtrans;
		FFMedia.AudioTranscoder atrans;
		ALStreamingSource alStream;

		public async Task PlayAsync(string file, bool enableVideo = false)
		{
			media = new FFMedia(file);
			vtrans = enableVideo ? new FFMedia.VideoTranscoder(media) : null;
			atrans = new FFMedia.AudioTranscoder(media);
			alStream = new ALStreamingSource(80);

		//	media.reader.Seek(30.0);
			media.reader.StartFill();

			//	vtrans.TakeFrame();

			var aFrame = new FFMedia.AudioFrame();
			while (atrans.TakeFrame(ref aFrame))
			{
				if (aFrame.data_size > 0)
				{
					await alStream.WriteDataAsync(atrans.dstALFormat, aFrame.data, aFrame.data_size, aFrame.sample_rate);
				}
				else
				{
					await Task.Delay(100);
				}
			}
		}

		public void Dispose()
		{
			alStream?.Dispose();
			vtrans?.Dispose();
			atrans?.Dispose();
			media?.Dispose();
		}
	}
}

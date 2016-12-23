using SmartAudioPlayer.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
			alStream = alDev.CreateStreamingSource(3);
		}

		public async Task PlayAsync(bool enableVideo = false)
		{
			string file = @"V:\__Video(old)\1995-2010\_etc\AT-X ロゴ (AT-X 640x480_x264 [2009Q3]).mp4";
			file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x720p Hi10P).mp4";
			//	file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x1080i Hi10P).mp4";

			media = new FFMedia(file);
			vtrans = enableVideo ? new FFMedia.VideoTranscoder(media) : null;
			atrans = new FFMedia.AudioTranscoder(media);

			//	vtrans.TakeFrame();

			var aFrame = new FFMedia.AudioFrame();
			using (var writer = File.OpenWrite("decout.pcm"))
			{
					byte[] d = new byte[192000];
				while (atrans.TakeFrame(ref aFrame))
				{
					System.Runtime.InteropServices.Marshal.Copy(aFrame.data, d, 0, aFrame.data_size);
					writer.Write(d, 0, aFrame.data_size);
					await alStream.WriteDataAsync(ALDevice.SourceFormat.STEREO_16, aFrame.data, aFrame.data_size, aFrame.sample_rate);
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

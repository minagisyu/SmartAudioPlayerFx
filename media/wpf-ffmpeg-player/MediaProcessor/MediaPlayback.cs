using SmartAudioPlayer.MediaProcessor.Audio;
using System;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	public sealed class MediaPlayback : IDisposable
	{
		FFMedia media;
		ALStreamingSource alStream;

		public async Task PlayAsync(string file, bool enableVideo = false)
		{
			media = new FFMedia(file);
			alStream = new ALStreamingSource(8);

			//	media.packet_reader.Seek(80.0);

			var aTask = Task.Factory.StartNew(async () =>
			{
				var aFrame = new FFMedia.AudioFrame();
				while (media.audio_dec.TakeFrame(media.packet_reader, ref aFrame))
				{
					if (aFrame.data_size > 0)
					{
						await alStream.WriteDataAsync(media.audio_dec.dstALFormat, aFrame.data, aFrame.data_size, aFrame.sample_rate);
					}
					else
					{
						await Task.Delay(100);
					}
				}
			});

			var vFrame = new FFMedia.VideoFrame();
			var window = new System.Windows.Window();
			window.Loaded += delegate
			{
				var image = new System.Windows.Media.Imaging.WriteableBitmap(1920, 1080, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);
				window.Content = new System.Windows.Controls.Image() { Source = image };
				Task.Factory.StartNew(() =>
				{
					while (media.video_dec.TakeFrame(media.packet_reader, ref vFrame))
					{
						if (vFrame.stride > 0)
						{
							window.Dispatcher.Invoke(new Action(() =>
							{
								image.WritePixels(new System.Windows.Int32Rect(0, 0, vFrame.width, vFrame.height), vFrame.data, vFrame.data_size, vFrame.stride);
							}));
							Task.Delay(20).Wait();
						}
						else
						{
							Task.Delay(100).Wait();
						}
					}
				});
			};

			new System.Windows.Application().Run(window);
		}

		public void Dispose()
		{
			alStream?.Dispose();
			media?.Dispose();
		}
	}
}

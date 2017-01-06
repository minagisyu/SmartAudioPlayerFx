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
			media = new FFMedia(file, false);
			alStream = new ALStreamingSource(8);


			var t1 = Task.Factory.StartNew(() =>
			{
				Task.Delay(10000).Wait();
				media.packet_reader.SetVideoReadSkip(true);
			}).ContinueWith(t=>
			{
				Task.Delay(10000).Wait();
				media.packet_reader.SetVideoReadSkip(false);
			}).ContinueWith(t=>
			{
				Task.Delay(10000).Wait();
				media.packet_reader.Seek(50.0);
			});

			var aTask = Task.Factory.StartNew(async () =>
			{
				var aFrame = new AudioFrame();
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

			var vFrame = new VideoFrame();
			var window = new System.Windows.Window();
			window.Loaded += delegate
			{
				System.Windows.Media.Imaging.WriteableBitmap image = null;

				Task.Factory.StartNew(() =>
				{
					if (media.video_dec == null) return;
					while (media.video_dec.TakeFrame(media.packet_reader, ref vFrame))
					{
						if (vFrame.stride > 0)
						{
							window.Dispatcher.Invoke(new Action(() =>
							{
								if(image == null)
								{
									image = new System.Windows.Media.Imaging.WriteableBitmap(vFrame.width, vFrame.height, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);
									window.Content = new System.Windows.Controls.Image() { Source = image };
								}
								image.WritePixels(new System.Windows.Int32Rect(0, 0, vFrame.width, vFrame.height), vFrame.data, vFrame.data_size, vFrame.stride);
							}));
							Task.Delay(14).Wait();
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

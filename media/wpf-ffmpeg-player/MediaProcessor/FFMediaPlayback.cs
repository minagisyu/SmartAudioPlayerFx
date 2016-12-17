using System;
using System.Collections.Generic;
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


		public void PlayAsync()
		{
			string file = @"V:\__Video(old)\1995-2010\_etc\AT-X ロゴ (AT-X 640x480_x264 [2009Q3]).mp4";
			file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x720p Hi10P).mp4";
			//	file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x1080i Hi10P).mp4";

			media = new FFMedia(file);
			vtrans = new FFMedia.VideoTranscoder(media);
			atrans = new FFMedia.AudioTranscoder(media);
		}

		public void Dispose()
		{
			vtrans.Dispose();
			atrans.Dispose();
			media.Dispose();
		}


	}
}

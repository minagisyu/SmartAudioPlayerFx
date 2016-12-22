using SmartAudioPlayer.MediaProcessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPFx.WPF
{
	class TestMain
	{
		[STAThread]
		static void Main()
		{
			string file = @"V:\__Video(old)\1995-2010\_etc\AT-X ロゴ (AT-X 640x480_x264 [2009Q3]).mp4";
			file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x720p Hi10P).mp4";
			//	file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x1080i Hi10P).mp4";

			var pb = new FFMediaPlayback();
			pb.PlayAsync();

		}
	}
}

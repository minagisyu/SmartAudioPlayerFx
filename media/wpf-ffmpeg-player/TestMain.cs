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
			string file =
				// @"V:\__Video(old)\1995-2010\_etc\AT-X ロゴ (AT-X 640x480_x264 [2009Q3]).mp4";
				// @"V:\bb-test\ブラック・ブレット ED (BS11 1280x720p Hi10P).mp4";
				// @"V:\bb-test\ブラック・ブレット ED (BS11 1280x1080i Hi10P).mp4";
				@"E:\Music\SAP用\[2013Q1]\クルトヒュムネス~神と対話した詩~\1-7 EXEC_HYMME_LIFE_W：R：S／．.AC3";

			var pb = new FFMediaPlayback();
			pb.PlayAsync(file).Wait();

		}
	}
}

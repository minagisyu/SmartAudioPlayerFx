using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vlc.DotNet.Core;
using static System.Console;

namespace ConsoleApplication1
{
	class Program
	{
		static void Main(string[] args)
		{
			var file = @"V:\bb-test\ブラック・ブレット ED (BS11 1280x1080i Hi10P).mp4";


			var libPath = new DirectoryInfo(@"..\x86");
			var options = new[] { "", "--vout-filter=deinterlace", "--deinterlace-mode=blend" };
			WriteLine($"Initialize libVLC");
			WriteLine($" > libPath: {libPath}");
			WriteLine($" > options: {string.Concat(options.Select(x => x + " "))}");

			var player = new VlcMediaPlayer(libPath, options);
			player.Backward += delegate { WriteLine($"player.Backward"); };
			player.Buffering += delegate { WriteLine($"player.Buffering"); };
			player.EncounteredError += delegate { WriteLine($"player.EncounteredError"); };
			player.EndReached += delegate { WriteLine($"player.EndReached"); };
			player.Forward += delegate { WriteLine($"player.Forward"); };
			player.LengthChanged += delegate { WriteLine($"player.LengthChanged"); };
			player.MediaChanged += delegate { WriteLine($"player.MediaChanged"); };
			player.Opening += delegate { WriteLine($"player.Opening"); };
			player.PausableChanged += delegate { WriteLine($"player.PausableChanged"); };
			player.Paused += delegate { WriteLine($"player.Paused"); };
			player.Playing += delegate { WriteLine($"player.Playing"); };
		//	player.PositionChanged += delegate { WriteLine($"player.PositionChanged"); };
			player.ScrambledChanged += delegate { WriteLine($"player.ScrambledChanged"); };
			player.SeekableChanged += delegate { WriteLine($"player.SeekableChanged"); };
			player.SnapshotTaken += delegate { WriteLine($"player.SnapshotTaken"); };
			player.Stopped += delegate { WriteLine($"player.Stopped"); };
		//	player.TimeChanged += delegate { WriteLine($"player.TimeChanged"); };
			player.TitleChanged += delegate { WriteLine($"player.TitleChanged"); };
			player.VideoOutChanged += delegate { WriteLine($"player.VideoOutChanged"); };

			WriteLine($"OpenMedia");
			VlcMedia media = null;
			media = player.SetMedia(new FileInfo(file));
			media.ParsedChanged += (_, __) =>
			{
				WriteLine($" > Album: {media?.Album}");
				WriteLine($" > Artist: {media?.Artist}");
				WriteLine($" > ArtworkURL: {media?.ArtworkURL}");
				WriteLine($" > Copyright: {media?.Copyright}");
				WriteLine($" > Date: {media?.Date}");
				WriteLine($" > Description: {media?.Description}");
				WriteLine($" > Duration: {media?.Duration}");
				WriteLine($" > EncodedBy: {media?.EncodedBy}");
				WriteLine($" > Genre: {media?.Genre}");
				WriteLine($" > Language: {media?.Language}");
				WriteLine($" > Mrl: {media?.Mrl}");
				WriteLine($" > NowPlaying: {media?.NowPlaying}");
				WriteLine($" > Publisher: {media?.Publisher}");
				WriteLine($" > Rating: {media?.Rating}");
				WriteLine($" > Setting: {media?.Setting}");
				WriteLine($" > State: {media?.State}");
				WriteLine($" > Statistics: {media?.Statistics}");
				WriteLine($" > Title: {media?.Title}");
				WriteLine($" > TrackID: {media?.TrackID}");
				WriteLine($" > TrackNumber: {media?.TrackNumber}");
				WriteLine($" > TracksInformations: {media?.TracksInformations}");
				WriteLine($" > URL: {media?.URL}");
			};
			media.ParseAsync();
			media.Dispose();


			WriteLine($"PlayMedia");
			player.Play();
		//	player.Stop();

			WriteLine($"[ENTER] to exit.");
			ReadLine();

			WriteLine($"StopMedia");
			player.Stop();

			WriteLine($"Dispose");
			player.Dispose();

		}
	}
}

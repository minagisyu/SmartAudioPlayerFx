using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace SmartAudioPlayerFx.Managers
{
	[TestFixture]
	class AudioPlayerManager_Test
	{
		readonly string GOODFILE = new FileInfo(@"_testdata\TestArtist - TestAlbum\01 music1.mp3").FullName;
		readonly string BADFILE = new FileInfo(@"_testdata\TestArtist - TestAlbum\00 notfound.file.mp3").FullName;

		[Test]
		public void InitValue()
		{
			using (var manager = new AudioPlayerManager())
			{
				manager.CurrentOpenedPath.IsNull();
				manager.Duration.IsNull();
				manager.IsPaused.Is(false);
				manager.Position.Is(TimeSpan.Zero);
				manager.Volume.Is(0.5);
				manager.Close();
			}
		}

		[Test]
		public void OtherThreadDispose()
		{
			using (var manager = new AudioPlayerManager())
			{
				Task.Factory.StartNew(() =>
				{
					manager.Dispose();
				}).Wait();
			}
		}

		[Test]
		public void EventTest()
		{
			using(var manager = new AudioPlayerManager())
			{
				bool opened_event = false, playended_event = false;
				string playended_event_reason;
				manager.Opened += delegate { opened_event = true; };
				manager.PlayEnded += e => { playended_event = true; playended_event_reason = e.ErrorReason; };
				manager.PlayFrom(GOODFILE, false, null, delegate
				{
					manager.CurrentOpenedPath.Is(GOODFILE);
					manager.Duration.Is(v => v.HasValue && v.Value != TimeSpan.Zero);
					manager.IsPaused.Is(false);
					manager.Position = manager.Duration.Value;
				});
				while (opened_event == false || playended_event == false)
				{
					System.Windows.Forms.Application.DoEvents();
				}


				bool ispaused_event = false, volume_event = false;
				manager.IsPausedChanged += delegate { ispaused_event = true; };
				manager.VolumeChanged += delegate { volume_event = true; };
				manager.PlayPause();
				manager.Volume = 0.1;
				while (ispaused_event == false || volume_event == false)
				{
					System.Windows.Forms.Application.DoEvents();
				}

			}
		}


	}
}

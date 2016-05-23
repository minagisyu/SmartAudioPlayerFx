using NUnit.Framework;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.Managers
{
	[TestFixture]
	class MediaItemFilterManager_Test
	{
		const string TESTDB = @"test.db";

		[Test]
		public void MediaItemFilterTest()
		{
			try
			{
				ManagerServices.Initialize(TESTDB);

				var good_item = new MediaItem()
				{
					FilePath = "testfile.mp3",
					Title = "GoodTitle",
					Artist = "GoodArtist",
					Album = "GoodAlbum",
					Comment = "GoodComment",
				};
				good_item.UpdateSearchHint();
				var bad_item = new MediaItem()
				{
					FilePath = "testfile.mp6",
					Title = "BadTitle",
					Artist = "BadArtist",
					Album = "BadAlbum",
					Comment = "BadComment",
				};
				bad_item.UpdateSearchHint();

				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#01");	// 初期設定で.mp3なら通るはず
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(false, "#02");	// 初期設定で.mp6は通らない

				ManagerServices.MediaItemFilterManager.SetAcceptExtensions(
					new MediaItemFilterManager.AcceptExtension(true, ".mp3"),
					new MediaItemFilterManager.AcceptExtension(true, ".mp6"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#03");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(true, "#04");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "GoodTitle"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(false, "#05");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(true, "#06");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "GoodArtist"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(false, "#07");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(true, "#08");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "GoodAlbum"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(false, "#09");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(true, "#10");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "GoodComment"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(false, "#11");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(true, "#12");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "BadTitle"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#13");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(false, "#14");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "BadArtist"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#15");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(false, "#16");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "BadAlbum"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#17");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(false, "#18");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "BadComment"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#19");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(false, "#20");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "Bad"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#21");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(false, "#22");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(false, "Bad"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(true, "#23");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(true, "#24");

				ManagerServices.MediaItemFilterManager.SetAcceptExtensions(
					new MediaItemFilterManager.AcceptExtension(false, ".mp3"),
					new MediaItemFilterManager.AcceptExtension(true, ".mp6"));
				ManagerServices.MediaItemFilterManager.Validate(good_item).Is(false, "#25");
				ManagerServices.MediaItemFilterManager.Validate(bad_item).Is(true, "#26");
			}
			finally { ManagerServices.Dispose(); }
		}

	}
}

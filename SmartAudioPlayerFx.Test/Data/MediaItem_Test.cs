using System;
using System.IO;
using NUnit.Framework;

namespace SmartAudioPlayerFx.Data
{
	[TestFixture]
	class MediaItem_Test
	{
		const string TESTDIR = @"_testdata\TestArtist - TestAlbum";
		const string TESTFILE = TESTDIR + @"\01 music1.mp3";

		[Test]
		public void CreateDefault_ValueTest()
		{
			var fi = new FileInfo(TESTFILE);
			var item = new MediaItem(TESTFILE);
			item.Album.Is(string.Empty, "Album != Empty");
			item.Artist.Is(string.Empty, "Artist != Empty");
			item.Comment.Is(string.Empty, "Comment != Empty");
			item.CreatedDate.Is(fi.CreationTimeUtc.Ticks, "CreatedDate != File");
			item.FilePath.Is(fi.FullName, "FilePath != File");
			item.ID.Is(0, "ID != 0");
			item.IsFavorite.Is(false, "IsFavorite != false");
			item.IsNotExist.Is(false, "IsNotExist != false");
			item.LastPlay.Is(DateTime.MinValue.Ticks, "LastPlay != MinValue");
			item.LastUpdate.Is(v => v != 0, "LastUpdate == 0");
			item.LastWrite.Is(fi.LastWriteTimeUtc.Ticks, "LastWrite != File");
			item.PlayCount.Is(0, "PlayCount != 0");
			item.SelectCount.Is(0, "SelectCount != 0");
			item.SkipCount.Is(0, "SkipCount != 0");
			item.Title.Is(fi.Name, "Title != FileName");

			// SearchHintは"filepath","title","artist","album","comment"を
			// "半角","小文字","カナ","に変換してMediaItem.SearchHintSplitCharで連結
			item = new MediaItem()
			{
				FilePath = "FILEPATH",
				Title = "ＴＩＴＬＥ",
				Artist = "アーティスト",
				Album = "あるばむ",
				Comment = "１２３",
			};
			item.UpdateSearchHint();
			var sp = MediaItemExtension.SearchHintSplitChar;
			var validHintValue =
				"filepath" + sp +
				"title" + sp +
				"ｱｰﾃｨｽﾄ" + sp +
				"ｱﾙﾊﾞﾑ" + sp +
				"123";

			item.SearchHint.Is(validHintValue, "SearchHint != validHintValue");
		}

		[Test]
		public void CopyTest()
		{
			var item = new MediaItem()
			{
				_play_error_reason = "error",
				Album = "album",
				Artist = "artist",
				Comment = "comment",
				CreatedDate = 12,
				FilePath = "filepath",
				ID = 34,
				IsFavorite = true,
				IsNotExist = true,
				LastPlay = 56,
				LastUpdate = 78,
				LastWrite = 90,
				PlayCount = 2,
				SearchHint = "hint",
				SelectCount = 4,
				SkipCount = 6,
				Title = "title",
			};
			var copyedItem = new MediaItem();
			item.CopyTo(copyedItem);

			copyedItem._play_error_reason.Is("error");
			copyedItem.Album.Is("album");
			copyedItem.Artist.Is("artist");
			copyedItem.Comment.Is("comment");
			copyedItem.CreatedDate.Is(12);
			copyedItem.FilePath.Is("filepath");
			copyedItem.ID.Is(34);
			copyedItem.IsFavorite.Is(true);
			copyedItem.IsNotExist.Is(true);
			copyedItem.LastPlay.Is(56);
			copyedItem.LastUpdate.Is(78);
			copyedItem.LastWrite.Is(90);
			copyedItem.PlayCount.Is(2);
			copyedItem.SearchHint.Is("hint");
			copyedItem.SelectCount.Is(4);
			copyedItem.SkipCount.Is(6);
			copyedItem.Title.Is("title");
		}

	}
}

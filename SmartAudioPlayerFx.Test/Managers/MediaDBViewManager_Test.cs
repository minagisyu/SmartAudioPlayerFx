using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.Managers
{
	[TestFixture]
	class MediaDBViewManager_Test
	{
		const string TESTDB = @"test.db";
		const string TESTDIR = @"_testdata";
		const string TESTFILE = TESTDIR + @"\TestArtist - TestAlbum\01 music1.mp3";

		[Test]
		public void Get_Update_Test()
		{
			try
			{
				Program.SafeDelete(TESTDB);
				ManagerServices.Initialize(TESTDB);

				string file;
				MediaItem item2;

				// エラーが返るか？
				var notfound_item = ManagerServices.MediaDBViewManager.GetOrCreate("d:\\test\\notfound.file");
				notfound_item.IsNull(); // ファイル=ない/DB=ない のアイテムがGetOrCreateで取得出来てしまった

				// 新規に作成されるか？
				File.Exists(TESTFILE).Is(true, "テスト対象のファイルがない(" + TESTFILE + ")");
				file = new FileInfo(TESTFILE).FullName;
				var item = ManagerServices.MediaDBViewManager.GetOrCreate(file);
				item.IsNotNull(); // ファイル=ある/DB=ない のアイテムがGetOrCreateで取得できない

				// 一度登録された「存在しない」ファイルをDBから取得出来るか？
				var testfile0 = file + ".test.mp3";
				File.Copy(file, testfile0, true);
				var erasing_item = ManagerServices.MediaDBViewManager.GetOrCreate(testfile0);
				ManagerServices.MediaDBViewManager.WaitForAsyncRaiging();
				Program.SafeDelete(testfile0);
				File.Exists(testfile0).Is(false, "削除したファイルが残っている(" + testfile0 + ")");

				erasing_item = ManagerServices.MediaDBViewManager.GetOrCreate(testfile0);
				ManagerServices.MediaDBViewManager.WaitForAsyncRaiging();
				erasing_item.IsNotNull(); // ファイル=ない/DB=ある のアイテムがGetOrCreateで取得できない
				erasing_item.IsNotExist.Is(true, "存在しないファイルを取得したのにDB上は存在していることになっている");

				// 更新結果がうまく取れるか
				item.LastPlay = 12345;
				item.LastUpdate = DateTime.UtcNow.Ticks;
				ManagerServices.MediaDBViewManager.RaiseDBUpdateAsync(item, _ => _.LastPlay, _ => _.LastUpdate);
				ManagerServices.MediaDBViewManager.WaitForAsyncRaiging();
				item2 = ManagerServices.MediaDBViewManager.GetOrCreate(file);
				item2.LastPlay.Is(12345, "RaiseUpdateに失敗？");

				// Itemsをクリアして、DBから取得する結果がちゃんとしているか確認
				ManagerServices.MediaDBViewManager.Items.Clear();
				var item3 = ManagerServices.MediaDBViewManager.GetOrCreate(file);
				item3.LastUpdate.Is(item2.LastUpdate, "DB再オープン後に値が変化？(LastUpdate)");
				item3.LastPlay.Is(item2.LastPlay, "DB再オープン後に値が変化？(LastPlay)");
			}
			finally
			{
				ManagerServices.Dispose();
			}
		}

		[Test]
		public void FocusPath_ReValidate_Test()
		{
			try
			{
				Program.SafeDelete(TESTDB);
				ManagerServices.Initialize(TESTDB);

				var corescan_finished_ev = new AutoResetEvent(false);
				ManagerServices.MediaDBViewManager.ItemCollect_CoreScanFinished += delegate { corescan_finished_ev.Set(); };

				var dir = new DirectoryInfo(TESTDIR).FullName;
				ManagerServices.MediaDBViewManager.FocusPath.Value = dir;
				ManagerServices.MediaDBViewManager.FocusPath.Value.Is(dir, "FocusPathが変化していない");
				ManagerServices.MediaDBViewManager.Items.Is(x => x != null, "Itemsがセットされていない？");
				corescan_finished_ev.WaitOne();	// 検索終わるまで待つ？
				ManagerServices.MediaDBViewManager.Items.Count.Is(8, "収集されたアイテム数がおかしい (Items.Count=" + ManagerServices.MediaDBViewManager.Items.Count + ")");

				ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "inst"));
				corescan_finished_ev.WaitOne(); // 再検証が終わるまで待つ
				ManagerServices.MediaDBViewManager.Items.Count.Is(5, "フィルタ後のアイテム数がおかしい (Items.Count=" + ManagerServices.MediaDBViewManager.Items.Count + ")");
			}
			finally
			{
				ManagerServices.Dispose();
			}
		}
	}
}

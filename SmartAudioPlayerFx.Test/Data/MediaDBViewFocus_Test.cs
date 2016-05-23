using System.IO;
using System.Threading;
using NUnit.Framework;
using SmartAudioPlayerFx.Managers;

namespace SmartAudioPlayerFx.Data
{
	[TestFixture]
	class MediaDBViewFocus_Test
	{
		const string TESTDB = @"test.db";
		const string TESTDIR = @"_testdata";
		const string TESTDIR2 = TESTDIR + @"\TestArtist - TestAlbum2";

		[Test]
		public void FocusPath_ReValidate_Test()
		{
			try
			{
				Program.SafeDelete(TESTDB);
				ManagerServices.Initialize(TESTDB);

				var corescan_finished_ev = new AutoResetEvent(false);
				ManagerServices.MediaDBViewManager.ItemCollect_CoreScanFinished += delegate { corescan_finished_ev.Set(); };

				ManagerServices.MediaItemFilterManager.Reset();
				corescan_finished_ev.WaitOne();

				var dir = new DirectoryInfo(TESTDIR).FullName;
				ManagerServices.MediaDBViewManager.FocusPath.Value = dir;
				corescan_finished_ev.WaitOne();

				using (var viewfocus = new MediaDBViewFocus(dir))
				{
					viewfocus.FocusPath.Is(dir);
					viewfocus.Items.Count.Is(8, "収集されたアイテム数がおかしい (viewfocus.Items.Count=" + viewfocus.Items.Count + ")");
				}

				dir = new DirectoryInfo(TESTDIR2).FullName;
				using (var viewfocus = new MediaDBViewFocus(dir))
				{
					viewfocus.Items.Count.Is(2, "収集されたアイテム数がおかしい(2) (viewfocus.Items.Count=" + viewfocus.Items.Count + ")");

					ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "02"));
					corescan_finished_ev.WaitOne();
					viewfocus.Items.Count.Is(1, "フィルタ後のアイテム数がおかしい (Items.Count=" + viewfocus.Items.Count + ")");
				}
			}
			finally
			{
				ManagerServices.Dispose();
			}
		}

	}
}

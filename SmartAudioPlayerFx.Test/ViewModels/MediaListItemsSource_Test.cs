using System.IO;
using System.Threading;
using Codeplex.Reactive;
using NUnit.Framework;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;

namespace SmartAudioPlayerFx.Views
{
	[TestFixture]
	class MediaListItemsSource_Test
	{
		const string TESTDB = @"test.db";
		const string TESTDIR = @"_testdata";

		[Test]
		public void ListFocus_ListItems_Test()
		{
			try
			{
				UIDispatcherScheduler.Initialize();
				ManagerServices.Initialize(TESTDB);

				var corescan_finished_ev = new AutoResetEvent(false);
				ManagerServices.MediaDBViewManager.ItemCollect_CoreScanFinished += delegate { corescan_finished_ev.Set(); };

				var dir = new DirectoryInfo(TESTDIR).FullName;
				ManagerServices.MediaDBViewManager.FocusPath.Value = dir;
				corescan_finished_ev.WaitOne();

				// ルート
				using (var viewFocus = new MediaDBViewFocus(dir))
				using (var source = new MediaListItemsSource(viewFocus))
				{
					source.Items.IsNotNull();
					source.Items.Count.Is(12);
				}

				// 不要なサブフォルダがあるパターン
				using (var viewFocus = new MediaDBViewFocus(dir + @"\TestArtist - TestAlbum"))
				using (var source = new MediaListItemsSource(viewFocus))
				{
					source.Items.Count.Is(5);
				}

				// ファイルだけのパターン
				using (var viewFocus = new MediaDBViewFocus(dir + @"\TestArtist - TestAlbum2"))
				using (var source = new MediaListItemsSource(viewFocus))
				{
					source.Items.Count.Is(3);
				}

				// サブフォルダx2のパターン
				using (var viewFocus = new MediaDBViewFocus(dir + @"\TestFolder"))
				using (var source = new MediaListItemsSource(viewFocus))
				{
					source.Items.Count.Is(2);
				}
			}
			finally
			{
				ManagerServices.Dispose();
			}
		}

	}
}

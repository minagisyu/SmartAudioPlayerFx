using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Codeplex.Reactive;
using NUnit.Framework;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;

namespace SmartAudioPlayerFx.Views
{
	[TestFixture]
	[FocusTestFixture]
	class MediaTreeItemViewModel_Test
	{
		const string TESTDB = @"test.db";
		const string TESTDIR = @"_testdata";
		const string TESTFILE = TESTDIR + @"\TestArtist - TestAlbum\01 music1.mp3";

		[Test]
		public void TreeItems_Test()
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

				var uithread_idle = new Action(() =>
				{
					UIDispatcherScheduler.Default.Dispatcher
						.Invoke(new Action(() => { }), DispatcherPriority.SystemIdle);
				});

				ManagerServices.JukeboxManager.ViewFocus.Value = new MediaDBViewFocus(dir);
				using (var treeVM = new MediaTreeItem_DefaultItemsViewModel(dir, 0))
				{
					treeVM.BasePath.Is(dir);
					treeVM.Name.Is(Path.GetFileName(dir));
					treeVM.SubItems.Count.Is(3);
					treeVM.SubItems[0].Name.Is("TestArtist - TestAlbum");
					treeVM.SubItems[0].SubItems.Count.Is(0);
					treeVM.SubItems[1].Name.Is("TestArtist - TestAlbum2");
					treeVM.SubItems[1].SubItems.Count.Is(0);
					treeVM.SubItems[2].Name.Is("TestFolder");
					treeVM.SubItems[2].SubItems.Count.Is(1);
					treeVM.SubItems[2].SubItems[0].Name.Is("TestSubFolder");
					treeVM.SubItems[2].SubItems[0].SubItems.Count.Is(0);
					
					// ツリー全消失パターン
					var exts = ManagerServices.MediaItemFilterManager.AcceptExtensions.ToList();
					exts.RemoveAll(x => x.Extension == ".mp3");
					exts.Add(new MediaItemFilterManager.AcceptExtension(false, ".mp3"));
					corescan_finished_ev.Reset();
					ManagerServices.MediaItemFilterManager.SetAcceptExtensions(exts.ToArray());
					corescan_finished_ev.WaitOne();
					uithread_idle();
					treeVM.SubItems.Count.Is(0);

					// ツリー復活パターン
					exts = ManagerServices.MediaItemFilterManager.AcceptExtensions.ToList();
					exts.RemoveAll(x => x.Extension == ".mp3");
					exts.Add(new MediaItemFilterManager.AcceptExtension(true, ".mp3"));
					corescan_finished_ev.Reset();
					ManagerServices.MediaItemFilterManager.SetAcceptExtensions(exts.ToArray());
					corescan_finished_ev.WaitOne();
					uithread_idle();
					uithread_idle();
					treeVM.SubItems.Count.Is(3);
					treeVM.SubItems[0].Name.Is("TestArtist - TestAlbum");
					treeVM.SubItems[0].SubItems.Count.Is(0);
					treeVM.SubItems[1].Name.Is("TestArtist - TestAlbum2");
					treeVM.SubItems[1].SubItems.Count.Is(0);
					treeVM.SubItems[2].Name.Is("TestFolder");
					treeVM.SubItems[2].SubItems.Count.Is(1);
					treeVM.SubItems[2].SubItems[0].Name.Is("TestSubFolder");
					treeVM.SubItems[2].SubItems[0].SubItems.Count.Is(0);

					// ツリーが増えるパターン1
					exts = ManagerServices.MediaItemFilterManager.AcceptExtensions.ToList();
					exts.Add(new MediaItemFilterManager.AcceptExtension(true, ".jpg"));
					corescan_finished_ev.Reset();
					ManagerServices.MediaItemFilterManager.SetAcceptExtensions(exts.ToArray());
					corescan_finished_ev.WaitOne();
					uithread_idle();
					treeVM.SubItems.Count.Is(3);
					treeVM.SubItems[0].Name.Is("TestArtist - TestAlbum");
					treeVM.SubItems[0].SubItems.Count.Is(1);
					treeVM.SubItems[0].SubItems[0].Name.Is("image");
					treeVM.SubItems[0].SubItems[0].SubItems.Count.Is(0);
					treeVM.SubItems[1].Name.Is("TestArtist - TestAlbum2");
					treeVM.SubItems[1].SubItems.Count.Is(0);
					treeVM.SubItems[2].Name.Is("TestFolder");
					treeVM.SubItems[2].SubItems.Count.Is(1);
					treeVM.SubItems[2].SubItems[0].Name.Is("TestSubFolder");
					treeVM.SubItems[2].SubItems[0].SubItems.Count.Is(0);

					// ツリーが減るパターン1
					exts = ManagerServices.MediaItemFilterManager.AcceptExtensions.ToList();
					exts.RemoveAll(x => x.Extension == ".jpg");
					exts.Add(new MediaItemFilterManager.AcceptExtension(false, ".jpg"));
					corescan_finished_ev.Reset();
					ManagerServices.MediaItemFilterManager.SetAcceptExtensions(exts.ToArray());
					corescan_finished_ev.WaitOne();
					uithread_idle();
					treeVM.SubItems.Count.Is(3);
					treeVM.SubItems[0].Name.Is("TestArtist - TestAlbum");
					treeVM.SubItems[0].SubItems.Count.Is(0);

					// ツリーが減るパターン2
					corescan_finished_ev.Reset();
					ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, "music2"));
					corescan_finished_ev.WaitOne();
					uithread_idle();
					treeVM.SubItems.Count.Is(2);
					treeVM.SubItems[0].Name.Is("TestArtist - TestAlbum");
					treeVM.SubItems[0].SubItems.Count.Is(0);
					treeVM.SubItems[1].Name.Is("TestArtist - TestAlbum2");
					treeVM.SubItems[1].SubItems.Count.Is(0);

					// ツリーが減るパターン3
					corescan_finished_ev.Reset();
					ManagerServices.MediaItemFilterManager.SetIgnoreWords(new MediaItemFilterManager.IgnoreWord(true, ".mp3"));
					corescan_finished_ev.WaitOne();
					uithread_idle();
					treeVM.SubItems.Count.Is(0);

					// ツリーが増えるパターン2
					exts = ManagerServices.MediaItemFilterManager.AcceptExtensions.ToList();
					exts.RemoveAll(x => x.Extension == ".jpg");
					exts.Add(new MediaItemFilterManager.AcceptExtension(true, ".jpg"));
					corescan_finished_ev.Reset();
					ManagerServices.MediaItemFilterManager.SetAcceptExtensions(exts.ToArray());
					corescan_finished_ev.WaitOne();
					uithread_idle();
					treeVM.SubItems.Count.Is(1);
					treeVM.SubItems[0].Name.Is("TestArtist - TestAlbum");
					treeVM.SubItems[0].SubItems.Count.Is(1);
					treeVM.SubItems[0].SubItems[0].Name.Is("image");
					treeVM.SubItems[0].SubItems[0].SubItems.Count.Is(0);

					// ViewFocusPath = nullで消えるパターン
					ManagerServices.JukeboxManager.ViewFocus.Value = new MediaDBViewFocus(null);
					uithread_idle();
					treeVM.SubItems.Count.Is(0);

					// ViewFocusPathで追加するパターン
					ManagerServices.JukeboxManager.ViewFocus.Value = new MediaDBViewFocus(dir);
					uithread_idle();
					treeVM.SubItems.Count.Is(1);

					// FocusPathで消えるパターン
					ManagerServices.MediaDBViewManager.FocusPath.Value = null;
					uithread_idle();
					treeVM.SubItems.Count.Is(0);

					// FocusPathで復活パターン
					corescan_finished_ev.Reset();
					ManagerServices.MediaDBViewManager.FocusPath.Value = dir;
					corescan_finished_ev.WaitOne();
					uithread_idle();
					treeVM.SubItems.Count.Is(1);

				}
			}
			finally
			{
				ManagerServices.Dispose();
			}
		}

	}
}

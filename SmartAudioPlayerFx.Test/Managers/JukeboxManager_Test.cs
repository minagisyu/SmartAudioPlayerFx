using System.IO;
using System.Threading;
using Codeplex.Reactive;
using NUnit.Framework;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.Managers
{
	[TestFixture]
	class JukeboxManager_Test
	{
		const string TESTDB = @"test.db";
		const string TESTDIR = @"_testdata";
		const string TESTDIR2 = TESTDIR + @"\TestArtist - TestAlbum2";

		[Test]
		public void Creation_Dispose_Test()
		{
			try
			{
				UIDispatcherScheduler.Initialize();
				ManagerServices.Initialize(TESTDB);

				ManagerServices.JukeboxManager.ViewFocus.IsNotNull();
				ManagerServices.JukeboxManager.IsServiceStarted.IsNotNull();
				ManagerServices.JukeboxManager.CurrentMedia.IsNotNull();
				ManagerServices.JukeboxManager.IsRepeat.IsNotNull();
				ManagerServices.JukeboxManager.SelectMode.IsNotNull();
			}
			finally
			{
				ManagerServices.Dispose();
			}
		}

	}
}

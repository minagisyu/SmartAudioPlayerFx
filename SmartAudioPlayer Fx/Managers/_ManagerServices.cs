using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using SmartAudioPlayer;

namespace SmartAudioPlayerFx.Managers
{
	static class ManagerServices
	{
		public static Preferences PreferencesManager { get; private set; }
		public static MediaDB MediaDBManager { get; private set; }
		public static AudioPlayerManager AudioPlayerManager { get; private set; }
		public static TaskIconManager TaskIconManager { get; private set; }
		public static AppUpdateManager AppUpdateManager { get; private set; }
		public static MediaItemFilter MediaItemFilterManager { get; private set; }
		public static MediaDBView MediaDBViewManager { get; private set; }
		public static RecentsManager RecentsManager { get; private set; }
		public static JukeboxManager JukeboxManager { get; private set; }
		public static ShortcutKeyManager ShortcutKeyManager { get; private set; }

		public static void Initialize(string dbFilename)
		{
			// Standalone
			PreferencesManager = new Preferences(isLoad: true);
			MediaDBManager = new MediaDB(dbFilename);

			// Standalone with UIThread
			AudioPlayerManager = new AudioPlayerManager();
			TaskIconManager = new TaskIconManager(
				"SmartAudioPlayer Fx",
				new Icon(App.GetResourceStream(new Uri("/Resources/SAPFx.ico", UriKind.Relative)).Stream));

			// require Preferences+TaskIcon
			AppUpdateManager = new AppUpdateManager();

			// require Preferences
			MediaItemFilterManager = new MediaItemFilter(PreferencesManager);
			// require Preferences+MediaDB+MediaItemFilter
			MediaDBViewManager = new MediaDBView(PreferencesManager, MediaDBManager, MediaItemFilterManager);
			// require Preferences+MediaDBView
			RecentsManager = new RecentsManager();

			// require Preferences+AudioPlayer+MediaDBView
			JukeboxManager = new JukeboxManager();
			// require Preferences+AudioPlayer+Jukebox
			ShortcutKeyManager = new ShortcutKeyManager();
		}

		public static void Dispose()
		{
			PreferencesManager.Dispose();
			MediaDBManager.Dispose();
			AudioPlayerManager.Dispose();
			TaskIconManager.Dispose();
			AppUpdateManager.Dispose();
			MediaItemFilterManager.Dispose();
			MediaDBViewManager.Dispose();
			RecentsManager.Dispose();
			JukeboxManager.Dispose();
			ShortcutKeyManager.Dispose();
		}

	}

}

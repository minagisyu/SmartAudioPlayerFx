using System.Reactive.Disposables;
using System.Threading.Tasks;
using SmartAudioPlayer;

namespace SmartAudioPlayerFx.Managers
{
	static class ManagerServices
	{
		public static PreferencesManager PreferencesManager { get; private set; }
		public static MediaDBManager MediaDBManager { get; private set; }
		public static AudioPlayerManager AudioPlayerManager { get; private set; }
		public static TaskIconManager TaskIconManager { get; private set; }
		public static AppUpdateManager AppUpdateManager { get; private set; }
		public static MediaItemFilterManager MediaItemFilterManager { get; private set; }
		public static MediaDBViewManager MediaDBViewManager { get; private set; }
		public static RecentsManager RecentsManager { get; private set; }
		public static JukeboxManager JukeboxManager { get; private set; }
		public static ShortcutKeyManager ShortcutKeyManager { get; private set; }

		public static void Initialize(string dbFilename)
		{
			// Standalone
			PreferencesManager = new PreferencesManager(isLoad: true);
			MediaDBManager = new MediaDBManager(dbFilename);

			// Standalone with UIThread
			AudioPlayerManager = new AudioPlayerManager();
			TaskIconManager = new TaskIconManager();

			// require Preferences+TaskIcon
			AppUpdateManager = new AppUpdateManager();

			// require Preferences
			MediaItemFilterManager = new MediaItemFilterManager();
			// require Preferences+MediaDB+MediaItemFilter
			MediaDBViewManager = new MediaDBViewManager();
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

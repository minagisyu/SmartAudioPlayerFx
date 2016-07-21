using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SmartAudioPlayerFx.Managers
{
	static class ManagerServices
	{
		// Standalone
		public static XmlPreferencesManager PreferencesManager { get; } = new XmlPreferencesManager();
	//	public static AudioPlayerManager AudioPlayerManager { get; } = new AudioPlayerManager();
		public static VlcAudioPlayerManager AudioPlayerManager { get; } = new VlcAudioPlayerManager();
		public static TaskIconManager TaskIconManager { get; } = new TaskIconManager();
		public static MediaDBManager MediaDBManager { get; } = new MediaDBManager();

		// require Preferences+TaskIcon
		public static AppUpdateManager AppUpdateManager { get; } = new AppUpdateManager();

		// require Preferences
		public static MediaItemFilterManager MediaItemFilterManager { get; } = new MediaItemFilterManager();
		// require Preferences+MediaDB+MediaItemFilter
		public static MediaDBViewManager MediaDBViewManager { get; } = new MediaDBViewManager();
		// require Preferences+MediaDBView
		public static RecentsManager RecentsManager { get; } = new RecentsManager();

		// require Preferences+AudioPlayer+MediaDBView
		public static JukeboxManager JukeboxManager { get; } = new JukeboxManager();
		// require Preferences+AudioPlayer+Jukebox
		public static ShortcutKeyManager ShortcutKeyManager { get; } = new ShortcutKeyManager();

		public static void Initialize()
		{
			new VlcAudioPlayerManager();
		}

		public static void Dispose()
		{
			// Preferences+AudioPlayer+Jukebox
			ShortcutKeyManager?.Dispose();
			// Preferences+AudioPlayer+MediaDBView
			JukeboxManager?.Dispose();

			// require Preferences+MediaDBView
			RecentsManager?.Dispose();
			// require Preferences+MediaDB+MediaItemFilter
			MediaDBViewManager?.Dispose();
			// require Preferences
			MediaItemFilterManager?.Dispose();

			// require Preferences+TaskIcon
			AppUpdateManager?.Dispose();

			// Standalones
			AudioPlayerManager?.Dispose();
			TaskIconManager?.Dispose();
			MediaDBManager?.Dispose();
		}

	}
}

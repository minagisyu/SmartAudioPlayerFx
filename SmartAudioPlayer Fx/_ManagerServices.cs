using System;
using SmartAudioPlayerFx.AppUpdate;
using SmartAudioPlayerFx.MediaDB;
using SmartAudioPlayerFx.MediaPlayer;
using SmartAudioPlayerFx.Notification;
using SmartAudioPlayerFx.Preferences;
using SmartAudioPlayerFx.Shortcut;

namespace SmartAudioPlayerFx
{
	[Obsolete]
	static class ManagerServices
	{
		// Standalone
	//	public static XmlPreferencesManager PreferencesManager { get; } = App.Models.Get<XmlPreferencesManager>();
	//	public static AudioPlayerManager AudioPlayerManager { get; } = new AudioPlayerManager();
	//	public static TaskIconManager TaskIconManager { get; } = App.Models.Get<TaskIconManager>();
	//	public static MediaDBManager MediaDBManager { get; } = App.Models.Get<MediaDBManager>();

		// require Preferences+TaskIcon
	//	public static AppUpdateManager AppUpdateManager { get; } = App.Models.Get<AppUpdateManager>();

		// require Preferences
	//	public static MediaItemFilterManager MediaItemFilterManager { get; } = App.Models.Get<MediaItemFilterManager>();
		// require Preferences+MediaDB+MediaItemFilter
	//	public static MediaDBViewManager MediaDBViewManager { get; } = App.Models.Get<MediaDBViewManager>();
		// require Preferences+MediaDBView
	//	public static RecentsManager RecentsManager { get; } = App.Models.Get<RecentsManager>();

		// require Preferences+AudioPlayer+MediaDBView
	//	public static JukeboxManager JukeboxManager { get; } = App.Models.Get<JukeboxManager>();
		// require Preferences+AudioPlayer+Jukebox
	//	public static ShortcutKeyManager ShortcutKeyManager { get; } = App.Models.Get<ShortcutKeyManager>();

		public static void Initialize()
		{
		//	App.Models.Get<XmlPreferencesManager>();
		//	App.Models.Get<AudioPlayerManager>();
		//	App.Models.Get<TasktrayIconView>();
		//	App.Models.Get<MediaDBManager>();
		}

		public static void Dispose()
		{
			// Preferences+AudioPlayer+Jukebox
		//	ShortcutKeyManager?.Dispose();
			// Preferences+AudioPlayer+MediaDBView
		//	JukeboxManager?.Dispose();

			// require Preferences+MediaDBView
		//	RecentsManager?.Dispose();
			// require Preferences+MediaDB+MediaItemFilter
		//	MediaDBViewManager?.Dispose();
			// require Preferences
		//	MediaItemFilterManager?.Dispose();

			// require Preferences+TaskIcon
		//	AppUpdateManager?.Dispose();

			// Standalones
		//	App.Models.DisposeObject<AudioPlayerManager>();
		//	TaskIconManager?.Dispose();
		//	MediaDBManager?.Dispose();
		}

	}
}

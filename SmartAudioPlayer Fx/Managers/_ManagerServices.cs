﻿using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SmartAudioPlayerFx.Managers
{
	static class ManagerServices
	{
		// Standalone
		public static XmlPreferencesManager PreferencesManager { get; } = App.Models.Get<XmlPreferencesManager>();
		//	public static AudioPlayerManager AudioPlayerManager { get; } = new AudioPlayerManager();
		public static VlcAudioPlayerManager AudioPlayerManager { get; } = App.Models.Get<VlcAudioPlayerManager>();
		public static TaskIconManager TaskIconManager { get; } = App.Models.Get<TaskIconManager>();
		public static MediaDBManager MediaDBManager { get; } = App.Models.Get<MediaDBManager>();

		// require Preferences+TaskIcon
		public static AppUpdateManager AppUpdateManager { get; } = App.Models.Get<AppUpdateManager>();

		// require Preferences
		public static MediaItemFilterManager MediaItemFilterManager { get; } = App.Models.Get<MediaItemFilterManager>();
		// require Preferences+MediaDB+MediaItemFilter
		public static MediaDBViewManager MediaDBViewManager { get; } = App.Models.Get<MediaDBViewManager>();
		// require Preferences+MediaDBView
		public static RecentsManager RecentsManager { get; } = App.Models.Get<RecentsManager>();

		// require Preferences+AudioPlayer+MediaDBView
		public static JukeboxManager JukeboxManager { get; } = App.Models.Get<JukeboxManager>();
		// require Preferences+AudioPlayer+Jukebox
		public static ShortcutKeyManager ShortcutKeyManager { get; } = App.Models.Get<ShortcutKeyManager>();

		public static void Initialize()
		{
			//	new VlcAudioPlayerManager();
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

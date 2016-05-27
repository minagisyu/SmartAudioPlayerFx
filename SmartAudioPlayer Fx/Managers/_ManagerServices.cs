using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SmartAudioPlayerFx.Managers
{
	static class ManagerServices
	{
		// Standalone
		public static AudioPlayerManager AudioPlayerManager { get; private set; }
		public static TaskIconManager TaskIconManager { get; private set; }
		public static XmlPreferencesManager PreferencesManager { get; private set; }
		public static MediaDBManager MediaDBManager { get; private set; }

		// require Preferences+TaskIcon
		public static AppUpdateManager AppUpdateManager { get; private set; }

		// require Preferences
		public static MediaItemFilterManager MediaItemFilterManager { get; private set; }
		// require Preferences+MediaDB+MediaItemFilter
		public static MediaDBViewManager MediaDBViewManager { get; private set; }
		// require Preferences+MediaDBView
		public static RecentsManager RecentsManager { get; private set; }

		// require Preferences+AudioPlayer+MediaDBView
		public static JukeboxManager JukeboxManager { get; private set; }
		// require Preferences+AudioPlayer+Jukebox
		public static ShortcutKeyManager ShortcutKeyManager { get; private set; }

		public static void Initialize(string dbFilename)
		{
			// Standalone
			PreferencesManager = new XmlPreferencesManager(isLoad: true);
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
			// Preferences+AudioPlayer+Jukebox
			if (ShortcutKeyManager != null)
			{
				ShortcutKeyManager.Dispose();
				ShortcutKeyManager = null;
			}
			// Preferences+AudioPlayer+MediaDBView
			if (JukeboxManager != null)
			{
				JukeboxManager.Dispose();
				JukeboxManager = null;
			}

			// require Preferences+MediaDBView
			if (RecentsManager != null)
			{
				RecentsManager.Dispose();
				RecentsManager = null;
			}
			// require Preferences+MediaDB+MediaItemFilter
			if (MediaDBViewManager != null)
			{
				MediaDBViewManager.Dispose();
				MediaDBViewManager = null;
			}
			// require Preferences
			if (MediaItemFilterManager != null)
			{
				MediaItemFilterManager.Dispose();
				MediaItemFilterManager = null;
			}

			// require Preferences+TaskIcon
			if (AppUpdateManager != null)
			{
				AppUpdateManager.Dispose();
				AppUpdateManager = null;
			}

			// Standalones
			if (AudioPlayerManager != null)
			{
				AudioPlayerManager.Dispose();
				AudioPlayerManager = null;
			}
			if (TaskIconManager != null)
			{
				TaskIconManager.Dispose();
				TaskIconManager = null;
			}
			if (PreferencesManager != null)
			{
				PreferencesManager.Dispose();
				PreferencesManager = null;
			}
			if (MediaDBManager != null)
			{
				MediaDBManager.Dispose();
				MediaDBManager = null;
			}
		}

	}

	// Managerは何も依存しない
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class StandaloneAttribute : Attribute
	{
		public StandaloneAttribute()
		{
		}
	}

	// ManagerはTypeに依存する
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class RequireAttribute : Attribute
	{
		public RequireAttribute(Type requireType)
		{
		}
	}
}

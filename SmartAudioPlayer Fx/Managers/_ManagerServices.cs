using System;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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
		static CompositeDisposable _disposable;

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

			_disposable = new CompositeDisposable(
				PreferencesManager, MediaDBManager,
				AudioPlayerManager, TaskIconManager,
				AppUpdateManager,
				MediaItemFilterManager, MediaDBViewManager, RecentsManager,
				JukeboxManager, ShortcutKeyManager);
		}

		public static void Dispose()
		{
			if (_disposable != null)
			{
				_disposable.Dispose();
			}
		}

	}

	// Managerは何も依存しない
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	sealed class StandaloneAttribute : Attribute
	{
		public StandaloneAttribute()
		{
		}
	}

	// ManagerはTypeに依存する
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	sealed class RequireAttribute : Attribute
	{
		public RequireAttribute(Type requireType)
		{
		}
	}
}

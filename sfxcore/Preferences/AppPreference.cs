using System;

namespace sfxcore.Preferences
{
	public sealed class AppPreference
	{
		public struct AppUpdate
		{
			public Uri UpdateInfo { get; set; }
			public DateTime LastCheckDate { get; set; }
			public Version LastCheckVersion { get; set; }
			public int CheckIntervalDays { get; set; }
			public bool IsAutoUpdateCheckEnabled { get; set; }
		}

		public struct Player
		{
			public string FocusPath { get; set; }
			public object[] AcceptExtensions { get; set; }
			public object[] IgnoreWords { get; set; }
			public string[] FolderRecents { get; set; }
			public string SelectMode { get; set; }
			public bool IsRepeat { get; set; }
			public bool IsPaused { get; set; }
			public double Volume { get; set; }
			public TimeSpan Position { get; set; }
			public string CurrenrMedia { get; set; }
			public string ViewMode { get; set; }
			public string ViewFocusPath { get; set; }
			public TimeSpan RecentIntervalDays { get; set; }
			public object[] ShortcutKeys { get; set; }
		}

		public struct Window
		{
			public object WindowPlacenent { get; set; }
			public int InactiveOpacity { get; set; }
			public int DeactiveOpacity { get; set; }
			public bool IsVisible { get; set; }
			public object DynamicWindowBounds { get; set; }
			public bool IsVisdeoDrawing { get; set; }
			public bool IsSoundFadeEffect { get; set; }
		}

		public struct MediaListWindow
		{
			public int Width { get; set; }
			public int Height { get; set; }
			public bool IsTitleFromFileName { get; set; }
			public bool IsAutoCloseWhenInactive { get; set; }
			public bool IsAutoCloseWhenLiseSelected { get; set; }
			public int TreeWidth { get; set; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sfxcore.Preferences
{
	class PlayerPreference
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
}

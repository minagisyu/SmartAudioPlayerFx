using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sfxcore.Preferences
{
	class AppUpdatePreference
	{
		public Uri UpdateInfo { get; set; }
		public DateTime LastCheckDate { get; set; }
		public Version LastCheckVersion { get; set; }
		public int CheckIntervalDays { get; set; }
		public bool IsAutoUpdateCheckEnabled { get; set; }



	}
}

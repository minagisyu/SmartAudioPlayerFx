using System;
using SmartAudioPlayerFx.Data;
using SmartAudioPlayerFx.Managers;

namespace SmartAudioPlayerFx.Views.Options
{
	sealed partial class OptionDialog_MediaList2 : OptionPage
	{
		public OptionDialog_MediaList2()
		{
			InitializeComponent();
			RecentIntervalDays = MediaDBViewFocus_LatestAddOnly.RecentIntervalDays;
		}

		public int RecentIntervalDays { get; set; }

		private void OptionDialog_MediaList2_Load(object sender, EventArgs e)
		{
			recentInterval.Value = Math.Min(Math.Max(recentInterval.Minimum, RecentIntervalDays), recentInterval.Maximum);
			recentInterval_ValueChanged(null, null);
		}

		public override void Save()
		{
			RecentIntervalDays = Math.Min(Math.Max(1, recentInterval.Value), 365);
			MediaDBViewFocus_LatestAddOnly.RecentIntervalDays = RecentIntervalDays;
			var vf = ManagerServices.JukeboxManager.ViewFocus.Value;
			if (vf is MediaDBViewFocus_LatestAddOnly)
			{
				ManagerServices.JukeboxManager.ViewFocus.Value =
					new MediaDBViewFocus_LatestAddOnly(vf.FocusPath);
			}
		}

		private void recentInterval_ValueChanged(object sender, EventArgs e)
		{
			int interval = recentInterval.Value;
			recentIntervalLabel.Text = "過去" + interval.ToString() + "日まで表示";
		}

	}
}

using System;
using System.Windows.Forms;

namespace SmartAudioPlayerFx.Views.Options
{
	sealed partial class OptionDialog_Update : OptionPage
	{
		public OptionDialog_Update()
		{
			InitializeComponent();
		}

		void OptionDialog_Update_Load(object sender, EventArgs e)
		{
			label_last_check_date.Text = ManagerServices.AppUpdateManager.LastCheckDate.ToLocalTime().ToString();
			label_last_check_version.Text = ManagerServices.AppUpdateManager.LastCheckVersion.ToString();
			checkBox_updateCheckEnabled.Checked = ManagerServices.AppUpdateManager.IsAutoUpdateCheckEnabled;
		}

		async void button1_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			button.Enabled = false;
			//
			var newVersion = await ManagerServices.AppUpdateManager.CheckUpdate();
			label_last_check_date.Text = ManagerServices.AppUpdateManager.LastCheckDate.ToLocalTime().ToString();
			label_last_check_version.Text = ManagerServices.AppUpdateManager.LastCheckVersion.ToString();
			if(newVersion == null)
			{
				MessageBox.Show("新しいアップデートはありませんでした", "SmartAudioPlayer Fx");
			}
			else if (await ManagerServices.AppUpdateManager.ShowUpdateMessageAsync(ParentDialog.Handle))
			{
				App.Current.Shutdown();
			}
			button.Enabled = true;
		}

		public override void Save()
		{
			ManagerServices.AppUpdateManager.IsAutoUpdateCheckEnabled = checkBox_updateCheckEnabled.Checked;
		}

	}
}

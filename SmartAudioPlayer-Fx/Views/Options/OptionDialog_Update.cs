using SmartAudioPlayerFx.AppUpdate;
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
			label_last_check_date.Text = App.Services.GetInstance<AppUpdateManager>().LastCheckDate.ToLocalTime().ToString();
			label_last_check_version.Text = App.Services.GetInstance<AppUpdateManager>().LastCheckVersion.ToString();
			checkBox_updateCheckEnabled.Checked = App.Services.GetInstance<AppUpdateManager>().IsAutoUpdateCheckEnabled;
		}

		async void button1_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			button.Enabled = false;
			//
			var newVersion = await App.Services.GetInstance<AppUpdateManager>().CheckUpdateAsync();
			label_last_check_date.Text = App.Services.GetInstance<AppUpdateManager>().LastCheckDate.ToLocalTime().ToString();
			label_last_check_version.Text = App.Services.GetInstance<AppUpdateManager>().LastCheckVersion.ToString();
			if(newVersion == null)
			{
				MessageBox.Show("新しいアップデートはありませんでした", "SmartAudioPlayer Fx");
			}
			else if (await App.Services.GetInstance<AppUpdateManager>().ShowUpdateMessageAsync(ParentDialog.Handle))
			{
				App.Current.Shutdown();
			}
			button.Enabled = true;
		}

		public override void Save()
		{
			App.Services.GetInstance<AppUpdateManager>().IsAutoUpdateCheckEnabled = checkBox_updateCheckEnabled.Checked;
		}

	}
}

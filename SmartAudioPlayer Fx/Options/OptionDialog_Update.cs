using System;
using System.Linq;
using System.Windows.Forms;
using SmartAudioPlayerFx.Update;

namespace SmartAudioPlayerFx.Options
{
	sealed partial class OptionDialog_Update : OptionPage
	{
		public OptionDialog_Update()
		{
			InitializeComponent();
		}

		void OptionDialog_Update_Load(object sender, EventArgs e)
		{
			label_last_check_date.Text = UpdateService.LastCheckDate.ToLocalTime().ToString();
			label_last_check_version.Text = UpdateService.LastCheckVersion.ToString();
			checkBox_updateCheckEnabled.Checked = UpdateService.IsAutoUpdateCheckEnabled;
		}

		void button1_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			button.Enabled = false;
			//
			var newVersionAvailable = false;
			UpdateService.CheckUpdate(_ =>
			{
				newVersionAvailable = true;
				UIService.UIThreadInvoke(() =>
				{
					if (UpdateService.ShowUpdateMessage(ParentDialog.Handle))
					{
						App.Current.MainWindow.Close();
						return;
					}
				});
			},
			() =>
			{
				UIService.UIThreadInvoke(() =>
				{
					label_last_check_date.Text = UpdateService.LastCheckDate.ToLocalTime().ToString();
					label_last_check_version.Text = UpdateService.LastCheckVersion.ToString();
					button.Enabled = true;
					if (newVersionAvailable == false)
					{
						MessageBox.Show("新しいアップデートはありませんでした", "SmartAudioPlayer Fx");
					}
				});
			});
		}

		public override void Save()
		{
			UpdateService.IsAutoUpdateCheckEnabled = checkBox_updateCheckEnabled.Checked;
		}

	}
}

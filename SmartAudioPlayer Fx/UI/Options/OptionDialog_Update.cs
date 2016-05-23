using System;
using System.Drawing;
using System.Windows.Forms;
using Quala;
using SmartAudioPlayerFx.Update;

namespace SmartAudioPlayerFx.UI.Options
{
	sealed partial class OptionDialog_Update : OptionPage
	{
		public OptionDialog_Update()
		{
			InitializeComponent();
		}

		void OptionDialog_Update_Load(object sender, EventArgs e)
		{
			label_last_check_date.Text = UpdateService.LastCheckDate.ToString();
			label_last_check_version.Text = UpdateService.LastCheckVersion.ToString();
		}

		void button1_Click(object sender, EventArgs e)
		{
			var button = sender as Button;
			button.Enabled = false;
			var ret = UpdateService.CheckUpdate() ?? false;
			if (ret && UpdateService.ShowUpdateMessage(ParentDialog.Handle))
			{
				App.Current.MainWindow.Close();
				return;
			}
			label_last_check_date.Text = UpdateService.LastCheckDate.ToString();
			label_last_check_version.Text = UpdateService.LastCheckVersion.ToString();
			button.Enabled = true;
			if (ret == false)
			{
				MessageBox.Show("新しいアップデートはありませんでした", "SmartAudioPlayer Fx");
			}
		}

		public override void Save()
		{
		}

	}
}

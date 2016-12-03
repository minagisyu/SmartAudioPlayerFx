using SmartAudioPlayerFx.MediaDB;
using System;
using System.Threading.Tasks;

namespace SmartAudioPlayerFx.Views.Options
{
	sealed partial class OptionDialog_DB : OptionPage
	{
		public OptionDialog_DB()
		{
			InitializeComponent();
		}

		void OptionDialog_DB_Load(object sender, EventArgs e)
		{
		}

		void button1_Click(object sender, EventArgs e)
		{
			Task.Run(() => App.Services.GetInstance<MediaDBManager>().Recycle(true));
		}

		public override void Save()
		{
		}

	}
}

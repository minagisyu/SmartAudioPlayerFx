using System;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.Options
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
			JukeboxService.AllItems.Recycle();
		}

		public override void Save()
		{
		}

	}
}

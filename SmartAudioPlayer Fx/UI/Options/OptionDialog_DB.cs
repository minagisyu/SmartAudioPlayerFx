using System;
using System.Drawing;
using System.Windows.Forms;
using Quala;
using SmartAudioPlayerFx.Update;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI.Options
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
			MediaDBService.Recycle(true);
		}

		public override void Save()
		{
		}

	}
}

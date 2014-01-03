using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using SmartAudioPlayerFx.Managers;

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
			Task.Run(() => ManagerServices.MediaDBManager.Recycle(true));
		}

		public override void Save()
		{
		}

	}
}

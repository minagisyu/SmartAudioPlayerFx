using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartAudioPlayerFx.Views.Options
{
	sealed partial class OptionDialog_Window : OptionPage
	{
		public OptionDialog_Window()
		{
			InitializeComponent();
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
		}

		public int InactiveOpacity { get; set; }
		public int DeactiveOpacity { get; set; }

		void OptionDialog_Window_Load(object sender, EventArgs e)
		{
			activeOpacity.Value = Math.Min(Math.Max(activeOpacity.Minimum, InactiveOpacity), activeOpacity.Maximum);
			deactiveOpacity.Value = Math.Min(Math.Max(deactiveOpacity.Minimum, DeactiveOpacity), deactiveOpacity.Maximum);
			activeOpacity_ValueChanged(null, null);
			deactiveOpacity_ValueChanged(null, null);
		}

		public override void Save()
		{
			InactiveOpacity = activeOpacity.Value;
			DeactiveOpacity = deactiveOpacity.Value;
		}

		void activeOpacity_ValueChanged(object sender, EventArgs e)
		{
			int opacity = activeOpacity.Value;
			activeOpacityLabel.Text = "“§–¾“x" + opacity.ToString() + "%";
			var alpha = (255.0 / 100.0) * (double)opacity;
			active_preview.BackColor = Color.FromArgb((int)alpha, 192, 192, 255);
		}

		void deactiveOpacity_ValueChanged(object sender, EventArgs e)
		{
			int opacity = deactiveOpacity.Value;
			deactiveOpacityLabel.Text = "“§–¾“x" + opacity.ToString() + "%";
			var alpha = (255.0/100.0) * (double)opacity;
			deactive_preview.BackColor = Color.FromArgb((int)alpha, 192, 192, 255);
		}

	}
}

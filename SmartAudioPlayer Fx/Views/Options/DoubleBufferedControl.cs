namespace SmartAudioPlayerFx.Views.Options
{
	using System.Windows.Forms;

	sealed partial class DoubleBufferedControl : Control
	{
		public DoubleBufferedControl()
		{
			InitializeComponent();
			SetStyle(
				ControlStyles.AllPaintingInWmPaint |
				ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.ResizeRedraw |
				ControlStyles.SupportsTransparentBackColor, true);
			DoubleBuffered = true;
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);
		}
	}
}

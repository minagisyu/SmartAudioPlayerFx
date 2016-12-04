using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace Quala.WPF.Behavior
{
	/// <summary>
	/// SliderのTooltipの出現位置を自動的に調整する
	/// </summary>
	public class SliderTooltipAutoAdjastBehavior : Behavior<Slider>
	{
		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
			AssociatedObject.MouseEnter -= AssociatedObject_MouseEnter;
		}

		void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
		{
			var pt = AssociatedObject.PointToScreen(new Point(0, 0));
			var screen = WinForms.Screen.FromPoint(new Drawing.Point((int)pt.X, (int)pt.Y));
			var area = screen.WorkingArea;
			var topSpace = pt.Y - area.Top;
			AssociatedObject.AutoToolTipPlacement =
				(topSpace > 32) ? AutoToolTipPlacement.TopLeft : AutoToolTipPlacement.BottomRight;
		}

	}
}

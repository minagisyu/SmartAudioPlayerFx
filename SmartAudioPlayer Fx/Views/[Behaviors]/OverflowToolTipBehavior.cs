using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;

namespace SmartAudioPlayerFx.Views
{
	// RenderedSizeが足りなくてはみ出す場合にToolTipで一時的に描画するビヘイビア
	class OverflowToolTipBehavior : Behavior<FrameworkElement>
	{
		protected override void OnAttached()
		{
			base.OnAttached();
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();
		}

	}
}

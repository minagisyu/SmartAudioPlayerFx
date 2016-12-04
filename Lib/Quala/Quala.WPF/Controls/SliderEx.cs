using System;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Quala.WPF.Controls
{
	/// <summary>
	/// Sliderのツールチップ・コンテンツを変換出来るように改造
	/// </summary>
	/// <remarks>
	/// ・Slider内部のAutoToolTipをリフレクションで取得
	/// ・AutoTooltipConverterでdouble値をカスタム文字列に変更
	/// </remarks>
	public class SliderEx : Slider
	{
		// Slider.Value -> ToolTip.Content
		public Converter<double, string> AutoTooltipConverter { get; set; }

		ToolTip _autoTooltip;
		ToolTip GetAutoTooltip()
		{
			// Sliderが必要になったタイミングで作成する模様？
			return _autoTooltip ??
				(_autoTooltip = typeof(Slider)
					.GetField("_autoToolTip", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(this) as ToolTip);
		}

		protected override void OnThumbDragStarted(DragStartedEventArgs e)
		{
			base.OnThumbDragStarted(e);
			var tooltip = GetAutoTooltip();
			if (tooltip != null && AutoTooltipConverter != null)
				tooltip.Content = AutoTooltipConverter(Value);
		}

		protected override void OnThumbDragDelta(DragDeltaEventArgs e)
		{
			base.OnThumbDragDelta(e);
			var tooltip = GetAutoTooltip();
			if (tooltip != null && AutoTooltipConverter != null)
				tooltip.Content = AutoTooltipConverter(Value);
		}

	}
}

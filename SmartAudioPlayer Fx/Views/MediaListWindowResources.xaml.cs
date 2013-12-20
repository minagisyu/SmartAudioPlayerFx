using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.Views
{
	sealed partial class MediaListWindowResources : ResourceDictionary
	{
		void TreeItem_TextBlock_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			// RenderedSizeがDesiredSizeより大きい(領域からはみ出している)ならツールチップ有効
			// MEMO: ConverterのIsElementWrappingConverterと同等だが、Converterは初回だけ呼び出されて終わる...
			var elem = sender as FrameworkElement;
			if (elem == null) return;
			ToolTipService.SetIsEnabled(elem, elem.RenderSize.Width > elem.DesiredSize.Width);
		}
	}

	#region Converter

	sealed class LeftMarginMultiplierConverter : IValueConverter
	{
		public double Length { get; set; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value is int) ? new Thickness(Length * (int)value, 0, 0, 0) : new Thickness(0);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new System.NotImplementedException();
		}
	}
	sealed class IsElementWrappingConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var elem = value as FrameworkElement;
			if (elem == null) return false;
			return true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new System.NotImplementedException();
		}
	}

	#endregion
}

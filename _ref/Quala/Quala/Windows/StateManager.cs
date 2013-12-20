using System;
using System.Windows;
using System.Windows.Controls;

namespace Quala.Windows
{
	/// <summary>
	/// VisualStateManager.GoToElementState(XXX, YYY)をXAMLで書くためのビヘイビア。
	/// </summary>
	/// <remarks>
	/// XXXに相当する部分のコントロールに組み込んで、YYYの文字列を返す配列をバインディングすると幸せになれる。
	/// 
	/// ＜Window StateManager.VisualStateProperty="{Binding State}"/＞
	/// (DataContext.Stateとバインディング、Stateプロパティは"State1", "State2"などのVisualState.Nameを返す)
	/// </remarks>
	public class StateManager : DependencyObject
	{
		public static string GetVisualStateProperty(DependencyObject obj)
		{
			return (string)obj.GetValue(VisualStatePropertyProperty);
		}

		public static void SetVisualStateProperty(DependencyObject obj, string value)
		{
			obj.SetValue(VisualStatePropertyProperty, value);
		}

		public static readonly DependencyProperty VisualStatePropertyProperty =
			DependencyProperty.RegisterAttached(
				"VisualStateProperty",
				typeof(string),
				typeof(StateManager),
				new PropertyMetadata((s, e) =>
				{
					var propertyName = (string)e.NewValue;
					var ctrl = s as Control;
					if (ctrl == null)
						throw new InvalidOperationException("This attached property only supports types derived from Control.");
					System.Windows.VisualStateManager.GoToElementState(ctrl, (string)e.NewValue, true);
				})
			);
	}
}

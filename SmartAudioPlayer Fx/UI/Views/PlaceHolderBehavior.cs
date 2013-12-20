using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartAudioPlayerFx.UI.Views
{
	/// <summary>
	/// プレースホルダーを表示する添付ビヘイビア
	/// </summary>
	/// <remarks>
	/// http://d.hatena.ne.jp/griefworker/20100929/textbox_placeholder
	/// 使い方はこんな感じです。
	/// 実行すると、TextBox が未入力のときにプレースホルダーが表示されます。
	/// <Window x:Class="PlaceHolderSample.MainWindow"
	///     xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	///     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	///     xmlns:local="clr-namespace:PlaceHolderSample"
	///     Title="MainWindow" Height="350" Width="525">
	///   <Canvas>
	///     <TextBox x:Name="_nameTextBox"
	///       Canvas.Left="30" Canvas.Top="30" Width="200"
	///       local:PlaceHolderBehavior.PlaceHolderText="名前"/>
	///   </Canvas>
	/// </Window>
	/// </remarks>
	public static class PlaceHolderBehavior
	{
		// プレースホルダーとして表示するテキスト
		public static readonly DependencyProperty PlaceHolderTextProperty = DependencyProperty.RegisterAttached(
			"PlaceHolderText",
			typeof(string),
			typeof(PlaceHolderBehavior),
			new PropertyMetadata(null, OnPlaceHolderChanged));

		private static void OnPlaceHolderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var textBox = sender as TextBox;
			if (textBox == null)
			{
				return;
			}

			var placeHolder = e.NewValue as string;
			var handler = CreateEventHandler(placeHolder);
			if (string.IsNullOrEmpty(placeHolder))
			{
				textBox.TextChanged -= handler;
			}
			else
			{
				textBox.TextChanged += handler;
				if (string.IsNullOrEmpty(textBox.Text))
				{
					textBox.Background = CreateVisualBrush(placeHolder);
				}
			}
		}

		private static TextChangedEventHandler CreateEventHandler(string placeHolder)
		{
			// TextChanged イベントをハンドルし、TextBox が未入力のときだけ
			// プレースホルダーを表示するようにする。
			return (sender, e) =>
			{
				// 背景に TextBlock を描画する VisualBrush を使って
				// プレースホルダーを実現
				var textBox = (TextBox)sender;
				if (string.IsNullOrEmpty(textBox.Text))
				{
					textBox.Background = CreateVisualBrush(placeHolder);
				}
				else
				{
					textBox.Background = new SolidColorBrush(Colors.Transparent);
				}
			};
		}

		private static VisualBrush CreateVisualBrush(string placeHolder)
		{
			var visual = new Label()
			{
				Content = placeHolder,
				Padding = new Thickness(5, 1, 1, 1),
			//	Foreground = new SolidColorBrush(Colors.LightGray),
				Foreground = new SolidColorBrush(Colors.Gray),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
			};
			return new VisualBrush(visual)
			{
				Stretch = Stretch.None,
				TileMode = TileMode.None,
				AlignmentX = AlignmentX.Left,
				AlignmentY = AlignmentY.Center,
			};
		}

		public static void SetPlaceHolderText(TextBox textBox, string placeHolder)
		{
			textBox.SetValue(PlaceHolderTextProperty, placeHolder);
		}

		public static string GetPlaceHolderText(TextBox textBox)
		{
			return textBox.GetValue(PlaceHolderTextProperty) as string;
		}
	}
}

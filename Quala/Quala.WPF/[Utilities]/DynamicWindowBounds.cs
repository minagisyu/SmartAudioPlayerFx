using System.Windows;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace Quala.WPF
{
	/// <summary>
	/// ウィンドウの矩形をディスプレイサイズの割合に変換します
	/// </summary>
	public struct DynamicWindowBounds
	{
		public Int32Rect RealBounds;	// 複数画面を含む絶対座標でのウィンドウ矩形
		public Point LogicalPoint;		// 単一画面の幅(0.0-1.0)を基準としたウィンドウの左上座標
		public Size LogicalSize;		// 単一画面の幅(0.0-1.0)を基準としたウィンドウのサイズ
		public LogicalBasePoint LogicalBase;	// LogicalPointの基準点
		// to WindowBounds
		public DynamicWindowBounds(double left, double top, double width, double height)
		{
			// 通常座標を格納
			RealBounds = new Int32Rect((int)left, (int)top, (int)width, (int)height);
			// 対象となるスクリーンを取得
			var screen = WinForms.Screen.FromRectangle(new Drawing.Rectangle((int)left, (int)top, (int)width, (int)height));
			var area = screen.WorkingArea;
			// 空きスペースが小さい方を基準点にする
			var leftSpace = left - area.Left;
			var topSpace = top - area.Top;
			var rightSpace = area.Right - (left + width);
			var bottomSpace = area.Bottom - (top + height);
			LogicalBase = (leftSpace < rightSpace) ?
				(topSpace < bottomSpace) ?
					LogicalBasePoint.LeftTop :
					LogicalBasePoint.LeftBottom :
				(topSpace < bottomSpace) ?
					LogicalBasePoint.RightTop :
					LogicalBasePoint.RightBottom;
			// 基準点を0.0として論理位置を計算
			var segment_x = 1.0 / (double)area.Width;
			var segment_y = 1.0 / (double)area.Height;
			switch (LogicalBase)
			{
				case LogicalBasePoint.LeftTop:
					LogicalPoint = new Point(
						(left - area.Left) * segment_x,
						(top - area.Top) * segment_y);
					break;
				case LogicalBasePoint.LeftBottom:
					LogicalPoint = new Point(
						(left - area.Left) * segment_x,
						(top - area.Bottom) * segment_y);
					break;
				case LogicalBasePoint.RightTop:
					LogicalPoint = new Point(
						(left - area.Right) * segment_x,
						(top - area.Top) * segment_y);
					break;
				case LogicalBasePoint.RightBottom:
					LogicalPoint = new Point(
						(left - area.Right) * segment_x,
						(top - area.Bottom) * segment_y);
					break;
				default:
					LogicalPoint = new Point();
					break;
			}
			// スクリーンの幅を元にLogicalXXXを設定
			LogicalSize = new Size(width * segment_x, height * segment_y);
		}

		// to Normal Window Rectangle
		//  - noSizeChange: 解像度に合わせてサイズ(Width/Height)が変更されるのを抑制します(RealBoundsの値が参照されます)
		public Rect ToRect(bool noSizeChange)
		{
			// 対象となるスクリーンを取得
			var screen = WinForms.Screen.FromRectangle(
				new Drawing.Rectangle((int)RealBounds.X, (int)RealBounds.Y, (int)RealBounds.Width, (int)RealBounds.Height));
			// スクリーンの幅を元に矩形を復元
			var area = screen.WorkingArea;
			var rect = Rect.Empty;
			switch (LogicalBase)
			{
				case LogicalBasePoint.LeftTop:
					rect = new Rect(
						(LogicalPoint.X * (double)area.Width) + area.Left,
						(LogicalPoint.Y * (double)area.Height) + area.Top,
						LogicalSize.Width * (double)area.Width,
						LogicalSize.Height* (double)area.Height);
					break;
				case LogicalBasePoint.LeftBottom:
					rect = new Rect(
						(LogicalPoint.X * (double)area.Width) + area.Left,
						(LogicalPoint.Y * (double)area.Height) + area.Bottom,
						LogicalSize.Width * (double)area.Width,
						LogicalSize.Height* (double)area.Height);
					break;
				case LogicalBasePoint.RightTop:
					rect = new Rect(
						(LogicalPoint.X * (double)area.Width) + area.Right,
						(LogicalPoint.Y * (double)area.Height) + area.Top,
						LogicalSize.Width * (double)area.Width,
						LogicalSize.Height* (double)area.Height);
					break;
				case LogicalBasePoint.RightBottom:
					rect = new Rect(
						(LogicalPoint.X * (double)area.Width) + area.Right,
						(LogicalPoint.Y * (double)area.Height) + area.Bottom,
						LogicalSize.Width * (double)area.Width,
						LogicalSize.Height* (double)area.Height);
					break;
			}
			if (noSizeChange)
			{
				rect.X += rect.Width;
				rect.Y += rect.Height;
				rect.Width = RealBounds.Width;
				rect.Height = RealBounds.Height;
				rect.X -= RealBounds.Width;
				rect.Y -= RealBounds.Height;
			}
			return rect;
		}

		public enum LogicalBasePoint
		{
			LeftTop,
			LeftBottom,
			RightTop,
			RightBottom,
		}

	}
}

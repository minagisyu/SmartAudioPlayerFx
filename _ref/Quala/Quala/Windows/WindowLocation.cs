using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;

namespace Quala.Windows
{
	/// <summary>
	/// 基点と位置、距離を示す構造体。
	/// ウィンドウの位置を保存するために使用します。
	/// </summary>
	/// <remarks>
	/// 画面に一番近い点を基点として距離を保存するため、
	/// ウィンドウの位置は、解像度が変化してもユーザーの意志に近い場所になるはず。
	/// </remarks>
	[Obsolete("use DynamicWindowBounds")]
	[DebuggerDisplay("{Base}, {X}, {Y}, {Width}, {Height}")]
	public struct WindowLocation : INotifyPropertyChanged
	{
		//
		// [TODO]
		// プライマリスクリーンからの視点に変更するか…？
		// 画面外なら画面内に補正するとか。
		//
		public WindowLocation(Rect rect)
			: this()
		{
			var wa = Screen.PrimaryScreen.WorkingArea;

			var leftSpace = rect.Left - wa.Left;
			var topSpace = rect.Top - wa.Top;
			var rightSpace = wa.Right - rect.Right;
			var bottomSpace = wa.Bottom - rect.Bottom;

			// 距離が狭い方を選ぶ
			this.Base = (leftSpace < rightSpace) ?
				(topSpace < bottomSpace) ?
					WindowLocation.BasePoint.LeftTop :
					WindowLocation.BasePoint.LeftBottom :
				(topSpace < bottomSpace) ?
					WindowLocation.BasePoint.RightTop :
					WindowLocation.BasePoint.RightBottom;

			switch(this.Base)
			{
				case BasePoint.LeftTop:
					this.X = (int)rect.Left - wa.Left;
					this.Y = (int)rect.Top - wa.Top;
					this.Width = (int)rect.Width;
					this.Height = (int)rect.Height;
					break;

				case BasePoint.LeftBottom:
					this.X = (int)rect.Left - wa.Left;
					this.Y = (int)rect.Top - wa.Bottom;
					this.Width = (int)rect.Width;
					this.Height = (int)rect.Height;
					break;

				case BasePoint.RightTop:
					this.X = (int)rect.Left - wa.Right;
					this.Y = (int)rect.Top - wa.Top;
					this.Width = (int)rect.Width;
					this.Height = (int)rect.Height;
					break;

				case BasePoint.RightBottom:
					this.X = (int)rect.Left - wa.Right;
					this.Y = (int)rect.Top - wa.Bottom;
					this.Width = (int)rect.Width;
					this.Height = (int)rect.Height;
					break;
			}
		}

		public Rect ToRect()
		{
			var wa = Screen.PrimaryScreen.WorkingArea;
			var rect = new Rect();

			switch(this.Base)
			{
				case BasePoint.LeftTop:
					rect = new Rect(
						_x + wa.Left,
						_y + wa.Top,
						_width,
						_height);
					break;

				case BasePoint.LeftBottom:
					rect = new Rect(
						_x + wa.Left,
						_y + wa.Bottom,
						_width,
						_height);
					break;

				case BasePoint.RightTop:
					rect = new Rect(
						_x + wa.Right,
						_y + wa.Top,
						_width,
						_height);
					break;

				case BasePoint.RightBottom:
					rect = new Rect(
						_x + wa.Right,
						_y + wa.Bottom,
						_width,
						_height);
					break;

				default:
					return Rect.Empty;
			}

			return rect;
		}

		public override string ToString()
		{
			return string.Format(
				"{{{0}, {1}, {2}, {3}, {4}}}",
				m_basePoint, _x, _y, _width, _height);
		}

		#region Property

		BasePoint m_basePoint;
		int _x;
		int _y;
		int _width;
		int _height;

		public BasePoint Base
		{
			get { return m_basePoint; }
			set
			{
				if(m_basePoint != value)
				{
					m_basePoint = value;
					PropertyChanged.InvokeOrIgnore(this, _basePointChanged);
				}
			}
		}

		public int X
		{
			get { return _x; }
			set
			{
				if(_x != value)
				{
					_x = value;
					PropertyChanged.InvokeOrIgnore(this, _xChanged);
				}
			}
		}
		public int Y
		{
			get { return _y; }
			set
			{
				if(_y != value)
				{
					_y = value;
					PropertyChanged.InvokeOrIgnore(this, _yChanged);
				}
			}
		}
		public int Width
		{
			get { return _width; }
			set
			{
				if(_width != value)
				{
					_width = value;
					PropertyChanged.InvokeOrIgnore(this, _widthChanged);
				}
			}
		}
		public int Height
		{
			get { return _height; }
			set
			{
				if(_height != value)
				{
					_height = value;
					PropertyChanged.InvokeOrIgnore(this, _heightChanged);
				}
			}
		}

		#endregion
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;
		static readonly PropertyChangedEventArgs _basePointChanged = new PropertyChangedEventArgs("Base");
		static readonly PropertyChangedEventArgs _xChanged = new PropertyChangedEventArgs("X");
		static readonly PropertyChangedEventArgs _yChanged = new PropertyChangedEventArgs("Y");
		static readonly PropertyChangedEventArgs _widthChanged = new PropertyChangedEventArgs("Width");
		static readonly PropertyChangedEventArgs _heightChanged = new PropertyChangedEventArgs("Height");

		#endregion
	
		/// <summary>
		/// 値の基点となる方向。
		/// 基点+値が期待している値。
		/// </summary>
		public enum BasePoint
		{
			[Description("左上")]
			LeftTop,
			[Description("左下")]
			LeftBottom,
			[Description("右上")]
			RightTop,
			[Description("右下")]
			RightBottom,
		}

	}
}

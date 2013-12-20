using System;
using System.Drawing;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// 中央点を取得
		/// </summary>
		/// <param name="rect"></param>
		/// <returns></returns>
		public static Point GetCenterPoint(this Rectangle rect)
		{
			return new Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
		}
	}
}

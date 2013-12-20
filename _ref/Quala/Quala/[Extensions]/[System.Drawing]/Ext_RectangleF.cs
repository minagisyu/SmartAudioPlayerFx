﻿using System;
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
		public static PointF GetCenterPoint(this RectangleF rect)
		{
			return new PointF(rect.X + (rect.Width / 2f), rect.Y + (rect.Height / 2f));
		}
	}
}
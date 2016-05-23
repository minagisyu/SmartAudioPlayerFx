using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Quala;

namespace Quala.Windows.Forms
{
	public sealed class DraggingEventArgs : EventArgs
	{
		public Size Moved { get; private set; }
		public DraggingEventArgs(Size moved) { Moved = moved; }
	}

	public class DraggablePanel : Panel
	{
		Cursor m_defaultCursor;
		Point m_mouseDragPoint;
		public Cursor DraggingCursor { get; set; }

		public DraggablePanel()
		{
			DraggingCursor = Cursors.Default;
		}

		public event EventHandler<DraggingEventArgs> Dragging;
		protected virtual void OnDragging(Size moved)
		{
			if(Dragging != null)
				Dragging(this, new DraggingEventArgs(moved));
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			// 左クリックならマウスキャプチャを開始してポインタの位置を記憶する
			if(e.Button == MouseButtons.Left)
			{
				Capture = true;
				m_mouseDragPoint = new Point(e.X, e.Y);

				// カーソル変更
				m_defaultCursor = base.Cursor;
				base.Cursor = DraggingCursor;
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			// 左クリックならマウスキャプチャを中止する
			// 左右同時に放されたりしても対応できるようにAndをとる。
			if((e.Button & MouseButtons.Left) != 0)
			{
				Capture = false;
				base.Cursor = m_defaultCursor;
			}
			base.OnMouseUp(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			// 左クリック＋マウスキャプチャ中ならフォームサイズを変動させる
			if(e.Button == MouseButtons.Left && Capture)
			{
				Size sz = new Size(m_mouseDragPoint - new Size(e.Location));
				OnDragging(sz);
			}
			base.OnMouseMove(e);
		}

	}
}

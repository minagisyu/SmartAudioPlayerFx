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
			// ���N���b�N�Ȃ�}�E�X�L���v�`�����J�n���ă|�C���^�̈ʒu���L������
			if(e.Button == MouseButtons.Left)
			{
				Capture = true;
				m_mouseDragPoint = new Point(e.X, e.Y);

				// �J�[�\���ύX
				m_defaultCursor = base.Cursor;
				base.Cursor = DraggingCursor;
			}
			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			// ���N���b�N�Ȃ�}�E�X�L���v�`���𒆎~����
			// ���E�����ɕ����ꂽ�肵�Ă��Ή��ł���悤��And���Ƃ�B
			if((e.Button & MouseButtons.Left) != 0)
			{
				Capture = false;
				base.Cursor = m_defaultCursor;
			}
			base.OnMouseUp(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			// ���N���b�N�{�}�E�X�L���v�`�����Ȃ�t�H�[���T�C�Y��ϓ�������
			if(e.Button == MouseButtons.Left && Capture)
			{
				Size sz = new Size(m_mouseDragPoint - new Size(e.Location));
				OnDragging(sz);
			}
			base.OnMouseMove(e);
		}

	}
}

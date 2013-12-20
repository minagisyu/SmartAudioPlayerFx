using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Quala.Interop.Win32;

namespace Quala.Windows.Forms
{
	/// <summary>
	/// �g�����ꂽForm�N���X
	/// </summary>
	public class FormEx : Form
	{
		#region Resizing

		/// <summary>
		/// �t�H�[�������T�C�Y����Ă���Ƃ��ɌĂяo����܂��B
		/// ���T�C�Y�����O�Ƀt�H�[���T�C�Y�𒲐����邱�Ƃ��\�ł��B
		/// </summary>
		public event EventHandler<ResizingEventArgs> Resizing;
		protected virtual void OnResizing(ResizingEventArgs e) { if(Resizing != null) Resizing(this, e); }

		void WmSizing(ref Message m)
		{
			base.WndProc(ref m);
			ResizingEdge edge = (ResizingEdge)m.WParam.ToInt32();
			RECT rc = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
			Rectangle bounds = Rectangle.FromLTRB(rc.left, rc.top, rc.right, rc.bottom);
			ResizingEventArgs e = new ResizingEventArgs(edge, bounds);
			OnResizing(e);
			if(e.changed)
			{
				rc.left = e.Bounds.Left;
				rc.top = e.Bounds.Top;
				rc.right = e.Bounds.Right;
				rc.bottom = e.Bounds.Bottom;
				Marshal.StructureToPtr(rc, m.LParam, true);
				m.Result = (IntPtr)1;
			}
			else
			{
				m.Result = IntPtr.Zero;
			}
		}

		#endregion

		[DebuggerHidden]
		protected override void WndProc(ref Message m)
		{
			switch((WM)m.Msg)
			{
				case WM.SIZING:
					WmSizing(ref m);
					return;
			}
			base.WndProc(ref m);
		}

	}

	#region Resizing

	/// <summary>
	/// �t�H�[���̂ǂ̊p�Ń��T�C�Y����Ă��邩�������l
	/// </summary>
	public enum ResizingEdge
	{
		// WM_SIZING [WMSZ_xxxx]
		Left = 1,
		Right = 2,
		Top = 3,
		TopLeft = 4,
		TopRight = 5,
		Bottom = 6,
		BottomLeft = 7,
		BottomRight = 8,
	}

	public sealed class ResizingEventArgs : EventArgs
	{
		ResizingEdge point;
		Rectangle bounds;
		internal bool changed = false;
		public ResizingEdge ResizingPoint { get { return point; } }
		public Rectangle Bounds { get { return bounds; } set { bounds = value; changed = true; } }

		public ResizingEventArgs(ResizingEdge point, Rectangle bounds)
		{
			this.point = point;
			this.bounds = bounds;
		}
	}

	#endregion
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Quala.Interop.Win32;
using System.Drawing;

namespace Quala.Windows.Forms
{
	/// <summary>
	/// ContextMenuStrip�������������́B
	///  - �\������Ƃ��Ƀt�F�[�h����
	///  - �œK�ȃ��j���[�ʒu���v�Z���ĕ\������ShowActual()���\�b�h
	/// </summary>
	public class ContextMenuStripEx : ContextMenuStrip
	{
		public ContextMenuStripEx() { }
		public ContextMenuStripEx(IContainer container) : base(container) { }

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if(!Visible) return;
			API.AnimateWindow(Handle, 200, AW.ACTIVATE | AW.BLEND);
		}

		/// <summary>
		/// ���j���[�̃I�[�i�[�ƕ\������ʒu���w�肵�āA
		/// �œK�ȃ��j���[�\���ʒu���v�Z���A�␳���Ă���\�����܂��B
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		public void ShowActual(Control owner, Point location)
		{
			Show(location, GetActualDirection(owner, location));
		}

		ToolStripDropDownDirection GetActualDirection(Control owner, Point location)
		{
			Screen sc = Screen.FromControl(owner);
			Rectangle rc = sc.WorkingArea;

			int top = location.Y - rc.Top;
			int right = rc.Right - location.X;

			ToolStripDropDownDirection dir;
			if(top > Height)
			{
				if(right > Width)
					dir = ToolStripDropDownDirection.AboveRight;
				else
					dir = ToolStripDropDownDirection.AboveLeft;
			}
			else
			{
				if(right > Width)
					dir = ToolStripDropDownDirection.BelowRight;
				else
					dir = ToolStripDropDownDirection.BelowLeft;
			}
			return dir;
		}

	}
}

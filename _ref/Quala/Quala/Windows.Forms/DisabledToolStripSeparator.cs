using System;
using System.Windows.Forms;

namespace Quala.Windows.Forms
{
	public sealed class DisabledToolStripSeparator : ToolStripSeparator
	{
		public DisabledToolStripSeparator()
		{
			// [MEMO]
			// ToolStripSeparator()を選択できないように無効に。
			// 何で選択できるようになってるねん…。
			this.Enabled = false;
		}
	}
}

using System.Text;
using System.Windows.Forms;
using Quala.Interop.Win32;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI.Options
{
	sealed class KeySettingListView : ListView
	{
		int keyset = 0;

		protected override void WndProc(ref Message m)
		{
			switch((WM)m.Msg)
			{
				case WM.KEYDOWN:
				case WM.SYSKEYDOWN:
					if(SelectedItems.Count == 0) return;
					if((((int)m.LParam & 0x40000000) == 0) &&	// is_down
						(int)m.WParam != 0x11 && 				// VK_CONTROL
						(int)m.WParam != 0x12 && 				// VK_MENU
						(int)m.WParam != 0x10) 					// VK_SHIFT
					{
						var key = ShortcutKeyService.HotKeyManager.LastDownKey;
						if (key.HasFlag(Keys.Control)) keyset++;
						if (key.HasFlag(Keys.Alt)) keyset++;
						if (key.HasFlag(Keys.Shift)) keyset++;
						keyset++;

						// �L�[�� �ɐݒ�
						var item = SelectedItems[0].SubItems[1];
						item.Text = GetKeyName(key);
						item.Tag = key;
					}
					return;

				case WM.KEYUP:
				case WM.SYSKEYUP:
					if(SelectedItems.Count == 0) return;
					if(keyset != 0) { keyset--; }
					else
					{
						// �Ȃ� �ɐݒ�
						var item = SelectedItems[0].SubItems[1];
						item.Text = "�Ȃ�";
						item.Tag = Keys.None;
					}
					return;
			}
			base.WndProc(ref m);
		}

		public static string GetKeyName(Keys key)
		{
			var modifier = (key & Keys.Modifiers);
			var vk = (key & Keys.KeyCode);
			if (vk == Keys.None) return "�Ȃ�";

			var sb = new StringBuilder();
			if (key.HasFlag(Keys.Control)) sb.Append("Ctrl + ");
			if (key.HasFlag(Keys.Alt)) sb.Append("Alt + ");
			if (key.HasFlag(Keys.Shift)) sb.Append("Shift + ");
			sb.Append(vk.ToString());
			return sb.ToString();
		}

	}
}

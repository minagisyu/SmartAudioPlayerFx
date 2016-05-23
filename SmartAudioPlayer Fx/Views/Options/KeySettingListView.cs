using System.Text;
using System.Windows.Forms;
using SmartAudioPlayerFx.Managers;
using Quala.Win32;

namespace SmartAudioPlayerFx.Views.Options
{
	sealed class KeySettingListView : ListView
	{
		int keyset = 0;

		protected override void WndProc(ref Message m)
		{
			switch((WinAPI.WM)m.Msg)
			{
				case WinAPI.WM.KEYDOWN:
				case WinAPI.WM.SYSKEYDOWN:
					if(SelectedItems.Count == 0) return;
					if((((int)m.LParam & 0x40000000) == 0) &&	// is_down
						(int)m.WParam != 0x11 && 				// VK_CONTROL
						(int)m.WParam != 0x12 && 				// VK_MENU
						(int)m.WParam != 0x10) 					// VK_SHIFT
					{
						var key = ManagerServices.ShortcutKeyManager.LastDownKey;
						if (key.HasFlag(Keys.Control)) keyset++;
						if (key.HasFlag(Keys.Alt)) keyset++;
						if (key.HasFlag(Keys.Shift)) keyset++;
						keyset++;

						// ÉLÅ[ñº Ç…ê›íË
						var item = SelectedItems[0].SubItems[1];
						item.Text = GetKeyName(key);
						item.Tag = key;
					}
					return;

				case WinAPI.WM.KEYUP:
				case WinAPI.WM.SYSKEYUP:
					if(SelectedItems.Count == 0) return;
					if(keyset != 0) { keyset--; }
					else
					{
						// Ç»Çµ Ç…ê›íË
						var item = SelectedItems[0].SubItems[1];
						item.Text = "Ç»Çµ";
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
			if (vk == Keys.None) return "Ç»Çµ";

			var sb = new StringBuilder();
			if (key.HasFlag(Keys.Control)) sb.Append("Ctrl + ");
			if (key.HasFlag(Keys.Alt)) sb.Append("Alt + ");
			if (key.HasFlag(Keys.Shift)) sb.Append("Shift + ");
			sb.Append(vk.ToString());
			return sb.ToString();
		}

	}
}

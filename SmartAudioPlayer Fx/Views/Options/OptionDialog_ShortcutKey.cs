using SmartAudioPlayerFx.Shortcut;
using System;
using System.Windows.Forms;

namespace SmartAudioPlayerFx.Views.Options
{
	sealed partial class OptionDialog_ShortcutKey : OptionPage
	{
		public OptionDialog_ShortcutKey()
		{
			InitializeComponent();
		}

		void OptionDialog_Shortcut_Load(object sender, EventArgs e)
		{
			keySetting.BeginUpdate();
			keySetting.Items.Clear();

			var ks = new[]
			{
				new { Name = "�Đ����[�h(�����_��)", Feature = ShortcutKeyManager.Features.PlayMode_Random, },
				new { Name = "�Đ����[�h(���X�g)", Feature = ShortcutKeyManager.Features.PlayMode_FileName, },
				new { Name = "�Đ����[�h(���s�[�g)", Feature = ShortcutKeyManager.Features.PlayMode_Repeat, },
				new { Name = "�Đ�/�ꎞ��~", Feature = ShortcutKeyManager.Features.Player_PlayPause, },
				new { Name = "�X�L�b�v", Feature = ShortcutKeyManager.Features.Player_Skip, },
				new { Name = "�n�߂���Đ�", Feature = ShortcutKeyManager.Features.Player_Replay, },
				new { Name = "��O���Đ�", Feature = ShortcutKeyManager.Features.Player_Previous, },
				new { Name = "�{�����[�����グ��", Feature = ShortcutKeyManager.Features.Volume_Up, },
				new { Name = "�{�����[����������", Feature = ShortcutKeyManager.Features.Volume_Down, },
				new { Name = "�E�B���h�E��\��/��\��", Feature = ShortcutKeyManager.Features.Window_ShowHide, },
				new { Name = "�E�B���h�E����ʉE���ֈړ�", Feature = ShortcutKeyManager.Features.Window_MoveRightDown, },
				new { Name = "�A�v���P�[�V�������I��", Feature = ShortcutKeyManager.Features.App_Exit, },
			};
			foreach(var item in ks)
			{
				var key = ManagerServices.ShortcutKeyManager.GetShortcutKey(item.Feature);
				var keyName = KeySettingListView.GetKeyName(key);
				var item2 = new ListViewItem(item.Name) { Tag = item.Feature, };
				item2.SubItems.Add(new ListViewItem.ListViewSubItem(item2, keyName) { Tag = key, });
				keySetting.Items.Add(item2);
			}
			keySetting.EndUpdate();
		}

		public override void Save()
		{
			foreach(ListViewItem item in keySetting.Items)
			{
				ManagerServices.ShortcutKeyManager.SetShortcutKey(
					(ShortcutKeyManager.Features)item.Tag,
					(Keys)item.SubItems[1].Tag);
			}
		}

	}
}

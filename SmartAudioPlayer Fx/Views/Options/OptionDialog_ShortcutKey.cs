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
				new { Name = "再生モード(ランダム)", Feature = ShortcutKeyManager.Features.PlayMode_Random, },
				new { Name = "再生モード(リスト)", Feature = ShortcutKeyManager.Features.PlayMode_FileName, },
				new { Name = "再生モード(リピート)", Feature = ShortcutKeyManager.Features.PlayMode_Repeat, },
				new { Name = "再生/一時停止", Feature = ShortcutKeyManager.Features.Player_PlayPause, },
				new { Name = "スキップ", Feature = ShortcutKeyManager.Features.Player_Skip, },
				new { Name = "始めから再生", Feature = ShortcutKeyManager.Features.Player_Replay, },
				new { Name = "一つ前を再生", Feature = ShortcutKeyManager.Features.Player_Previous, },
				new { Name = "ボリュームを上げる", Feature = ShortcutKeyManager.Features.Volume_Up, },
				new { Name = "ボリュームを下げる", Feature = ShortcutKeyManager.Features.Volume_Down, },
				new { Name = "ウィンドウを表示/非表示", Feature = ShortcutKeyManager.Features.Window_ShowHide, },
				new { Name = "ウィンドウを画面右下へ移動", Feature = ShortcutKeyManager.Features.Window_MoveRightDown, },
				new { Name = "アプリケーションを終了", Feature = ShortcutKeyManager.Features.App_Exit, },
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

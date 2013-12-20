using System;
using System.Windows.Forms;
using SmartAudioPlayerFx.Player;

namespace SmartAudioPlayerFx.UI.Options
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
				new { Name = "再生モード(ランダム)", Feature = ShortcutKeyService.Features.PlayMode_Random, },
				new { Name = "再生モード(リスト)", Feature = ShortcutKeyService.Features.PlayMode_FileName, },
				new { Name = "再生モード(リピート)", Feature = ShortcutKeyService.Features.PlayMode_Repeat, },
				new { Name = "再生/一時停止", Feature = ShortcutKeyService.Features.Player_PlayPause, },
				new { Name = "スキップ", Feature = ShortcutKeyService.Features.Player_Skip, },
				new { Name = "始めから再生", Feature = ShortcutKeyService.Features.Player_Replay, },
				new { Name = "一つ前を再生", Feature = ShortcutKeyService.Features.Player_Previous, },
				new { Name = "ボリュームを上げる", Feature = ShortcutKeyService.Features.Volume_Up, },
				new { Name = "ボリュームを下げる", Feature = ShortcutKeyService.Features.Volume_Down, },
				new { Name = "ウィンドウを表示/非表示", Feature = ShortcutKeyService.Features.Window_ShowHide, },
				new { Name = "ウィンドウを画面右下へ移動", Feature = ShortcutKeyService.Features.Window_MoveRightDown, },
				new { Name = "アプリケーションを終了", Feature = ShortcutKeyService.Features.App_Exit, },
			};
			foreach(var item in ks)
			{
				var key = ShortcutKeyService.GetShortcutKey(item.Feature);
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
				ShortcutKeyService.SetShortcutKey(
					(ShortcutKeyService.Features)item.Tag,
					(Keys)item.SubItems[1].Tag);
			}
		}

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Quala;
using SmartAudioPlayerFx.UI;

namespace SmartAudioPlayerFx.Player
{
	/// <summary>
	/// HotKeyServiceを使って特定のキーが押された時の反応を管理する
	/// </summary>
	static class ShortcutKeyService
	{
		static Dictionary<Features, Keys> shortcuts;
		public static HotKeyManager HotKeyManager { get; private set; }

		static ShortcutKeyService()
		{
			LogService.AddDebugLog("ShortcutKeyService", "Call ctor.");
			shortcuts = new Dictionary<Features, Keys>();
			HotKeyManager = new HotKeyManager();
		}

		#region Preferences

		public static void SavePreferencesAdd(XElement element)
		{
			LogService.AddDebugLog("ShortcutKeyService", "Call SavePreferencesAdd");
			var elm1 = element.Element("ShortcutKeys");
			if (elm1 == null) { elm1 = new XElement("ShortcutKeys"); element.Add(elm1); }
			var elm2 = elm1.Elements("Item").ToArray();
			shortcuts.Run(i =>
			{
				// 同じFeature属性の値を持つ最後のElementを選択、なければ作る
				var target = elm2.Where(m =>
				{
					return m.Attributes("Feature")
						.Any(n => n.Value.Equals(i.Key.ToString(), StringComparison.CurrentCultureIgnoreCase));
				}).LastOrDefault();
				if (target == null) { target = new XElement("Item"); elm1.Add(target); }
				// 設定
				target.SetAttributeValue("Feature", i.Key);
				target.SetAttributeValue("Modifier", i.Value & Keys.Modifiers);
				target.SetAttributeValue("Key", i.Value & Keys.KeyCode);
			});

		}

		public static void LoadPreferencesApply(XElement element)
		{
			LogService.AddDebugLog("ShortcutKeyService", "Call LoadPreferencesApply");
			HotKeyManager.RemoveAll();
			ResetShortcuts();
			element.GetArrayValues("ShortcutKeys",
				el => new
				{
					Feature = el.GetOrDefaultValue("Feature", Features.None),
					Modifier = el.GetOrDefaultValue("Modifier", Keys.None),
					Key = el.GetOrDefaultValue("Key", Keys.None),
				})
				.Where(i => i.Feature != Features.None && i.Key != Keys.None)
				.Run(i => shortcuts[i.Feature] = (i.Key | i.Modifier));
			//
			shortcuts
				.Where(i => i.Key != Features.None)
				.Where(i => (i.Value & Keys.KeyCode) != Keys.None)
				.Run(i => HotKeyManager.SetHotKey(i.Value, CreateFeatureAction(i.Key)));
		}

		static void ResetShortcuts()
		{
			LogService.AddDebugLog("ShortcutKeyService", "Call resetShortcuts");
			shortcuts.Clear();
			shortcuts = Enum.GetValues(typeof(Features))
				.OfType<Features>()
				.Where(i => i != Features.None)
				.ToDictionary(i => i, i => Keys.None);
			shortcuts[Features.Player_PlayPause] = Keys.Control | Keys.F10;
			shortcuts[Features.Player_Skip] = Keys.Control | Keys.Scroll;
			shortcuts[Features.Volume_Up] = Keys.Alt | Keys.PageUp;
			shortcuts[Features.Volume_Down] = Keys.Alt | Keys.PageDown;
		}

		#endregion

		/// <summary>
		/// 指定機能のショートカットキーを取得する
		/// </summary>
		/// <param name="feature"></param>
		/// <returns></returns>
		public static Keys GetShortcutKey(Features feature)
		{
			LogService.AddDebugLog("ShortcutKeyService", "Call GetShortcutKey: feature={0}", feature);
			if (feature == Features.None) return Keys.None;
			return shortcuts[feature];
		}

		/// <summary>
		/// 指定機能のショートカットキーを設定する
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="key"></param>
		public static void SetShortcutKey(Features feature, Keys key)
		{
			LogService.AddDebugLog("ShortcutKeyService", "Call SetShortcutKey: feature={0}, key={1}", feature, key);
			if (feature == Features.None && ((key & Keys.KeyCode) == Keys.None)) return;
			HotKeyManager.RemoveHotKey(shortcuts[feature]);
			shortcuts[feature] = key;
			HotKeyManager.SetHotKey(key, CreateFeatureAction(feature));
		}

		/// <summary>
		/// 「ウィンドウを表示/非表示」のコマンド反応用イベント
		/// </summary>
		public static event Action Window_ShowHide_Request;

		/// <summary>
		/// 「ウィンドウを画面右下に移動」のコマンド反応用イベント
		/// </summary>
		public static event Action Window_Move_On_RightDown_Request;

		// 指定機能のアクション用デリゲートを作成
		static Action CreateFeatureAction(Features feature)
		{
			LogService.AddDebugLog("ShortcutKeyService", "Call CreateFeatureAction: feature={0}", feature);
			switch (feature)
			{
				case Features.PlayMode_Random:
					return () => UIService.UIThreadInvoke(() => JukeboxService.SetSelectMode(JukeboxService.SelectionMode.Random));
				case Features.PlayMode_FileName:
					return () => UIService.UIThreadInvoke(() => JukeboxService.SetSelectMode(JukeboxService.SelectionMode.Filename));
				case Features.PlayMode_Repeat:
					return () => UIService.UIThreadInvoke(() => JukeboxService.SetIsRepeat(!JukeboxService.IsRepeat));
				case Features.Player_PlayPause:
					return () => UIService.UIThreadInvoke(() => JukeboxService.AudioPlayer.PlayPause());
				case Features.Player_Skip:
					return () => UIService.UIThreadInvoke(() => JukeboxService.SelectNext(true));
				case Features.Player_Replay:
					return () => UIService.UIThreadInvoke(() => JukeboxService.AudioPlayer.Replay());
				case Features.Player_Previous:
					return () => UIService.UIThreadInvoke(() => JukeboxService.SelectPrevious());
				case Features.Volume_Up:
					return () => UIService.UIThreadInvoke(() => JukeboxService.AudioPlayer.SetVolume(JukeboxService.AudioPlayer.Volume + 0.1));
				case Features.Volume_Down:
					return () => UIService.UIThreadInvoke(() => JukeboxService.AudioPlayer.SetVolume(JukeboxService.AudioPlayer.Volume - 0.1));
				case Features.Window_ShowHide:
					return () => UIService.UIThreadInvoke(() => { if (Window_ShowHide_Request != null) { Window_ShowHide_Request(); } });
				case Features.Window_MoveRightDown:
					return () => UIService.UIThreadInvoke(() => { if (Window_Move_On_RightDown_Request != null) { Window_Move_On_RightDown_Request(); } });
				case Features.App_Exit:
					return () => UIService.UIThreadInvoke(() => UIService.PlayerWindow.Close());
			}
			return null;
		}

		#region define

		public enum Features
		{
			None,
			PlayMode_Random,
			PlayMode_FileName,
			PlayMode_Repeat,
			Player_PlayPause,
			Player_Skip,
			Player_Replay,
			Player_Previous,
			Volume_Up,
			Volume_Down,
			Window_ShowHide,
			Window_MoveRightDown,
			App_Exit,
		}

		#endregion
	}

}

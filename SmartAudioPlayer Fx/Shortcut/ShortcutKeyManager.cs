using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Reactive.Bindings.Extensions;
using Quala;
using Quala.Win32;
using Quala.Extensions;
using Quala.Win32.Components;
using SmartAudioPlayerFx.Preferences;
using SmartAudioPlayerFx.MediaPlayer;

namespace SmartAudioPlayerFx.Shortcut
{
	/// <summary>
	/// HotKeyServiceを使って特定のキーが押された時の反応を管理する
	/// </summary>
//	[Require(typeof(XmlPreferencesManager))]
//	[Require(typeof(AudioPlayerManager))]
//	[Require(typeof(JukeboxManager))]
	sealed class ShortcutKeyManager : IDisposable
	{
		#region ctor

		HotKey hotkey = new HotKey();
		Dictionary<Features, Keys> shortcuts = new Dictionary<Features, Keys>();
		readonly CompositeDisposable _disposables;
		AudioPlayerManager _audio_player;
		JukeboxManager _jukebox;

		public ShortcutKeyManager(XmlPreferencesManager preference, AudioPlayerManager audio_player, JukeboxManager jukebox)
		{
			ResetShortcuts();
			_disposables = new CompositeDisposable(hotkey);
			_audio_player = audio_player;
			_jukebox = jukebox;

			preference.PlayerSettings
				.Subscribe(x => LoadPreferences(x))
				.AddTo(_disposables);
			preference.SerializeRequestAsObservable()
				.Subscribe(_ => SavePreferences(preference.PlayerSettings.Value))
				.AddTo(_disposables);
		}
		public void Dispose()
		{
			_disposables.Dispose();
			Window_Move_On_RightDown_Request = null;
			Window_ShowHide_Request = null;
		}

		void ResetShortcuts()
		{
			lock (shortcuts)
			{
				shortcuts.Clear();
				Enum.GetValues(typeof(Features))
					.OfType<Features>()
					.Where(x => x != Features.None)
					.ForEach(x => shortcuts[x] = Keys.None);
				shortcuts[Features.Player_PlayPause] = Keys.Control | Keys.F10;
				shortcuts[Features.Player_Skip] = Keys.Control | Keys.Scroll;
				shortcuts[Features.Volume_Up] = Keys.Alt | Keys.PageUp;
				shortcuts[Features.Volume_Down] = Keys.Alt | Keys.PageDown;
			}
		}

		void LoadPreferences(XElement element)
		{
			lock (shortcuts)
			{
				hotkey.RemoveAll();
				ResetShortcuts();
				element.GetArrayValues("ShortcutKeys",
					el => new
					{
						Feature = el.GetAttributeValueEx("Feature", Features.None),
						Modifier = el.GetAttributeValueEx("Modifier", Keys.None),
						Key = el.GetAttributeValueEx("Key", Keys.None),
					})
					.Where(i => i.Feature != Features.None && i.Key != Keys.None)
					.ForEach(i => shortcuts[i.Feature] = (i.Key | i.Modifier));
				//
				shortcuts
					.Where(i => i.Key != Features.None)
					.Where(i => (i.Value & Keys.KeyCode) != Keys.None)
					.ForEach(i => hotkey.SetHotKey(i.Value, CreateFeatureAction(i.Key)));
			}
		}
		void SavePreferences(XElement element)
		{
			element.SubElement("ShortcutKeys", true, elm =>
			{
				elm.RemoveAll();
				lock (shortcuts)
				{
					shortcuts.ForEach(i =>
					{
						elm
							// 同じFeature属性の値を持つ最後のElementを選択、なければ作る
							.GetOrCreateElement("Item",
								m => m.Attributes("Feature")
									.Any(n => string.Equals(n.Value, i.Key.ToString(), StringComparison.CurrentCultureIgnoreCase)))
							.SetAttributeValueEx("Feature", i.Key)
							.SetAttributeValueEx("Modifier", i.Value & Keys.Modifiers)
							.SetAttributeValueEx("Key", i.Value & Keys.KeyCode);
					});
				}
			});
		}

		#endregion

		/// <summary>
		/// 指定機能のショートカットキーを取得する
		/// </summary>
		/// <param name="feature"></param>
		/// <returns></returns>
		public Keys GetShortcutKey(Features feature)
		{
			App.Services.GetInstance<LogManager>().AddDebugLog($"Call GetShortcutKey: feature={feature}");

			if (feature == Features.None) return Keys.None;
			lock (shortcuts)
			{
				return shortcuts[feature];
			}
		}

		/// <summary>
		/// 指定機能のショートカットキーを設定する
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="key"></param>
		public void SetShortcutKey(Features feature, Keys key)
		{
			App.Services.GetInstance<LogManager>().AddDebugLog($"Call SetShortcutKey: feature={feature}, key={key}");

			if (feature == Features.None && ((key & Keys.KeyCode) == Keys.None)) return;
			lock (shortcuts)
			{
				hotkey.RemoveHotKey(shortcuts[feature]);
				shortcuts[feature] = key;
				hotkey.SetHotKey(key, CreateFeatureAction(feature));
			}
		}

		/// <summary>
		/// 「ウィンドウを表示/非表示」のコマンド反応用イベント
		/// </summary>
		public event Action Window_ShowHide_Request;

		/// <summary>
		/// 「ウィンドウを画面右下に移動」のコマンド反応用イベント
		/// </summary>
		public event Action Window_Move_On_RightDown_Request;

		public Keys LastDownKey
		{
			get
			{
				return hotkey.LastDownKey;
			}
		}

		public bool SuspressKeyEvent
		{
			get
			{
				return hotkey.SuspressKeyEvent;
			}
			set
			{
				hotkey.SuspressKeyEvent = value;
			}
		}

		// 指定機能のアクション用デリゲートを作成
		Action CreateFeatureAction(Features feature)
		{
			App.Services.GetInstance<LogManager>().AddDebugLog($"Call CreateFeatureAction: feature={feature}");
			switch (feature)
			{
				case Features.PlayMode_Random:
					return () => _jukebox.SelectMode.Value = JukeboxManager.SelectionMode.Random;
				case Features.PlayMode_FileName:
					return () => _jukebox.SelectMode.Value = JukeboxManager.SelectionMode.Filename;
				case Features.PlayMode_Repeat:
					return () => _jukebox.IsRepeat.Value = (!_jukebox.IsRepeat.Value);
				case Features.Player_PlayPause:
					return () => _audio_player.PlayPause();
				case Features.Player_Skip:
					return () => _jukebox.SelectNext(true);
				case Features.Player_Replay:
					return () => _audio_player.Replay();
				case Features.Player_Previous:
					return () => _jukebox.SelectPrevious();
				case Features.Volume_Up:
					return () => _audio_player.Volume = _audio_player.Volume + 0.1;
				case Features.Volume_Down:
					return () => _audio_player.Volume = _audio_player.Volume - 0.1;
				case Features.Window_ShowHide:
					return () => Window_ShowHide_Request?.Invoke();
				case Features.Window_MoveRightDown:
					return () => Window_Move_On_RightDown_Request?.Invoke();
				case Features.App_Exit:
					return () => App.Current?.Shutdown();
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

		/// <summary>
		/// キーを押されたときにデリゲートを呼び出す
		/// </summary>
		sealed class HotKey : IDisposable
		{
			readonly KeyboardHook hook;
			readonly Dictionary<Keys, Action> registered_keys;

			/// <summary>
			/// キーイベントが発生しないようにします
			/// </summary>
			public bool SuspressKeyEvent { get; set; }

			/// <summary>
			/// 最後に押されたキー
			/// </summary>
			public Keys LastDownKey { get; private set; }

			public HotKey()
			{
				hook = new KeyboardHook();
				hook.KeyboardHooked += hook_KeyboardHooked;

				registered_keys = new Dictionary<Keys, Action>();
				LastDownKey = Keys.None;
				SuspressKeyEvent = false;
			}

			#region IDisposable

			~HotKey() { Dispose(false); }
			public void Dispose() { Dispose(true); }

			void Dispose(bool disposing)
			{
				if (disposing)
				{
					hook.Dispose();
					GC.SuppressFinalize(this);
				}
			}

			#endregion

			/// <summary>
			/// ホットキーを設定します
			/// </summary>
			/// <param name="key">設定されるキー</param>
			/// <param name="action">呼び出されるデリゲート</param>
			public void SetHotKey(Keys key, Action action)
			{
				App.Services.GetInstance<LogManager>().AddDebugLog($"Call SetHotKey: key={key}");
				lock (registered_keys)
				{
					registered_keys[key] = action;
				}
			}

			/// <summary>
			/// ホットキーを解除します
			/// </summary>
			/// <param name="key">解除されるキー</param>
			public void RemoveHotKey(Keys key)
			{
				App.Services.GetInstance<LogManager>().AddDebugLog($"Call RemoveHotKey: key={key}");
				lock (registered_keys)
				{
					registered_keys.Remove(key);
				}
			}

			/// <summary>
			/// ホットキーをすべて解除します
			/// </summary>
			public void RemoveAll()
			{
				App.Services.GetInstance<LogManager>().AddDebugLog("Call RemoveAll");
				lock (registered_keys)
				{
					registered_keys.Clear();
				}
			}

			void hook_KeyboardHooked(object sender, KeyboardHook.HookedEventArgs e)
			{
				// 修飾キー(メッセージはアテにならないので)
				bool isCtrl = ((WinAPI.GetAsyncKeyState(0x11) & 0x8000) != 0);		// VK_CONTROL
				bool isShift = ((WinAPI.GetAsyncKeyState(0x10) & 0x8000) != 0);	// VK_SHIFT
				bool isAlt = ((WinAPI.GetAsyncKeyState(0x12) & 0x8000) != 0);		// VK_MENU
				bool isScroll = (e.Key == (Keys)(1 | 2));						// Ctrl+ScrollLockは なんか変
				// 最後に押されたキーの設定
				LastDownKey =
					(e.Key & Keys.KeyCode) |
					(isCtrl ? Keys.Control : Keys.None) |
					(isAlt ? Keys.Alt : Keys.None) |
					(isShift ? Keys.Shift : Keys.None);
				// イベント抑制中 or キーが登録されていないならここで処理中断
				if (e.IsKeyDown || SuspressKeyEvent || registered_keys.Count == 0) return;

				App.Services.GetInstance<LogManager>().AddDebugLog($" **KeyboardHooked: LastDownKey={LastDownKey}");

				lock (registered_keys)
				{
					registered_keys
						.Where(k =>
						{
							var modifier = k.Key & Keys.Modifiers;
							var keycode = k.Key & Keys.KeyCode;

							// ctrl+scrollはなんか変なので別途判定する
							if (keycode == e.Key || ((keycode == Keys.Scroll) && isScroll))
							{
								if (modifier.HasFlag(Keys.Control) != isCtrl) return false;
								if (modifier.HasFlag(Keys.Shift) != isShift) return false;
								if (modifier.HasFlag(Keys.Alt) != isAlt) return false;
								return true;
							}
							return false;
						})
						.ForEach(x =>
						{
							try { x.Value(); }
							catch (Exception ex)
							{
								App.Services.GetInstance<LogManager>().AddErrorLog(
									"キーイベント処理中に例外が発生しました" + Environment.NewLine +
									"キー：" + x.Key.ToString(),
									ex);
							}
						});
				}
			}
		}

		#endregion
	}

	static class ShortcutKeyManagerExtensions
	{
		public static IObservable<Unit> Window_ShowHide_RequestAsObservable(this ShortcutKeyManager manager)
		{
			return Observable.FromEvent(
				v => manager.Window_ShowHide_Request += v,
				v => manager.Window_ShowHide_Request -= v);
		}
		public static IObservable<Unit> Window_Move_On_RightDown_RequestAsObservable(this ShortcutKeyManager manager)
		{
			return Observable.FromEvent(
				v => manager.Window_Move_On_RightDown_Request += v,
				v => manager.Window_Move_On_RightDown_Request -= v);
		}

	}
}

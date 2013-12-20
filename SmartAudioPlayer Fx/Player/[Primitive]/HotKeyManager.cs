using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Quala;
using Quala.Interop.Win32;
using Quala.Windows.Input;

namespace SmartAudioPlayerFx.Player
{
	/// <summary>
	/// キーを押されたときにデリゲートを呼び出す
	/// </summary>
	sealed class HotKeyManager
	{
		ConcurrentDictionary<Keys, Action> registered_keys;
		KeyboardHook hook;

		/// <summary>
		/// キーイベントが発生しないようにします
		/// </summary>
		public bool SuspressKeyEvent { get; set; }

		/// <summary>
		/// 最後に押されたキー
		/// </summary>
		public Keys LastDownKey { get; private set; }

		public HotKeyManager()
		{
			LastDownKey = Keys.None;
			SuspressKeyEvent = false;
			registered_keys = new ConcurrentDictionary<Keys, Action>();
			hook = null;
			PrepareService();
		}

		/// <summary>
		/// サービスを準備します
		/// </summary>
		void PrepareService()
		{
			if (hook != null) return;
			hook = new KeyboardHook();
			hook.KeyboardHooked += hook_KeyboardHooked;
		}

		/// <summary>
		/// ホットキーを設定します
		/// </summary>
		/// <param name="key">設定されるキー</param>
		/// <param name="action">呼び出されるデリゲート</param>
		public void SetHotKey(Keys key, Action action)
		{
			LogService.AddDebugLog("HotKeyManager", "Call SetHotKey: key={0}", key);
			registered_keys.AddOrUpdate(key, action, (k, v) => action);
		}

		/// <summary>
		/// ホットキーを解除します
		/// </summary>
		/// <param name="key">解除されるキー</param>
		public void RemoveHotKey(Keys key)
		{
			LogService.AddDebugLog("HotKeyManager", "Call RemoveHotKey: key={0}", key);
			Action v;
			registered_keys.TryRemove(key, out v);
		}

		/// <summary>
		/// ホットキーをすべて解除します
		/// </summary>
		public void RemoveAll()
		{
			LogService.AddDebugLog("HotKeyManager", "Call RemoveAll");
			registered_keys.Clear();
		}

		void hook_KeyboardHooked(object sender, KeyboardHook.HookedEventArgs e)
		{
			// 修飾キー(メッセージはアテにならないので)
			bool isCtrl = ((API.GetAsyncKeyState(0x11) & 0x8000) != 0);		// VK_CONTROL
			bool isShift = ((API.GetAsyncKeyState(0x10) & 0x8000) != 0);	// VK_SHIFT
			bool isAlt = ((API.GetAsyncKeyState(0x12) & 0x8000) != 0);		// VK_MENU
			bool isScroll = (e.Key == (Keys)(1 | 2));						// Ctrl+ScrollLockは なんか変
			// 最後に押されたキーの設定
			LastDownKey =
				(e.Key & Keys.KeyCode) |
				(isCtrl ? Keys.Control : Keys.None) |
				(isAlt ? Keys.Alt : Keys.None) |
				(isShift ? Keys.Shift : Keys.None);
			// イベント抑制中 or キーが登録されていないならここで処理中断
			if (e.IsKeyDown || SuspressKeyEvent || registered_keys.Count == 0) return;

			LogService.AddDebugLog("HotKeyManager", " **KeyboardHooked: LastDownKey={0}", LastDownKey);
			Task.Factory.StartNew(() =>
			{
				registered_keys
					.AsParallel()
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
					.ForAll(a =>
					{
						try { a.Value(); }
						catch (Exception ex)
						{
							LogService.AddErrorLog("HotKeyManager",
								"キーイベント処理中に例外が発生しました" + Environment.NewLine +
								"キー：" + a.Key.ToString(),
								ex);
						}
					});
			});
		}
	}
}

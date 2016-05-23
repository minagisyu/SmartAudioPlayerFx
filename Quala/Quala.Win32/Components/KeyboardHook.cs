using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Quala.Win32.Components
{
	//
	// �ǂ������R�s�y
	//
	public sealed class KeyboardHook : Component
	{
		IntPtr _hook;
		readonly GCHandle _hookDelegate;
		readonly HookedEventArgs _hookedEventArgs = new HookedEventArgs();
		public event EventHandler<HookedEventArgs> KeyboardHooked;

		public KeyboardHook()
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				throw new PlatformNotSupportedException("Windows 9x�ł̓T�|�[�g����Ă��܂���B");

			using (var curProcess = Process.GetCurrentProcess())
			using (var curModule = curProcess.MainModule)
			{
				var callback = new WinAPI.LowLevelKeyboardProc(CallNextHook);
				var module = WinAPI.GetModuleHandle(curModule.ModuleName);
				_hookDelegate = GCHandle.Alloc(callback);
				_hook = WinAPI.SetWindowsHookEx(WinAPI.WH.KEYBOARD_LL, callback, module, 0);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(_hookDelegate.IsAllocated)
			{
				WinAPI.UnhookWindowsHookEx(_hook);
				_hook = IntPtr.Zero;
				_hookDelegate.Free();
				if(disposing)
				{
					_hook = IntPtr.Zero;
				}
			}

			base.Dispose(disposing);
		}

		int CallNextHook(WinAPI.HC code, WinAPI.WM message, ref WinAPI.KBDLLHOOKSTRUCT state)
		{
			if(code >= WinAPI.HC.ACTION)
			{
				if(KeyboardHooked != null)
				{
					try
					{
						_hookedEventArgs.message = message;
						_hookedEventArgs.state = state;
						_hookedEventArgs.Cancel = false;
						KeyboardHooked(this, _hookedEventArgs);
						if (_hookedEventArgs.Cancel)
						{
							return -1;
						}
					}
					catch (Exception) { }
				}
			}
			return WinAPI.CallNextHookEx(IntPtr.Zero, code, message, ref state);
		}


		public sealed class HookedEventArgs : CancelEventArgs
		{
			internal WinAPI.WM message;
			internal WinAPI.KBDLLHOOKSTRUCT state;

			/// <summary>
			/// �L�[�{�[�h�������ꂽ�������ꂽ�����擾
			/// </summary>
			public bool IsKeyDown
			{
				get { return (message == WinAPI.WM.KEYDOWN || message == WinAPI.WM.SYSKEYDOWN); }
			}

			/// <summary>
			/// ���삳�ꂽ�L�[���擾
			/// </summary>
			public Keys Key { get { return (Keys)state.vkCode; } }

			/// <summary>
			/// ���삳�ꂽ�L�[�̃X�L�����R�[�h���擾
			/// </summary>
			public uint ScanCode { get { return state.scanCode; } }

			/// <summary>
			/// ���삳�ꂽ�L�[���e���L�[�Ȃǂ̊g���L�[���ǂ������擾
			/// </summary>
			public bool IsExtendedKey { get { return (state.flags & 0x01) != 0; } }

			/// <summary>
			/// �C�x���g���C���W�F�N�g���ꂽ���ǂ����擾
			/// </summary>
			public bool IsInjected { get { return (state.flags & 0x10) != 0; } }

			/// <summary>
			/// ALT�L�[��������Ă��邩�ǂ������擾
			/// </summary>
			public bool IsAltDown { get { return (state.flags & 0x20) != 0; } }

			/// <summary>
			/// �L�[�������ꂽ���ǂ������擾
			/// </summary>
			public bool IsUp { get { return (state.flags & 0x80) != 0; } }
		}

	}
}

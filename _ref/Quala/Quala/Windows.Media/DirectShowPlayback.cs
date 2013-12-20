//============================================================================
// 2007.xx.xx
//  - ��������
//
// 2008.09.20
//  - State enum -> PlayState enum
//  - PlayState property -> State property
//
//============================================================================
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Quala.Interop.Win32;
using Quala.Interop.Win32.COM;
using Quala.Windows.Forms;

namespace Quala.Windows.Media
{
	#region =[ DirectShowPlayback ]===========================================

	public sealed partial class DirectShowPlayback : IDisposable
	{
		/// <summary>
		/// Dispose()�̌�ɔ�������C�x���g�B
		/// �f�X�g���N�^����̌Ăяo���͂���܂���B
		/// </summary>
		public event Action DisposeComplate;

		partial void Init_IGraphBuilder();
		partial void Init_IMediaControl();
		partial void Init_IMediaEventEx();
		partial void Init_IMediaPosition();
		partial void Init_IBasicAudio();
		partial void Init_IBasicVideo();
		partial void Init_IVideoWindow();

		public DirectShowPlayback()
		{
			Init_IGraphBuilder();
			Init_IMediaControl();
			Init_IMediaEventEx();
			Init_IMediaPosition();
			Init_IBasicAudio();
			Init_IBasicVideo();
			Init_IVideoWindow();
		}

		~DirectShowPlayback() { Dispose(false); }
		public void Dispose() { Dispose(true); }

		void Dispose(bool disposing)
		{
			if(disposing)
			{
				Close();

				if(DisposeComplate != null)
					DisposeComplate();

				GC.SuppressFinalize(this);
			}
		}

	}

	#endregion
	#region =[ IBasicAudio ]==================================================

	partial class DirectShowPlayback
	{
		IBasicAudio m_basicAudio;
		double m_volume = 1.0;
		double m_balance = 0.0;

		partial void Init_IBasicAudio()
		{
			OpenBegin += () => QueryInterface(out m_basicAudio);
			OpenComplate += () => { Volume = m_volume; Balance = m_balance; };
			CloseComplate += () => m_basicAudio = null;
		}

		/// <summary>
		/// ���̃t�B���^�O���t�ɂ����ăI�[�f�B�I���L�����ǂ����B
		/// IBasicAudio.get_Volume()������ɌĂяo���邩�Ŕ��f���Ă܂��B
		/// </summary>
		public bool IsEnableAudio
		{
			get
			{
				if(m_basicAudio != null)
				{
					// [TODO]
					// �I�[�f�B�I�o�͂����邩���ׂ��������������B
					// set�̂ق��̓I�[�f�B�I�o�͏������葝�₵����H
					try
					{
						int value;
						m_basicAudio.get_Volume(out value);
						return true;
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return false;
			}
		}

		/// <summary>
		/// �{�����[�����擾�ݒ�B�͈͂�0.0(����)�`1.0(�ő�)�ł��B
		/// Open-Close�̉e�����󂯂܂���B
		/// </summary>
		public double Volume
		{
			get
			{
				if(m_basicAudio != null)
				{
					// [MEMO]
					// AC3�p�X�X���[�Ƃ����ƁA�ȉ��̗�O�������B
					// COMException(0x8878001E: DSERR_CONTROLUNAVAIL)
					try
					{
						int value;
						m_basicAudio.get_Volume(out value);
						m_volume = DecibelToVolume(value);
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return m_volume;
			}
			set
			{
				m_volume = value;
				if(m_basicAudio == null) return;
				try { m_basicAudio.put_Volume(VolumeToDecibel(m_volume)); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		/// <summary>
		/// �����o�����X���擾�ݒ�B�͈͂�-1(��)�`1(�E)�B
		/// Open-Close�̉e�����󂯂܂���B
		/// </summary>
		public double Balance
		{
			get
			{
				if(m_basicAudio != null)
				{
					try
					{
						int value;
						m_basicAudio.get_Balance(out value);
						m_balance = value / 10000.0;
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return m_balance;
			}
			set
			{
				m_balance = value;
				if(m_basicAudio == null) return;
				try
				{
					var val = ((int)(m_balance * 10000.0)).Limit(-10000, 10000);
					m_basicAudio.put_Balance(val);
				}
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		//= �{�����[���ƃf�V�x���̕ϊ� =======================================
		// 0.0�`1.0�̃{�����[����-10000�`0�ɕϊ�����
		// �{�����[���ƃf�V�x���l�̕ϊ����悭����Ȃ������̂ŁA�ȉ��̃\�[�X�R�[�h���p�N��(ry
		// http://lamp.sourceforge.jp/lamp/reference/Sound_8cpp-source.html
		//
		static int VolumeToDecibel(double volume)
		{
			volume = volume.Limit(0.0, 1.0);

			// [MEMO]
			// volume==0���AAthlonXP�n��Math.Log10()==-Infinity�Ȃ̂͗ǂ����A
			// �|���Z���Ă�0�ɂȂ��Ă��܂�(�\�z�Ƃ��ẮA����ȃ}�C�i�X�l)
			// ����[�Ȃ��̂ŁA���ʏ����B
			if(volume == 0) return -10000;

			// 10db�Ől�̎��ɕ������鉹�ʂ������ɂȂ�
			// 33.22 = -10db / log10(0.5f)�A100.f��DirectSound��1db = 100.f�Ȃ̂�
			return (int)(33.22 * 100.0 * Math.Log10(volume)).Limit(-10000, 0);
		}

		// -10000�`0�̃{�����[����0.0�`1.0�ɕϊ�����
		static double DecibelToVolume(int decibel)
		{
			decibel = decibel.Limit(-10000, 0);

			// 10db�Ől�̎��ɕ������鉹�ʂ������ɂȂ�
			// 33.22 = -10db / log10(0.5f)�A100.f��DirectSound��1db = 100.f�Ȃ̂�
			return Math.Pow(10, (decibel / 33.22f / 100.0)).Limit(0.0, 1.0);
		}

	}

	#endregion
	#region =[ IBasicVideo ]==================================================

	partial class DirectShowPlayback
	{
		IBasicVideo m_basicVideo;

		partial void Init_IBasicVideo()
		{
			OpenBegin += () => QueryInterface(out m_basicVideo);
			CloseComplate += () => m_basicVideo = null;
		}

		/// <summary>
		/// ���̃t�B���^�O���t�ɂ����ăr�f�I���L�����ǂ����B
		/// IBasicVideo.get_VideoWidth()������ɌĂяo���邩�Ŕ��f���܂��B
		/// </summary>
		public bool IsEnableVideo
		{
			get
			{
				if(m_basicVideo != null)
				{
					try
					{
						int value;
						m_basicVideo.get_VideoWidth(out value);
						return true;
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return false;
			}
		}

	}

	#endregion
	#region =[ IGraphBuilder ]================================================

	partial class DirectShowPlayback
	{
		IGraphBuilder m_graphBuilder;

		/// <summary>
		/// IGraphBuilder.RenderFile()�̑O�ɔ�������C�x���g�B
		/// ���̎��_��QueryInterface()�̗��p���\�ł��B
		/// </summary>
		public event Action OpenBegin;

		/// <summary>
		/// IGraphBuilder.RenderFile()�̌�ɔ�������C�x���g�B
		/// �J���̂Ɏ��s�����Ƃ��͔������Ȃ��B
		/// </summary>
		public event Action OpenComplate;

		/// <summary>
		/// ReleaseComObject()�̑O�ɔ�������C�x���g�B
		/// </summary>
		public event Action CloseBegin;

		/// <summary>
		/// ReleaseComObject()�̌�ɔ�������C�x���g�B
		/// </summary>
		public event Action CloseComplate;

		/*	partial void Init_IGraphBuilder()
			{
			}
		*/
		/// <summary>
		/// IGraphBuilder����COM�I�u�W�F�N�g���擾�B
		/// OpenBegin�C�x���g�`CloseBegin�C�x���g�̊ԂŎg�p�\�B
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		void QueryInterface<T>(out T obj)
		{
			obj = (m_graphBuilder != null) ?
				(T)m_graphBuilder : default(T);
		}

		/// <summary>
		/// �t�@�C�����w�肵�ă��f�B�A���J���܂��B
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public bool Open(string filename)
		{
			if(m_graphBuilder != null) Close();

			// Init
			m_graphBuilder = (IGraphBuilder)
				Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID.FilterGraph));
			if(OpenBegin != null) OpenBegin();

			// RenderFile
			try
			{
				if(m_graphBuilder.RenderFile(filename, null) == COMRESULT.S_OK)
				{
					if(OpenComplate != null) OpenComplate();
					return true;
				}
			}
			catch(InvalidCastException) { }
			catch(InvalidComObjectException) { }
			catch(NotImplementedException) { }
			catch(COMException) { }
			return false;
		}

		/// <summary>
		/// ���f�B�A����܂��B
		/// </summary>
		public void Close()
		{
			if(m_graphBuilder == null) return;
			if(CloseBegin != null) CloseBegin();

			// [MEMO]
			// ���X���b�h�Ŏg�p�����Ɠ{����c
			var ret = Marshal.ReleaseComObject(m_graphBuilder);
			if(CloseComplate != null) CloseComplate();

			// �Ƃ肠�����A�ˁB
			if(ret > 0)
				DebugThrow("COM_refcount > 0");
		}

		[Conditional("DEBUG")]
		void DebugThrow(string msg)
		{
			throw new ApplicationException(msg);
		}

	}

	#endregion
	#region =[ IMediaControl ]================================================

	partial class DirectShowPlayback
	{
		IMediaControl m_mediaControl;
		partial void Init_IMediaControl()
		{
			OpenBegin += () => QueryInterface(out m_mediaControl);
			CloseBegin += () => State = PlayState.Stop;
			CloseComplate += () => m_mediaControl = null;
		}

		/// <summary>
		/// �t�B���^�O���t���ꎞ��~��Ԃɂ�����A��~�B
		/// ��~���̃t�B���^�O���t�ւ̑�����r�f�I�E�B���h�E�Ȃǂɔ��f���������ꍇ�ɗ��p����B
		/// </summary>
		public void StopWhenReady()
		{
			if(m_mediaControl == null) return;
			try { m_mediaControl.StopWhenReady(); }
			catch(InvalidCastException) { }
			catch(InvalidComObjectException) { }
			catch(NotImplementedException) { }
			catch(COMException) { }
		}

		/// <summary>
		/// �t�B���^�O���t�̍Đ���Ԃ��擾�ݒ�B
		/// Open-Close�Ńv���p�e�B���ω����܂��B
		/// </summary>
		public PlayState State
		{
			get
			{
				int nativeState = 0;
				if(m_mediaControl != null)
				{
					try { m_mediaControl.GetState(0, out nativeState); }
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return (PlayState)nativeState;
			}
			set
			{
				if(m_mediaControl == null) return;

				switch(value)
				{
					case PlayState.Play:
						try { m_mediaControl.Run(); }
						catch(InvalidCastException) { }
						catch(InvalidComObjectException) { }
						catch(NotImplementedException) { }
						catch(COMException) { }
						break;

					case PlayState.Pause:
						try { m_mediaControl.Pause(); }
						catch(InvalidCastException) { }
						catch(InvalidComObjectException) { }
						catch(NotImplementedException) { }
						catch(COMException) { }
						break;

					case PlayState.Stop:
						try { m_mediaControl.Stop(); }
						catch(InvalidCastException) { }
						catch(InvalidComObjectException) { }
						catch(NotImplementedException) { }
						catch(COMException) { }
						break;
				}
			}
		}

		/// <summary>
		/// �t�B���^�O���t�̍Đ����
		/// </summary>
		public enum PlayState : int
		{
			Stop = 0,
			Pause = 1,
			Play = 2,
		}

	}

	#endregion
	#region =[ IMediaEventEx ]================================================

	partial class DirectShowPlayback
	{
		IMediaEventEx m_mediaEventEx;
		volatile bool m_exitReq = false;

		/// <summary>
		/// �Đ�������ɔ�������C�x���g�B
		/// �ʃX���b�h����Ăяo����܂��B
		/// </summary>
		public event Action PlayComplete;

		/// <summary>
		/// DirectShow�t�B���^�O���t����̃C�x���g�B
		/// �ʃX���b�h����Ă΂�܂��B
		/// </summary>
		public event Action<EC> DirectShowEvent;

		partial void Init_IMediaEventEx()
		{
			Thread thread = null;

			OpenBegin += () => QueryInterface(out m_mediaEventEx);
			OpenComplate += () =>
				{
					m_exitReq = false;
					thread = new Thread(EventWaitingThread);
					thread.Name = "DirectShowPlayback EventListener";
					thread.IsBackground = true;
					thread.Priority = ThreadPriority.BelowNormal;
					thread.Start();
				};
			CloseBegin += () =>
				{
					m_exitReq = true;
					if(thread != null)
						thread.Join();
				};
			CloseComplate += () => m_mediaEventEx = null;
			DirectShowEvent += (evCode) =>
				{
					// [MEMO]
					// ����Đ����AActiveMovie�E�B���h�E������EC_USERABOET�B
					// �������ʓ|�Ȃ̂ŁA�Đ������������Ƃɂ��Ă����B
					switch(evCode)
					{
						case EC.COMPLETE:
						case EC.USERABORT:
							// �Đ�������������Stop�ɂ��Ă���
							State = PlayState.Stop;
							if(PlayComplete != null) PlayComplete();
							break;
					}
				};
		}

		void EventWaitingThread()
		{
			// [MEMO]
			// while���[�v����Dispose()�����ƁA
			// CloseBegin�C�x���g����thread.Join()�̕����Ńf�b�h���b�N�ɂȂ�B
			// �C�x���g�Ȃǂ�BeginInvoke()�Ŕ񓯊��ɌĂяo�����Ƃŉ�������B
			//
			// ���Ȃ݂ɁA�C�x���g�p�f���Q�[�g��BeginInvoke()�̓}���`�L���X�g�o�����A
			// �����̃f���Q�[�g������Ɨ�O�f���̂Ŏg��Ȃ��B
			// ������ new Action(()=> { ...code... }).BeginInvoke(null, null);
			//
			while(m_exitReq == false)
			{
				if(m_mediaEventEx == null) return;

				EC evCode = (EC)0;
				IntPtr param1, param2;
				try
				{
					var ret = m_mediaEventEx.GetEvent(out evCode, out param1, out param2, 100);
					m_mediaEventEx.FreeEventParams(evCode, param1, param2);
					if(ret == COMRESULT.E_ABORT) continue;
				}
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }

				// �C�x���g�ʒm
				if(DirectShowEvent != null)
					new Action(() => DirectShowEvent(evCode)).BeginInvoke(null, null);
			}
		}

		/// <summary>
		/// ���f�B�A���Đ��I������܂ő҂��܂�
		/// </summary>
		/// <returns></returns>
		public EC WaitForCompletion()
		{
			return WaitForCompletion(-1);
		}

		/// <summary>
		/// ���f�B�A���Đ��I�����邩�A�w�肵���^�C���A�E�g�܂ő҂��܂��B
		/// </summary>
		/// <param name="msTimeout"></param>
		/// <returns></returns>
		public EC WaitForCompletion(TimeSpan timeout)
		{
			return WaitForCompletion(timeout.Milliseconds);
		}

		/// <summary>
		/// ���f�B�A���Đ��I�����邩�A�w�肵���^�C���A�E�g�܂ő҂��܂��B
		/// [msTimeout == -1]�͈����Ȃ���WaitForCompletion()�Ɠ����ł��B
		/// </summary>
		/// <param name="msTimeout"></param>
		/// <returns></returns>
		public EC WaitForCompletion(int msTimeout)
		{
			EC code = 0;
			if(m_mediaEventEx != null)
			{
				try { m_mediaEventEx.WaitForCompletion(msTimeout, out code); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
			return code;
		}

	}

	#endregion
	#region =[ IMediaPosition ]===============================================

	partial class DirectShowPlayback
	{
		IMediaPosition m_mediaPosition;
		double m_rate = 1.0;

		partial void Init_IMediaPosition()
		{
			OpenBegin += () => QueryInterface(out m_mediaPosition);
			OpenComplate += () => Rate = m_rate;
			CloseComplate += () => m_mediaPosition = null;
		}

		/// <summary>
		/// ���f�B�A�̍Đ��ʒu���擾�ݒ�B
		/// Open-Close�Ńv���p�e�B���ω����܂��B
		/// </summary>
		public TimeSpan Position
		{
			get
			{
				if(m_mediaPosition != null)
				{
					try
					{
						double time;
						m_mediaPosition.get_CurrentPosition(out time);
						return TimeSpan.FromMilliseconds(time * 1000.0);
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return TimeSpan.FromMilliseconds(0);
			}
			set
			{
				if(m_mediaPosition == null) return;
				try
				{
					var time = value.TotalMilliseconds / 1000.0;
					m_mediaPosition.put_CurrentPosition(time);
				}
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		/// <summary>
		/// ���f�B�A�̒������擾���܂��B
		/// Open-Close�Ńv���p�e�B���ω����܂��B
		/// </summary>
		public TimeSpan Duration
		{
			get
			{
				if(m_mediaPosition != null)
				{
					try
					{
						// �ꕔ�A�������ρB
						double length;
						m_mediaPosition.get_Duration(out length);
						return TimeSpan.FromMilliseconds(length * 1000.0);
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return TimeSpan.FromMilliseconds(0);
			}
		}

		/// <summary>
		/// ���f�B�A�̍Đ����[�g���擾�ݒ肵�܂��B
		/// 1.0�œ��{�Đ��A0.5�Ŕ����̑��x�A2.0�Ŕ{���A-1.0�ŋt�Đ��B
		/// Open-Close�̉e�����󂯂܂���B
		/// </summary>
		public double Rate
		{
			get
			{
				if(m_mediaPosition != null)
				{
					try { m_mediaPosition.get_Rate(out m_rate); }
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return m_rate;
			}
			set
			{
				m_rate = value;
				if(m_mediaPosition == null) return;
				try { m_mediaPosition.put_Rate(m_rate); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

	}

	#endregion
	#region =[ IVideoWindow ]=================================================

	partial class DirectShowPlayback
	{
		IntPtr m_owner = IntPtr.Zero;
		IVideoWindow m_videoWindow;
		WindowMessageHandling m_videoWindowHandling = new WindowMessageHandling();
		bool m_autoShow = false;
		bool m_visible = false;
		bool m_fullscreenMode = false;

		partial void Init_IVideoWindow()
		{
			m_videoWindowHandling.WindowMessage += videoWindowHandling_WindowMessage;
			OpenBegin += () => QueryInterface(out m_videoWindow);
			OpenComplate += () =>
				{
					VideoWindowOwner = m_owner;
					IsAutoShow = m_autoShow;
					IsVisible = m_visible;
					IsFullScreenMode = m_fullscreenMode;
				};
			CloseBegin += () => m_videoWindowHandling.ReleaseHandle();
			CloseComplate += () => m_videoWindow = null;
		}

		void videoWindowHandling_WindowMessage(object sender, ref Message m)
		{
			// ActiveMovie��VideoWindow�Ƀ��b�Z�[�W���M
			if(m_videoWindow != null)
			{
				// [MEMO]
				// �t���X�N���[�����E�B���h�E�̐؂�ւ����I���t�߂ňȉ��̗�O�����B
				// mpeg�Đ��������A�ق��̃t�@�C���͖��m�F�B
				// COMException(0x8004023B: VFW_E_UNKNOWN_FILE_TYPE)
				try { m_videoWindow.NotifyOwnerMessage(m.HWnd, m.Msg, m.WParam, m.LParam); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		/// <summary>
		/// �r�f�I��\������E�B���h�E�̏��L�E�B���h�E���擾�ݒ�B
		/// Open-Close�̉e�����󂯂܂���B
		/// </summary>
		public IntPtr VideoWindowOwner
		{
			get
			{
				if(m_videoWindow != null)
				{
					try { m_videoWindow.get_Owner(out m_owner); }
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return m_owner;
			}
			set
			{
				m_owner = value;
				if(m_videoWindow == null) return;
				try
				{
					m_videoWindowHandling.ReleaseHandle();
					if(value != IntPtr.Zero)
					{
						m_videoWindowHandling.AssignHandle(m_owner);

						RECT rc;
						bool bret = API.GetWindowRect(value, out rc);
						m_videoWindow.put_Owner(m_owner);
						m_videoWindow.put_MessageDrain(m_owner);
						m_videoWindow.put_WindowStyle(
							(int)(WS.CHILD | WS.CLIPCHILDREN | WS.CLIPSIBLINGS));
						m_videoWindow.put_Left(0);
						m_videoWindow.put_Top(0);
						m_videoWindow.put_Width(rc.right - rc.left);
						m_videoWindow.put_Height(rc.bottom - rc.top);
					}
					else
					{
						m_videoWindow.put_Owner(IntPtr.Zero);
						m_videoWindow.put_MessageDrain(IntPtr.Zero);
					}
				}
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		/// <summary>
		/// �r�f�I�E�B���h�E�������I�ɕ\�����邩�ǂ����B
		/// Open-Close�̉e�����󂯂܂���B
		/// </summary>
		public bool IsAutoShow
		{
			get
			{
				if(m_videoWindow != null)
				{
					try
					{
						int mode;
						m_videoWindow.get_AutoShow(out mode);
						m_autoShow = (mode != 0);
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return m_autoShow;
			}
			set
			{
				m_autoShow = value;
				if(m_videoWindow == null) return;
				try { m_videoWindow.put_AutoShow((m_autoShow) ? -1 : 0); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		/// <summary>
		/// �r�f�I��\�����邩�ǂ����B
		/// Open-Close�̉e�����󂯂܂���B
		/// </summary>
		public bool IsVisible
		{
			get
			{
				if(m_videoWindow != null)
				{
					try
					{
						int value;
						m_videoWindow.get_Visible(out value);
						m_visible = (value != 0);
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return m_visible;
			}
			set
			{
				m_visible = value;
				if(m_videoWindow == null) return;
				try { m_videoWindow.put_Visible(m_visible ? -1 : 0); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		/// <summary>
		/// �t���X�N���[�����[�h�ɂ��邩�ǂ����B
		/// Open-Close�̉e�����󂯂܂���B
		/// </summary>
		public bool IsFullScreenMode
		{
			get
			{
				if(m_videoWindow != null)
				{
					try
					{
						int mode;
						m_videoWindow.get_FullScreenMode(out mode);
						m_fullscreenMode = (mode != 0);
					}
					catch(InvalidCastException) { }
					catch(InvalidComObjectException) { }
					catch(NotImplementedException) { }
					catch(COMException) { }
				}
				return m_fullscreenMode;
			}
			set
			{
				m_fullscreenMode = value;
				if(m_videoWindow == null) return;
				try { m_videoWindow.put_FullScreenMode((m_fullscreenMode) ? -1 : 0); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

	}

	#endregion
}

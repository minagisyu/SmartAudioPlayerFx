//============================================================================
// 2007.xx.xx
//  - 初期実装
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
		/// Dispose()の後に発生するイベント。
		/// デストラクタからの呼び出しはありません。
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
		/// このフィルタグラフにおいてオーディオが有効かどうか。
		/// IBasicAudio.get_Volume()が正常に呼び出せるかで判断してます。
		/// </summary>
		public bool IsEnableAudio
		{
			get
			{
				if(m_basicAudio != null)
				{
					// [TODO]
					// オーディオ出力があるか調べた方がいいかも。
					// setのほうはオーディオ出力消したり増やしたり？
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
		/// ボリュームを取得設定。範囲は0.0(無音)～1.0(最大)です。
		/// Open-Closeの影響を受けません。
		/// </summary>
		public double Volume
		{
			get
			{
				if(m_basicAudio != null)
				{
					// [MEMO]
					// AC3パススルーとかだと、以下の例外が発生。
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
		/// 音声バランスを取得設定。範囲は-1(左)～1(右)。
		/// Open-Closeの影響を受けません。
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

		//= ボリュームとデシベルの変換 =======================================
		// 0.0～1.0のボリュームを-10000～0に変換する
		// ボリュームとデシベル値の変換がよく解らなかったので、以下のソースコードをパクっ(ry
		// http://lamp.sourceforge.jp/lamp/reference/Sound_8cpp-source.html
		//
		static int VolumeToDecibel(double volume)
		{
			volume = volume.Limit(0.0, 1.0);

			// [MEMO]
			// volume==0時、AthlonXP系でMath.Log10()==-Infinityなのは良いが、
			// 掛け算しても0になってしまう(予想としては、巨大なマイナス値)
			// しゃーないので、特別処理。
			if(volume == 0) return -10000;

			// 10dbで人の耳に聞こえる音量が半分になる
			// 33.22 = -10db / log10(0.5f)、100.fはDirectSoundが1db = 100.fなので
			return (int)(33.22 * 100.0 * Math.Log10(volume)).Limit(-10000, 0);
		}

		// -10000～0のボリュームを0.0～1.0に変換する
		static double DecibelToVolume(int decibel)
		{
			decibel = decibel.Limit(-10000, 0);

			// 10dbで人の耳に聞こえる音量が半分になる
			// 33.22 = -10db / log10(0.5f)、100.fはDirectSoundが1db = 100.fなので
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
		/// このフィルタグラフにおいてビデオが有効かどうか。
		/// IBasicVideo.get_VideoWidth()が正常に呼び出せるかで判断します。
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
		/// IGraphBuilder.RenderFile()の前に発生するイベント。
		/// この時点でQueryInterface()の利用が可能です。
		/// </summary>
		public event Action OpenBegin;

		/// <summary>
		/// IGraphBuilder.RenderFile()の後に発生するイベント。
		/// 開くのに失敗したときは発生しない。
		/// </summary>
		public event Action OpenComplate;

		/// <summary>
		/// ReleaseComObject()の前に発生するイベント。
		/// </summary>
		public event Action CloseBegin;

		/// <summary>
		/// ReleaseComObject()の後に発生するイベント。
		/// </summary>
		public event Action CloseComplate;

		/*	partial void Init_IGraphBuilder()
			{
			}
		*/
		/// <summary>
		/// IGraphBuilderからCOMオブジェクトを取得。
		/// OpenBeginイベント～CloseBeginイベントの間で使用可能。
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
		/// ファイルを指定してメディアを開きます。
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
		/// メディアを閉じます。
		/// </summary>
		public void Close()
		{
			if(m_graphBuilder == null) return;
			if(CloseBegin != null) CloseBegin();

			// [MEMO]
			// 他スレッドで使用中だと怒られる…
			var ret = Marshal.ReleaseComObject(m_graphBuilder);
			if(CloseComplate != null) CloseComplate();

			// とりあえず、ね。
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
		/// フィルタグラフを一時停止状態にした後、停止。
		/// 停止中のフィルタグラフへの操作をビデオウィンドウなどに反映させたい場合に利用する。
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
		/// フィルタグラフの再生状態を取得設定。
		/// Open-Closeでプロパティが変化します。
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
		/// フィルタグラフの再生状態
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
		/// 再生完了後に発生するイベント。
		/// 別スレッドから呼び出されます。
		/// </summary>
		public event Action PlayComplete;

		/// <summary>
		/// DirectShowフィルタグラフからのイベント。
		/// 別スレッドから呼ばれます。
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
					// 動画再生時、ActiveMovieウィンドウを閉じるとEC_USERABOET。
					// 処理が面倒なので、再生完了したことにしておく。
					switch(evCode)
					{
						case EC.COMPLETE:
						case EC.USERABORT:
							// 再生が完了したらStopにしておく
							State = PlayState.Stop;
							if(PlayComplete != null) PlayComplete();
							break;
					}
				};
		}

		void EventWaitingThread()
		{
			// [MEMO]
			// whileループ中にDispose()されると、
			// CloseBeginイベント中のthread.Join()の部分でデッドロックになる。
			// イベントなどをBeginInvoke()で非同期に呼び出すことで解決する。
			//
			// ちなみに、イベント用デリゲートのBeginInvoke()はマルチキャスト出来ず、
			// 複数のデリゲートがあると例外吐くので使わない。
			// 解決策→ new Action(()=> { ...code... }).BeginInvoke(null, null);
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

				// イベント通知
				if(DirectShowEvent != null)
					new Action(() => DirectShowEvent(evCode)).BeginInvoke(null, null);
			}
		}

		/// <summary>
		/// メディアが再生終了するまで待ちます
		/// </summary>
		/// <returns></returns>
		public EC WaitForCompletion()
		{
			return WaitForCompletion(-1);
		}

		/// <summary>
		/// メディアが再生終了するか、指定したタイムアウトまで待ちます。
		/// </summary>
		/// <param name="msTimeout"></param>
		/// <returns></returns>
		public EC WaitForCompletion(TimeSpan timeout)
		{
			return WaitForCompletion(timeout.Milliseconds);
		}

		/// <summary>
		/// メディアが再生終了するか、指定したタイムアウトまで待ちます。
		/// [msTimeout == -1]は引数なしのWaitForCompletion()と等価です。
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
		/// メディアの再生位置を取得設定。
		/// Open-Closeでプロパティが変化します。
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
		/// メディアの長さを取得します。
		/// Open-Closeでプロパティが変化します。
		/// </summary>
		public TimeSpan Duration
		{
			get
			{
				if(m_mediaPosition != null)
				{
					try
					{
						// 一部、長さが変。
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
		/// メディアの再生レートを取得設定します。
		/// 1.0で等倍再生、0.5で半分の速度、2.0で倍速、-1.0で逆再生。
		/// Open-Closeの影響を受けません。
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
			// ActiveMovieのVideoWindowにメッセージ送信
			if(m_videoWindow != null)
			{
				// [MEMO]
				// フルスクリーン→ウィンドウの切り替えが終わる付近で以下の例外発生。
				// mpeg再生時だが、ほかのファイルは未確認。
				// COMException(0x8004023B: VFW_E_UNKNOWN_FILE_TYPE)
				try { m_videoWindow.NotifyOwnerMessage(m.HWnd, m.Msg, m.WParam, m.LParam); }
				catch(InvalidCastException) { }
				catch(InvalidComObjectException) { }
				catch(NotImplementedException) { }
				catch(COMException) { }
			}
		}

		/// <summary>
		/// ビデオを表示するウィンドウの所有ウィンドウを取得設定。
		/// Open-Closeの影響を受けません。
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
		/// ビデオウィンドウを自動的に表示するかどうか。
		/// Open-Closeの影響を受けません。
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
		/// ビデオを表示するかどうか。
		/// Open-Closeの影響を受けません。
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
		/// フルスクリーンモードにするかどうか。
		/// Open-Closeの影響を受けません。
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

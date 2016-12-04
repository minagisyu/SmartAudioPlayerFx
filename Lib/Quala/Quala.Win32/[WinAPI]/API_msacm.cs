using System;
using System.Runtime.InteropServices;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace Quala.Win32
{
	partial class WinAPI
	{
		const string msacm32 = "msacm32.dll";

		/// <summary>
		/// ACM または指定された ACM ドライバをテストして、目的のフォーマットを提供されるソースフォーマットとして推奨します。たとえば、この関数を使って、圧縮フォーマットを解凍できる 1 つまたは複数の有効な PCM フォーマットを調べることができます。
		/// </summary>
		/// <param name="had">推奨される目的のフォーマットについてテストするドライバの、オープンインスタンスのハンドルを指定します。このパラメータに NULL を指定すると、ACM は目的のフォーマットを推奨するための最適なドライバを検索します。</param>
		/// <param name="pwfxSrc">ソースフォーマットを識別する WAVEFORMATEX 構造体のアドレスを指定します。このソースフォーマットには、ACM または指定された ACM ドライバによって目的のフォーマットが推奨されます。 </param>
		/// <param name="pwfxDst">pwfxSrc フォーマットとして推奨される目的のフォーマットを受け取る WAVEFORMATEX 構造体のアドレスを指定します。pwfxDst パラメータで指定する構造体のメンバは初期化が必要になります。初期化されるメンバは、fdwSuggest パラメータの値によって違います。</param>
		/// <param name="cbwfxDst">目的のフォーマットが利用できるサイズをバイト単位で指定します。acmMetrics 関数および acmFormatTagDetails 関数を使うと、指定されたドライバ（またはインストールされたすべての ACM ドライバ）で利用可能なフォーマットに必要な最大サイズを調べることができます。</param>
		/// <param name="fdwSuggest">変換先フォーマットに適合するフラグをセットします。次の値が定義されています。ACM_FORMATSUGGESTF_NCHANNELS ACM_FORMATSUGGESTF_NSAMPLESPERSEC ACM_FORMATSUGGESTF_WBITSPERSAMPLE ACM_FORMATSUGGESTF_WFORMATTAG</param>
		/// <returns></returns>
		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmFormatSuggest(IntPtr had, ref WAVEFORMATEX pwfxSrc,
			ref WAVEFORMATEX pwfxDst, uint cbwfxDst, ACM_FORMATSUGGESTF fdwSuggest);

		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmStreamClose(IntPtr has, uint fdwClose);

		/// <summary>
		/// ACM 変換ストリームを開きます。変換ストリームを使うと、指定されたオーディオフォーマットから別のフォーマットにデータを変換できます。
		/// </summary>
		/// <param name="phas">変換に使える、新しいストリームハンドルを受け取るハンドルのアドレスを指定します。このハンドルは、他の ACM ストリーム変換関数でストリームを識別するために使われます。ACM_STREAMOPENF_QUERY フラグが設定される場合、このパラメータには NULL を指定しなければなりません。</param>
		/// <param name="had">ACM ドライバのハンドルを指定します。このハンドルが指定されている場合、変換ストリームに対して使われる特定のドライバが識別されます。このパラメータに NULL を指定すると、適合するドライバが検索されるまで、インストールされている適切な ACM ドライバがすべて照会されます。</param>
		/// <param name="pwfxSrc">変換元フォーマットを識別する WAVEFORMATEX 構造体のアドレスを指定します。</param>
		/// <param name="pwfxDst">変換先フォーマットを識別する WAVEFORMATEX 構造体のアドレスを指定します。 </param>
		/// <param name="pwfltr">変換ストリームで行うフィルタ操作を識別する WAVEFILTER 構造体のアドレスを指定します。フィルタ操作が不用の場合は、このパラメータには NULL を指定します。フィルタが指定されている場合は、変換元のフォーマット（pwfxSrc）と変換先のフォーマット（pwfxDst）は同じでなければなりません。 </param>
		/// <param name="dwCallback">コールバック関数、ウィンドウハンドル、またはイベントハンドルのアドレスを指定します。コールバック関数が呼び出せるのは、ACM_STREAMOPENF_ASYNC フラグをセットして変換ストリームを開いた場合のみです。コールバック関数への通知は、変換ストリームのオープンまたはクローズ時、および各バッファの変換後に行われます。ACM_STREAMOPENF_ASYNC フラグを設定せずに変換ストリームが開かれた場合は、このパラメータは 0 に設定されます。 </param>
		/// <param name="dwInstance">dwCallback パラメータで指定されるコールバック関数に渡されるユーザーインスタンスデータを指定します。このパラメータはウィンドウコールバックやイベントコールバックでは使いません。ACM_STREAMOPENF_ASYNC フラグをセットせずに変換ストリームが開かれた場合は、このパラメータは 0 に設定されます。</param>
		/// <param name="fdwOpen">変換ストリームを開くためのフラグをセットします。次の値が定義されています。ACM_STREAMOPENF_ASYNC ACM_STREAMOPENF_NONREALTIME ACM_STREAMOPENF_QUERY CALLBACK_EVENT CALLBACK_FUNCTION CALLBACK_WINDOW</param>
		/// <returns></returns>
		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmStreamOpen(out IntPtr phas, IntPtr had,
			ref WAVEFORMATEX pwfxSrc, ref WAVEFORMATEX pwfxDst, IntPtr pwfltr,
			IntPtr dwCallback, IntPtr dwInstance, ACM_STREAMOPENF fdwOpen);

		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmStreamOpen(out IntPtr phas, IntPtr had,
			ref WAVEFORMATEX pwfxSrc, ref WAVEFORMATEX pwfxDst, IntPtr pwfltr,
			acmStreamConvertCallback dwCallback, IntPtr dwInstance, ACM_STREAMOPENF fdwOpen);

		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmStreamOpen(out IntPtr phas, IntPtr had,
			ref WAVEFORMATEX pwfxSrc, ref WAVEFORMATEX pwfxDst, ref WAVEFILTER pwfltr,
			IntPtr dwCallback, IntPtr dwInstance, ACM_STREAMOPENF fdwOpen);

		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmStreamOpen(out IntPtr phas, IntPtr had,
			ref WAVEFORMATEX pwfxSrc, ref WAVEFORMATEX pwfxDst, ref WAVEFILTER pwfltr,
			acmStreamConvertCallback dwCallback, IntPtr dwInstance, ACM_STREAMOPENF fdwOpen);

		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmStreamSize(
			IntPtr has,
			uint cbInput,
			out uint pdwOutputBytes,
			ACM_STREAMSIZEF fdwSize);

		/// <summary>
		/// acmStreamOpen 関数が CALLBACK_FUNCTION フラグをセットした場合に使われる、アプリケーション定義のコールバック関数です。acmStreamConvertCallback はプレースホルダーであり、実際にこの関数名を使う必要はありません。
		/// </summary>
		/// <param name="hdrvr">コールバック関数に関連付けられた ACM 変換ストリームのハンドルが入ります。</param>
		/// <param name="uMsg">ACM 変換ストリームメッセージが入ります。次の値が定義されています。MM_ACM_CLOSE MM_ACM_DONE MM_ACM_OPEN</param>
		/// <param name="dwUser">acmStreamOpen 関数の dwInstance パラメータで指定された、ユーザーインスタンスデータが入ります。</param>
		/// <param name="dw1">メッセージパラメータ 1 です。</param>
		/// <param name="dw2">メッセージパラメータ 2 です。</param>
		/// <remarks>acmDriverAdd 関数、acmDriverRemove 関数および acmDriverPriority 関数は、コールバック関数内から呼び出さないでください。</remarks>
		public delegate void acmStreamConvertCallback(IntPtr hdrvr, MM_ACM uMsg, IntPtr dwUser, IntPtr dw1, IntPtr dw2);


		[Flags]
		public enum ACM_FORMATSUGGESTF : uint
		{
			/// <summary>
			/// pwfxDst パラメータで指定した構造体の wFormatTag メンバが有効です。ACM は、インストールされた適切なドライバをテストし、wFormatTag に適合する送り先フォーマットを推奨します。そうでない場合、失敗します。 
			/// </summary>
			WFORMATTAG = 0x00010000,
			/// <summary>
			/// pwfxDst パラメータで指定する構造体の nChannels メンバが有効です。 ACM は、インストール済みの適切なドライバをテストし、nChannels メンバに適合する変換先フォーマットを推奨します。そうでない場合、失敗します。 
			/// </summary>
			NCHANNELS = 0x00020000,
			/// <summary>
			/// pwfxDst パラメータで指定する構造体の nSamplesPerSec メンバが有効です。ACM は、インストールされた適切なドライバをテストし、nSamplesPerSec に適合する送り先フォーマットを推奨します。そうでない場合、失敗します。 
			/// </summary>
			NSAMPLESPERSEC = 0x00040000,
			/// <summary>
			/// pwfxDst によって指された構造体の wBitsPerSample メンバが有効です。ACM は、インストールされた適切なドライバをテストし、wBitsPerSample に適合する目的のフォーマットを推奨します。そうでな場合、失敗します。 
			/// </summary>
			WBITSPERSAMPLE = 0x00080000,

			TYPEMASK = 0x00FF0000,
		}

		[Flags]
		public enum ACM_STREAMOPENF : uint
		{
			/// <summary>
			/// ACM を照会し、指定の変換がサポートされるかを確認します。変換ストリームは開かれることも、phas パラメータにハンドルが返されることもありません。
			/// </summary>
			QUERY = 0x00000001,
			/// <summary>
			/// ストリーム変換は非同期に実行されます。このフラグが設定されている場合、アプリケーションはコールバック関数を使って、変換ストリームのオープンまたはクローズ時、および各バッファの変換後に通知を受けます。また、ACMSTREAMHEADER 構造体の fdwStatus メンバに ACMSTREAMHEADER_STATUSF_DONE フラグがセットされているかを調べることもできます。
			/// </summary>
			ASYNC = 0x00000002,
			/// <summary>
			/// ACM は、時間の条件を考慮せずにデータを変換します。既定では、ドライバはデータをリアルタイムで変換しようとします。フォーマットによっては、このフラグを指定することでオーディオの品質またはその他の特性が向上します。
			/// </summary>
			NONREALTIME = 0x00000004,
			/// <summary>
			/// dwCallback パラメータはイベントのハンドルです。
			/// </summary>
			EVENT = CALLBACK.EVENT,
			/// <summary>
			/// dwCallback パラメータはコールバックプロシージャアドレスです。関数プロトタイプは acmStreamConvertCallback プロトタイプに準拠していなければなりません。
			/// </summary>
			FUNCTION = CALLBACK.FUNCTION,
			/// <summary>
			/// dwCallback パラメータはウィンドウハンドルです。
			/// </summary>
			WINDOW = CALLBACK.WINDOW,
		}

		[Flags]
		public enum ACM_STREAMSIZEF : uint
		{
			SOURCE = 0x00000000,
			DESTINATION = 0x00000001,
			QUERYMASK = 0x0000000F,
		}

		public enum CALLBACK : uint
		{
			/// <summary>
			/// callback type mask
			/// </summary>
			TYPEMASK = 0x00070000,
			/// <summary>
			/// no callback
			/// </summary>
			NULL = 0x00000000,
			/// <summary>
			/// dwCallback is a HWND
			/// </summary>
			WINDOW = 0x00010000,
			/// <summary>
			/// dwCallback is a HTASK
			/// </summary>
			TASK = 0x00020000,
			/// <summary>
			/// dwCallback is a FARPROC (DRVCALLBACK=acmStreamConvertCallback)
			/// </summary>
			FUNCTION = 0x00030000,
			/// <summary>
			/// thread ID replaces 16 bit task
			/// </summary>
			THREAD = TASK,
			/// <summary>
			/// dwCallback is an EVENT Handle
			/// </summary>
			EVENT = 0x00050000,
		}

		public enum MM_ACM : uint
		{
			/// <summary>
			/// ACM は、has パラメータで指定した変換ストリームのクローズに成功しました。has パラメータで指定したハンドルは、このメッセージの受信以降、無効になります。
			/// </summary>
			CLOSE = MM_STREAM.CLOSE,
			/// <summary>
			/// ACM は、has パラメータで指定したストリームハンドルに対して、lParam1（ACMSTREAMHEADER 構造体へのポインタ）で指定されたバッファの変換に成功しました。
			/// </summary>
			DONE = MM_STREAM.DONE,
			/// <summary>
			/// ACM は has パラメータで指定された変換ストリームのオープンに成功しました。
			/// </summary>
			OPEN = MM_STREAM.OPEN,
		}

		public enum MM_STREAM
		{
			OPEN = 0x3D4,
			CLOSE = 0x3D5,
			DONE = 0x3D6,
			ERROR = 0x3D7,
		}

		public enum MMRESULT
		{
			NOERROR = 0,
			ERROR = 1,
			BADDEVICEID = 2,
			NOTENABLED = 3,
			ALLOCATED = 4,
			/// <summary>
			/// 指定されたハンドルは無効です。
			/// </summary>
			INVALHANDLE = 5,
			NODRIVER = 6,
			NOMEM = 7,
			NOTSUPPORTED = 8,
			BADERRNUM = 9,
			/// <summary>
			/// 少なくとも 1 つのフラグが無効です。
			/// </summary>
			INVALFLAG = 10,
			/// <summary>
			/// 少なくとも 1 つのパラメータが無効です。
			/// </summary>
			INVALPARAM = 11,
			HANDLEBUSY = 12,
			INVALIDALIAS = 13,
			BADDB = 14,
			KEYNOTFOUND = 15,
			READERROR = 16,
			WRITEERROR = 17,
			DELETEERROR = 18,
			VALNOTFOUND = 19,
			NODRIVERCB = 20,
			MOREDATA = 21,
			LASTERROR = 21,
		}

		public enum WAVE_FORMAT : ushort
		{
			PCM = 1,
		}


		public struct WAVEFILTER
		{
			/// <summary>
			/// Size of the filter in bytes
			/// </summary>
			public uint cbStruct;
			/// <summary>
			/// filter type
			/// </summary>
			public uint dwFilterTag;
			/// <summary>
			/// Flags for the filter (Universal Dfns)
			/// </summary>
			public uint fdwFilter;
			/// <summary>
			/// Reserved for system use
			/// </summary>
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 5)]
			public uint[] dwReserved;
		}

		public struct WAVEFORMATEX
		{
			/// <summary>format type</summary>
			public WAVE_FORMAT wFormatTag;
			/// <summary>number of channels (i.e. mono, stereo...)</summary>
			public ushort nChannels;
			/// <summary>sample rate</summary>
			public uint nSamplesPerSec;
			/// <summary>for buffer estimation</summary>
			public uint nAvgBytesPerSec;
			/// <summary>block size of data</summary>
			public ushort nBlockAlign;
			/// <summary>number of bits per sample of mono data</summary>
			public ushort wBitsPerSample;
			/// <summary>the count in bytes of the size of extra information (after cbSize)</summary>
			public ushort cbSize;
		}

	}
}

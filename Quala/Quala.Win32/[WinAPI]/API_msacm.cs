using System;
using System.Runtime.InteropServices;

// �uwarning CS0649: �t�B�[���h 'xxx' �͊��蓖�Ă��܂���B��Ɋ���l ���g�p���܂��B�v�̗}���B
#pragma warning disable 649

namespace Quala.Win32
{
	partial class WinAPI
	{
		const string msacm32 = "msacm32.dll";

		/// <summary>
		/// ACM �܂��͎w�肳�ꂽ ACM �h���C�o���e�X�g���āA�ړI�̃t�H�[�}�b�g��񋟂����\�[�X�t�H�[�}�b�g�Ƃ��Đ������܂��B���Ƃ��΁A���̊֐����g���āA���k�t�H�[�}�b�g���𓀂ł��� 1 �܂��͕����̗L���� PCM �t�H�[�}�b�g�𒲂ׂ邱�Ƃ��ł��܂��B
		/// </summary>
		/// <param name="had">���������ړI�̃t�H�[�}�b�g�ɂ��ăe�X�g����h���C�o�́A�I�[�v���C���X�^���X�̃n���h�����w�肵�܂��B���̃p�����[�^�� NULL ���w�肷��ƁAACM �͖ړI�̃t�H�[�}�b�g�𐄏����邽�߂̍œK�ȃh���C�o���������܂��B</param>
		/// <param name="pwfxSrc">�\�[�X�t�H�[�}�b�g�����ʂ��� WAVEFORMATEX �\���̂̃A�h���X���w�肵�܂��B���̃\�[�X�t�H�[�}�b�g�ɂ́AACM �܂��͎w�肳�ꂽ ACM �h���C�o�ɂ���ĖړI�̃t�H�[�}�b�g����������܂��B </param>
		/// <param name="pwfxDst">pwfxSrc �t�H�[�}�b�g�Ƃ��Đ��������ړI�̃t�H�[�}�b�g���󂯎�� WAVEFORMATEX �\���̂̃A�h���X���w�肵�܂��BpwfxDst �p�����[�^�Ŏw�肷��\���̂̃����o�͏��������K�v�ɂȂ�܂��B����������郁���o�́AfdwSuggest �p�����[�^�̒l�ɂ���ĈႢ�܂��B</param>
		/// <param name="cbwfxDst">�ړI�̃t�H�[�}�b�g�����p�ł���T�C�Y���o�C�g�P�ʂŎw�肵�܂��BacmMetrics �֐������ acmFormatTagDetails �֐����g���ƁA�w�肳�ꂽ�h���C�o�i�܂��̓C���X�g�[�����ꂽ���ׂĂ� ACM �h���C�o�j�ŗ��p�\�ȃt�H�[�}�b�g�ɕK�v�ȍő�T�C�Y�𒲂ׂ邱�Ƃ��ł��܂��B</param>
		/// <param name="fdwSuggest">�ϊ���t�H�[�}�b�g�ɓK������t���O���Z�b�g���܂��B���̒l����`����Ă��܂��BACM_FORMATSUGGESTF_NCHANNELS ACM_FORMATSUGGESTF_NSAMPLESPERSEC ACM_FORMATSUGGESTF_WBITSPERSAMPLE ACM_FORMATSUGGESTF_WFORMATTAG</param>
		/// <returns></returns>
		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmFormatSuggest(IntPtr had, ref WAVEFORMATEX pwfxSrc,
			ref WAVEFORMATEX pwfxDst, uint cbwfxDst, ACM_FORMATSUGGESTF fdwSuggest);

		[DllImport(msacm32, CharSet = CharSet.Auto)]
		public static extern MMRESULT acmStreamClose(IntPtr has, uint fdwClose);

		/// <summary>
		/// ACM �ϊ��X�g���[�����J���܂��B�ϊ��X�g���[�����g���ƁA�w�肳�ꂽ�I�[�f�B�I�t�H�[�}�b�g����ʂ̃t�H�[�}�b�g�Ƀf�[�^��ϊ��ł��܂��B
		/// </summary>
		/// <param name="phas">�ϊ��Ɏg����A�V�����X�g���[���n���h�����󂯎��n���h���̃A�h���X���w�肵�܂��B���̃n���h���́A���� ACM �X�g���[���ϊ��֐��ŃX�g���[�������ʂ��邽�߂Ɏg���܂��BACM_STREAMOPENF_QUERY �t���O���ݒ肳���ꍇ�A���̃p�����[�^�ɂ� NULL ���w�肵�Ȃ���΂Ȃ�܂���B</param>
		/// <param name="had">ACM �h���C�o�̃n���h�����w�肵�܂��B���̃n���h�����w�肳��Ă���ꍇ�A�ϊ��X�g���[���ɑ΂��Ďg�������̃h���C�o�����ʂ���܂��B���̃p�����[�^�� NULL ���w�肷��ƁA�K������h���C�o�����������܂ŁA�C���X�g�[������Ă���K�؂� ACM �h���C�o�����ׂďƉ��܂��B</param>
		/// <param name="pwfxSrc">�ϊ����t�H�[�}�b�g�����ʂ��� WAVEFORMATEX �\���̂̃A�h���X���w�肵�܂��B</param>
		/// <param name="pwfxDst">�ϊ���t�H�[�}�b�g�����ʂ��� WAVEFORMATEX �\���̂̃A�h���X���w�肵�܂��B </param>
		/// <param name="pwfltr">�ϊ��X�g���[���ōs���t�B���^��������ʂ��� WAVEFILTER �\���̂̃A�h���X���w�肵�܂��B�t�B���^���삪�s�p�̏ꍇ�́A���̃p�����[�^�ɂ� NULL ���w�肵�܂��B�t�B���^���w�肳��Ă���ꍇ�́A�ϊ����̃t�H�[�}�b�g�ipwfxSrc�j�ƕϊ���̃t�H�[�}�b�g�ipwfxDst�j�͓����łȂ���΂Ȃ�܂���B </param>
		/// <param name="dwCallback">�R�[���o�b�N�֐��A�E�B���h�E�n���h���A�܂��̓C�x���g�n���h���̃A�h���X���w�肵�܂��B�R�[���o�b�N�֐����Ăяo����̂́AACM_STREAMOPENF_ASYNC �t���O���Z�b�g���ĕϊ��X�g���[�����J�����ꍇ�݂̂ł��B�R�[���o�b�N�֐��ւ̒ʒm�́A�ϊ��X�g���[���̃I�[�v���܂��̓N���[�Y���A����ъe�o�b�t�@�̕ϊ���ɍs���܂��BACM_STREAMOPENF_ASYNC �t���O��ݒ肹���ɕϊ��X�g���[�����J���ꂽ�ꍇ�́A���̃p�����[�^�� 0 �ɐݒ肳��܂��B </param>
		/// <param name="dwInstance">dwCallback �p�����[�^�Ŏw�肳���R�[���o�b�N�֐��ɓn����郆�[�U�[�C���X�^���X�f�[�^���w�肵�܂��B���̃p�����[�^�̓E�B���h�E�R�[���o�b�N��C�x���g�R�[���o�b�N�ł͎g���܂���BACM_STREAMOPENF_ASYNC �t���O���Z�b�g�����ɕϊ��X�g���[�����J���ꂽ�ꍇ�́A���̃p�����[�^�� 0 �ɐݒ肳��܂��B</param>
		/// <param name="fdwOpen">�ϊ��X�g���[�����J�����߂̃t���O���Z�b�g���܂��B���̒l����`����Ă��܂��BACM_STREAMOPENF_ASYNC ACM_STREAMOPENF_NONREALTIME ACM_STREAMOPENF_QUERY CALLBACK_EVENT CALLBACK_FUNCTION CALLBACK_WINDOW</param>
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
		/// acmStreamOpen �֐��� CALLBACK_FUNCTION �t���O���Z�b�g�����ꍇ�Ɏg����A�A�v���P�[�V������`�̃R�[���o�b�N�֐��ł��BacmStreamConvertCallback �̓v���[�X�z���_�[�ł���A���ۂɂ��̊֐������g���K�v�͂���܂���B
		/// </summary>
		/// <param name="hdrvr">�R�[���o�b�N�֐��Ɋ֘A�t����ꂽ ACM �ϊ��X�g���[���̃n���h��������܂��B</param>
		/// <param name="uMsg">ACM �ϊ��X�g���[�����b�Z�[�W������܂��B���̒l����`����Ă��܂��BMM_ACM_CLOSE MM_ACM_DONE MM_ACM_OPEN</param>
		/// <param name="dwUser">acmStreamOpen �֐��� dwInstance �p�����[�^�Ŏw�肳�ꂽ�A���[�U�[�C���X�^���X�f�[�^������܂��B</param>
		/// <param name="dw1">���b�Z�[�W�p�����[�^ 1 �ł��B</param>
		/// <param name="dw2">���b�Z�[�W�p�����[�^ 2 �ł��B</param>
		/// <remarks>acmDriverAdd �֐��AacmDriverRemove �֐������ acmDriverPriority �֐��́A�R�[���o�b�N�֐�������Ăяo���Ȃ��ł��������B</remarks>
		public delegate void acmStreamConvertCallback(IntPtr hdrvr, MM_ACM uMsg, IntPtr dwUser, IntPtr dw1, IntPtr dw2);


		[Flags]
		public enum ACM_FORMATSUGGESTF : uint
		{
			/// <summary>
			/// pwfxDst �p�����[�^�Ŏw�肵���\���̂� wFormatTag �����o���L���ł��BACM �́A�C���X�g�[�����ꂽ�K�؂ȃh���C�o���e�X�g���AwFormatTag �ɓK�����鑗���t�H�[�}�b�g�𐄏����܂��B�����łȂ��ꍇ�A���s���܂��B 
			/// </summary>
			WFORMATTAG = 0x00010000,
			/// <summary>
			/// pwfxDst �p�����[�^�Ŏw�肷��\���̂� nChannels �����o���L���ł��B ACM �́A�C���X�g�[���ς݂̓K�؂ȃh���C�o���e�X�g���AnChannels �����o�ɓK������ϊ���t�H�[�}�b�g�𐄏����܂��B�����łȂ��ꍇ�A���s���܂��B 
			/// </summary>
			NCHANNELS = 0x00020000,
			/// <summary>
			/// pwfxDst �p�����[�^�Ŏw�肷��\���̂� nSamplesPerSec �����o���L���ł��BACM �́A�C���X�g�[�����ꂽ�K�؂ȃh���C�o���e�X�g���AnSamplesPerSec �ɓK�����鑗���t�H�[�}�b�g�𐄏����܂��B�����łȂ��ꍇ�A���s���܂��B 
			/// </summary>
			NSAMPLESPERSEC = 0x00040000,
			/// <summary>
			/// pwfxDst �ɂ���Ďw���ꂽ�\���̂� wBitsPerSample �����o���L���ł��BACM �́A�C���X�g�[�����ꂽ�K�؂ȃh���C�o���e�X�g���AwBitsPerSample �ɓK������ړI�̃t�H�[�}�b�g�𐄏����܂��B�����łȏꍇ�A���s���܂��B 
			/// </summary>
			WBITSPERSAMPLE = 0x00080000,

			TYPEMASK = 0x00FF0000,
		}

		[Flags]
		public enum ACM_STREAMOPENF : uint
		{
			/// <summary>
			/// ACM ���Ɖ�A�w��̕ϊ����T�|�[�g����邩���m�F���܂��B�ϊ��X�g���[���͊J����邱�Ƃ��Aphas �p�����[�^�Ƀn���h�����Ԃ���邱�Ƃ�����܂���B
			/// </summary>
			QUERY = 0x00000001,
			/// <summary>
			/// �X�g���[���ϊ��͔񓯊��Ɏ��s����܂��B���̃t���O���ݒ肳��Ă���ꍇ�A�A�v���P�[�V�����̓R�[���o�b�N�֐����g���āA�ϊ��X�g���[���̃I�[�v���܂��̓N���[�Y���A����ъe�o�b�t�@�̕ϊ���ɒʒm���󂯂܂��B�܂��AACMSTREAMHEADER �\���̂� fdwStatus �����o�� ACMSTREAMHEADER_STATUSF_DONE �t���O���Z�b�g����Ă��邩�𒲂ׂ邱�Ƃ��ł��܂��B
			/// </summary>
			ASYNC = 0x00000002,
			/// <summary>
			/// ACM �́A���Ԃ̏������l�������Ƀf�[�^��ϊ����܂��B����ł́A�h���C�o�̓f�[�^�����A���^�C���ŕϊ����悤�Ƃ��܂��B�t�H�[�}�b�g�ɂ���ẮA���̃t���O���w�肷�邱�ƂŃI�[�f�B�I�̕i���܂��͂��̑��̓��������サ�܂��B
			/// </summary>
			NONREALTIME = 0x00000004,
			/// <summary>
			/// dwCallback �p�����[�^�̓C�x���g�̃n���h���ł��B
			/// </summary>
			EVENT = CALLBACK.EVENT,
			/// <summary>
			/// dwCallback �p�����[�^�̓R�[���o�b�N�v���V�[�W���A�h���X�ł��B�֐��v���g�^�C�v�� acmStreamConvertCallback �v���g�^�C�v�ɏ������Ă��Ȃ���΂Ȃ�܂���B
			/// </summary>
			FUNCTION = CALLBACK.FUNCTION,
			/// <summary>
			/// dwCallback �p�����[�^�̓E�B���h�E�n���h���ł��B
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
			/// ACM �́Ahas �p�����[�^�Ŏw�肵���ϊ��X�g���[���̃N���[�Y�ɐ������܂����Bhas �p�����[�^�Ŏw�肵���n���h���́A���̃��b�Z�[�W�̎�M�ȍ~�A�����ɂȂ�܂��B
			/// </summary>
			CLOSE = MM_STREAM.CLOSE,
			/// <summary>
			/// ACM �́Ahas �p�����[�^�Ŏw�肵���X�g���[���n���h���ɑ΂��āAlParam1�iACMSTREAMHEADER �\���̂ւ̃|�C���^�j�Ŏw�肳�ꂽ�o�b�t�@�̕ϊ��ɐ������܂����B
			/// </summary>
			DONE = MM_STREAM.DONE,
			/// <summary>
			/// ACM �� has �p�����[�^�Ŏw�肳�ꂽ�ϊ��X�g���[���̃I�[�v���ɐ������܂����B
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
			/// �w�肳�ꂽ�n���h���͖����ł��B
			/// </summary>
			INVALHANDLE = 5,
			NODRIVER = 6,
			NOMEM = 7,
			NOTSUPPORTED = 8,
			BADERRNUM = 9,
			/// <summary>
			/// ���Ȃ��Ƃ� 1 �̃t���O�������ł��B
			/// </summary>
			INVALFLAG = 10,
			/// <summary>
			/// ���Ȃ��Ƃ� 1 �̃p�����[�^�������ł��B
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

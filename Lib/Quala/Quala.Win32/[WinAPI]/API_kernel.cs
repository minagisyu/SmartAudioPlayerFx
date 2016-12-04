using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

// �uwarning CS0649: �t�B�[���h 'xxx' �͊��蓖�Ă��܂���B��Ɋ���l ���g�p���܂��B�v�̗}���B
#pragma warning disable 649

namespace Quala.Win32
{
	using HANDLE = System.IntPtr;
	using HMODULE = System.IntPtr;
	using DWORD = System.UInt32;

	partial class WinAPI
	{
		const string Kernel32 = "kernel32.dll";

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool AllocConsole();

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern int CloseHandle(IntPtr hObject);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern HANDLE CreateMutex(IntPtr psa, bool bInitialOwner, string pszMutexName);
		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern HANDLE CreateMutex(out SECURITY_ATTRIBUTES psa, bool bInitialOwner, string pszMutexName);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool FreeConsole();

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool FreeLibrary(HMODULE hModule);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern HANDLE GetCurrentProcess();

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern IntPtr GetCurrentThread();

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern DWORD GetLastError();

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, uint nSize);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		/// <summary>
		/// �w��Z�N�V�����̃L�[�̈ꗗ�𓾂�
		/// </summary>
		/// <param name="bufferSize"></param>
		/// <param name="sectionName"></param>
		/// <param name="lpFileName"></param>
		/// <returns></returns>
		public static string[] GetPrivateProfileKeys(uint bufferSize, string sectionName, string lpFileName)
		{
			byte[] ar1 = new byte[bufferSize];
			uint resultSize1 = GetPrivateProfileStringA(
				sectionName, null, "default", ar1, (uint)ar1.Length, lpFileName);
			string result1 = Encoding.Default.GetString(ar1, 0, (int)resultSize1 - 1);
			return result1.Split('\0');
		}

		/// <summary>
		/// �w��t�@�C���̃Z�N�V�����̈ꗗ�𓾂�
		/// </summary>
		/// <param name="bufferSize"></param>
		/// <param name="lpFileName"></param>
		/// <returns></returns>
		public static string[] GetPrivateProfileSections(uint bufferSize, string lpFileName)
		{
			byte[] ar2 = new byte[bufferSize];

			uint resultSize2 = GetPrivateProfileStringA(
				null, null, "default", ar2, (uint)ar2.Length, lpFileName);

			string result2 = Encoding.Default.GetString(ar2, 0, (int)resultSize2 - 1);
			return result2.Split('\0');
		}

		/// <summary>
		/// �w�肳�ꂽ .ini �t�@�C���i�������t�@�C���j�̎w�肳�ꂽ�Z�N�V�������ɂ���A
		/// �w�肳�ꂽ�L�[�Ɋ֘A�t�����Ă��镶������擾���܂��B
		/// </summary>
		/// <param name="lpAppName">�Z�N�V������</param>
		/// <param name="lpKeyName">�L�[��</param>
		/// <param name="lpDefault">����̕�����</param>
		/// <param name="lpReturnedString">��񂪊i�[�����o�b�t�@</param>
		/// <param name="nSize">���o�b�t�@�̃T�C�Y</param>
		/// <param name="lpFileName">.ini �t�@�C���̖��O</param>
		/// <returns>
		/// �֐�����������ƁA�o�b�t�@�Ɋi�[���ꂽ���������Ԃ�܂��i�I�[�� NULL �����͊܂܂Ȃ��j�B
		/// 
		/// lpAppName �� lpKeyName �p�����[�^�̂ǂ���� NULL �ł͂Ȃ��ꍇ�A
		/// �o�b�t�@�̃T�C�Y���s�����āA�v�����ꂽ������S�̂��i�[�ł��Ȃ��ƁA
		/// ������͓r���Ő؂�̂Ă��A�Ō�� 1 �� NULL �������ǉ�����A
		/// �߂�l�� nSize-1 �̒l�ɂȂ�܂��B
		/// 
		/// lpAppName �܂��� lpKeyName �p�����[�^�̂ǂ��炩�� NULL �̏ꍇ�A
		/// �o�b�t�@�̃T�C�Y���s�����āA�v�����ꂽ������S�̂��i�[�ł��Ȃ��ƁA
		/// ������͓r���Ő؂�̂Ă��A�Ō�� 2 �� NULL �������ǉ�����A
		/// �߂�l�� nSize-2 �̒l�ɂȂ�܂��B
		/// </returns>
		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern uint GetPrivateProfileString(
			string lpAppName, string lpKeyName, string lpDefault,
			StringBuilder lpReturnedString, uint nSize, string lpFileName);

		[DllImport(Kernel32, EntryPoint = "GetPrivateProfileStringA", CharSet = CharSet.Ansi)]
		public static extern uint GetPrivateProfileStringA(
			string lpAppName, string lpKeyName, string lpDefault,
			byte[] lpReturnedString, uint nSize, string lpFileName);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern uint GetTickCount();

		[DllImport(Kernel32, CharSet = CharSet.Ansi)]
		public static extern bool GetVersionExA(ref OSVERSIONINFOA lpVersionInformation);
		[DllImport(Kernel32, CharSet = CharSet.Unicode)]
		public static extern bool GetVersionExW(ref OSVERSIONINFOW lpVersionInformation);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern HMODULE LoadLibrary(string lpFileName);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool QueryPerformanceFrequency(out long freq);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool QueryPerformanceCounter(out long count);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool SetPriorityClass(IntPtr hProcess, PriorityClass dwPriorityClass);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool SetProcessWorkingSetSize(
			IntPtr hProcess, int dwMinimumWorkingSetSize,
			int dwMaximumWorkingSetSize);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool SetThreadPriority(HANDLE hThread, THREAD_PRIORITY nPriority);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern WAIT WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern WAIT WaitForSingleObject(HandleRef hHandle, uint dwMilliseconds);

		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool WaitMessage();

		/// <summary>
		/// �w�肳�ꂽ .ini �t�@�C���i�������t�@�C���j�́A�w�肳�ꂽ�Z�N�V�������ɁA
		/// �w�肳�ꂽ�L�[���Ƃ���Ɋ֘A�t����ꂽ��������i�[���܂��B
		/// </summary>
		/// <param name="lpAppName">�Z�N�V������</param>
		/// <param name="lpKeyName">�L�[��</param>
		/// <param name="lpString">�ǉ�����ׂ�������</param>
		/// <param name="lpFileName">.ini �t�@�C��</param>
		/// <returns>
		/// �֐���������� .ini �t�@�C���Ɋi�[���邱�Ƃɐ�������ƁA0 �ȊO�̒l���Ԃ�܂��B
		/// �֐������s���邩�A���O�ɃA�N�Z�X���� .ini �t�@�C�����L���b�V������
		/// �f�B�X�N��̃t�@�C���փt���b�V������i�������ށj�ƁA0 ���Ԃ�܂��B
		/// �g���G���[�����擾����ɂ́AGetLastError �֐����g���܂��B
		/// </returns>
		[DllImport(Kernel32, CharSet = CharSet.Auto)]
		public static extern bool WritePrivateProfileString(
		  string lpAppName, string lpKeyName, string lpString, string lpFileName);

		public enum PriorityClass : int
		{
			NORMAL = 0x00000020,
			IDLE = 0x00000040,
			HIGH = 0x00000080,
			REALTIME = 0x00000100,
			BELOW_NORMAL = 0x00004000,
			ABOVE_NORMAL = 0x00008000,

			// Vista�ȍ~��p
			PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000,
			PROCESS_MODE_BACKGROUND_END = 0x00200000,
		}

		public enum THREAD_PRIORITY : int
		{
			ABOVE_NORMAL = 1,
			BELOW_NORMAL = -1,
			HIGHEST = 2,
			IDLE = -15,
			LOWEST = -2,
			NORMAL = 0,
			TIME_CRITICAL = 15,

			// Vista�ȍ~��p
			THREAD_MODE_BACKGROUND_BEGIN = 0x00010000,
			THREAD_MODE_BACKGROUND_END = 0x00020000,
		}

		public enum WAIT : uint
		{
			/// <summary>
			/// �I�u�W�F�N�g���V�O�i����ԂɂȂ������Ƃ������܂�
			/// </summary>
			OBJECT_0 = 0x00000000,

			/// <summary>
			/// �������ꂽ���߂Ƀ~���[�e�b�N�X�I�u�W�F�N�g���V�O�i����ԂɂȂ������Ƃ������܂�
			/// </summary>
			ABANDONED = 0x00000080,

			/// <summary>
			/// �^�C���A�E�g���Ԃ��o�߂������Ƃ������܂�
			/// </summary>
			TIMEOUT = 0x00000102,

			/// <summary>
			/// �G���[�������������Ƃ������܂��B
			/// �g���G���[�����擾����ɂ́A GetLastError �֐����g���܂��B
			/// </summary>
			FAILED = 0xFFFFFFFF,
		}

		// szCSDVersion������2�ɕ����c�B
		public struct OSVERSIONINFOA
		{
			public uint dwOSVersionInfoSize;
			public uint dwMajorVersion;
			public uint dwMinorVersion;
			public uint dwBuildNumber;
			public uint dwPlatformId;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
			public byte[] szCSDVersion;
		}

		public struct OSVERSIONINFOW
		{
			public uint dwOSVersionInfoSize;
			public uint dwMajorVersion;
			public uint dwMinorVersion;
			public uint dwBuildNumber;
			public uint dwPlatformId;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
			public char[] szCSDVersion;
		}

		public struct SECURITY_ATTRIBUTES
		{
			public uint nLength;
			public IntPtr lpSecurityDescriptor;
			public bool bInheritHandle;
		}

	}
}

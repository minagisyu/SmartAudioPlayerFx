using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
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
		/// 指定セクションのキーの一覧を得る
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
		/// 指定ファイルのセクションの一覧を得る
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
		/// 指定された .ini ファイル（初期化ファイル）の指定されたセクション内にある、
		/// 指定されたキーに関連付けられている文字列を取得します。
		/// </summary>
		/// <param name="lpAppName">セクション名</param>
		/// <param name="lpKeyName">キー名</param>
		/// <param name="lpDefault">既定の文字列</param>
		/// <param name="lpReturnedString">情報が格納されるバッファ</param>
		/// <param name="nSize">情報バッファのサイズ</param>
		/// <param name="lpFileName">.ini ファイルの名前</param>
		/// <returns>
		/// 関数が成功すると、バッファに格納された文字数が返ります（終端の NULL 文字は含まない）。
		/// 
		/// lpAppName と lpKeyName パラメータのどちらも NULL ではない場合、
		/// バッファのサイズが不足して、要求された文字列全体を格納できないと、
		/// 文字列は途中で切り捨てられ、最後に 1 個の NULL 文字が追加され、
		/// 戻り値は nSize-1 の値になります。
		/// 
		/// lpAppName または lpKeyName パラメータのどちらかが NULL の場合、
		/// バッファのサイズが不足して、要求された文字列全体を格納できないと、
		/// 文字列は途中で切り捨てられ、最後に 2 個の NULL 文字が追加され、
		/// 戻り値は nSize-2 の値になります。
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
		/// 指定された .ini ファイル（初期化ファイル）の、指定されたセクション内に、
		/// 指定されたキー名とそれに関連付けられた文字列を格納します。
		/// </summary>
		/// <param name="lpAppName">セクション名</param>
		/// <param name="lpKeyName">キー名</param>
		/// <param name="lpString">追加するべき文字列</param>
		/// <param name="lpFileName">.ini ファイル</param>
		/// <returns>
		/// 関数が文字列を .ini ファイルに格納することに成功すると、0 以外の値が返ります。
		/// 関数が失敗するか、直前にアクセスした .ini ファイルをキャッシュから
		/// ディスク上のファイルへフラッシュする（書き込む）と、0 が返ります。
		/// 拡張エラー情報を取得するには、GetLastError 関数を使います。
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

			// Vista以降専用
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

			// Vista以降専用
			THREAD_MODE_BACKGROUND_BEGIN = 0x00010000,
			THREAD_MODE_BACKGROUND_END = 0x00020000,
		}

		public enum WAIT : uint
		{
			/// <summary>
			/// オブジェクトがシグナル状態になったことを示します
			/// </summary>
			OBJECT_0 = 0x00000000,

			/// <summary>
			/// 放棄されたためにミューテックスオブジェクトがシグナル状態になったことを示します
			/// </summary>
			ABANDONED = 0x00000080,

			/// <summary>
			/// タイムアウト時間が経過したことを示します
			/// </summary>
			TIMEOUT = 0x00000102,

			/// <summary>
			/// エラーが発生したことを示します。
			/// 拡張エラー情報を取得するには、 GetLastError 関数を使います。
			/// </summary>
			FAILED = 0xFFFFFFFF,
		}

		// szCSDVersionが問題で2つに分割…。
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

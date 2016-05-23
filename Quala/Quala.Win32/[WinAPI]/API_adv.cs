using System;
using System.Runtime.InteropServices;

namespace Quala.Win32
{
	partial class WinAPI
	{
		const string Adv32 = "advapi32.dll";

		[DllImport(Adv32, CharSet = CharSet.Auto)]
		public static extern bool AdjustTokenPrivileges(
			IntPtr TokenHandle,
			bool DisableAllPrivileges,
			ref TOKEN_PRIVILEGES NewState,
			int BufferLength,
			out TOKEN_PRIVILEGES PreviousState,
			out int ReturnLength);

		[DllImport(Adv32, CharSet = CharSet.Auto)]
		public static extern bool AdjustTokenPrivileges(
			IntPtr TokenHandle,
			bool DisableAllPrivileges,
			ref TOKEN_PRIVILEGES NewState,
			int BufferLength,
			IntPtr PreviousState,
			IntPtr ReturnLength);

		[DllImport(Adv32, CharSet = CharSet.Auto)]
		public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);

		[DllImport(Adv32, CharSet = CharSet.Auto)]
		public static extern bool OpenProcessToken(IntPtr ProcessHandle, TOKEN DesiredAccess, ref IntPtr TokenHandle);

		[DllImport(Adv32, CharSet = CharSet.Auto)]
		public static extern bool OpenThreadToken(IntPtr ThreadHandle, TOKEN DesiredAccess, bool OpenAsSelf, ref IntPtr TokenHandle);


		/// <summary>
		/// NT Defined Privileges
		/// </summary>
		public static class SE
		{
			public const string CREATE_TOKEN_NAME = "SeCreateTokenPrivilege";
			public const string ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
			public const string LOCK_MEMORY_NAME = "SeLockMemoryPrivilege";
			public const string INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
			public const string UNSOLICITED_INPUT_NAME = "SeUnsolicitedInputPrivilege";
			public const string MACHINE_ACCOUNT_NAME = "SeMachineAccountPrivilege";
			public const string TCB_NAME = "SeTcbPrivilege";
			public const string SECURITY_NAME = "SeSecurityPrivilege";
			public const string TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";
			public const string LOAD_DRIVER_NAME = "SeLoadDriverPrivilege";
			public const string SYSTEM_PROFILE_NAME = "SeSystemProfilePrivilege";
			public const string SYSTEMTIME_NAME = "SeSystemtimePrivilege";
			public const string PROF_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
			public const string INC_BASE_PRIORITY_NAME = "SeIncreaseBasePriorityPrivilege";
			public const string CREATE_PAGEFILE_NAME = "SeCreatePagefilePrivilege";
			public const string CREATE_PERMANENT_NAME = "SeCreatePermanentPrivilege";
			public const string BACKUP_NAME = "SeBackupPrivilege";
			public const string RESTORE_NAME = "SeRestorePrivilege";
			public const string SHUTDOWN_NAME = "SeShutdownPrivilege";
			public const string DEBUG_NAME = "SeDebugPrivilege";
			public const string AUDIT_NAME = "SeAuditPrivilege";
			public const string SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";
			public const string CHANGE_NOTIFY_NAME = "SeChangeNotifyPrivilege";
			public const string REMOTE_SHUTDOWN_NAME = "SeRemoteShutdownPrivilege";
			public const string UNDOCK_NAME = "SeUndockPrivilege";
			public const string SYNC_AGENT_NAME = "SeSyncAgentPrivilege";
			public const string ENABLE_DELEGATION_NAME = "SeEnableDelegationPrivilege";
			public const string MANAGE_VOLUME_NAME = "SeManageVolumePrivilege";
			public const string IMPERSONATE_NAME = "SeImpersonatePrivilege";
			public const string CREATE_GLOBAL_NAME = "SeCreateGlobalPrivilege";
			public const string TRUSTED_CREDMAN_ACCESS_NAME = "SeTrustedCredManAccessPrivilege";
			public const string RELABEL_NAME = "SeRelabelPrivilege";
			public const string INC_WORKING_SET_NAME = "SeIncreaseWorkingSetPrivilege";
			public const string TIME_ZONE_NAME = "SeTimeZonePrivilege";
			public const string CREATE_SYMBOLIC_LINK_NAME = "SeCreateSymbolicLinkPrivilege";
		}


		[Flags]
		public enum SE_PRIVILEGE : int
		{
			ENABLED_BY_DEFAULT = 0x00000001,
			ENABLED = 0x00000002,
			REMOVED = 0X00000004,
			USED_FOR_ACCESS = unchecked((int)0x80000000),
			VALID_ATTRIBUTES = ENABLED_BY_DEFAULT | ENABLED | REMOVED | USED_FOR_ACCESS,
		}

		[Flags]
		public enum TOKEN : int
		{
			ASSIGN_PRIMARY = 0x0001,
			DUPLICATE = 0x0002,
			IMPERSONATE = 0x0004,
			QUERY = 0x0008,
			QUERY_SOURCE = 0x0010,
			ADJUST_PRIVILEGES = 0x0020,
			ADJUST_GROUPS = 0x0040,
			ADJUST_DEFAULT = 0x0080,
			ADJUST_SESSIONID = 0x0100,

			// STANDARD_RIGHTS_REQUIRED (0x000F0000L)
			ALL_ACCESS_P = 0x000F0000 | ASSIGN_PRIMARY | DUPLICATE |
				IMPERSONATE | QUERY | QUERY_SOURCE | ADJUST_PRIVILEGES |
				ADJUST_GROUPS | ADJUST_DEFAULT,
		}


		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct LUID_AND_ATTRIBUTES
		{
			public long Luid;
			public SE_PRIVILEGE Attributes;

			public LUID_AND_ATTRIBUTES(bool initialize)
			{
				Luid = 0;
				Attributes = 0;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct TOKEN_PRIVILEGES
		{
			public int PrivilegeCount;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public LUID_AND_ATTRIBUTES[] Privileges;

			public TOKEN_PRIVILEGES(bool initialize)
			{
				PrivilegeCount = 0;
				Privileges = null;
			}
		}
	}
}

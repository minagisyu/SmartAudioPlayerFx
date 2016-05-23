using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Quala
{
	/// <summary>
	/// �ėp���[�e�B���e�B
	/// </summary>
	public static partial class Tool
	{
		/// <summary>
		/// �Ǘ��Ҍ����������Ă��邩�m�F���܂��B
		/// </summary>
		/// <returns></returns>
		/// <see cref="http://blogs.msdn.com/tsmatsuz/archive/2007/01/25/windows-vista-uac-part-2.aspx"/>
		public static bool IsAdministratorRole()
		{
			var usrId = WindowsIdentity.GetCurrent();
			var p = new WindowsPrincipal(usrId);
			return p.IsInRole(@"BUILTIN\Administrators");
		}

		/// <summary>
		/// �w�肳�ꂽ�p�X���f�B���N�g���p�X���ǂ����m�F���܂��B
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool? IsDirectoryPath(string path)
		{
			try
			{
				FileAttributes attr = File.GetAttributes(path);
				return ((attr & FileAttributes.Directory) != 0);
			}
			catch(FileNotFoundException) { }
			return null;
		}

		/// <summary>
		/// �Ǘ��Ҍ����ŃA�v���P�[�V�������N���B
		/// Vista�ȑO��OS�ł͎��s���܂��B
		/// </summary>
		/// <returns>
		/// true: �����A���݂̃A�v���P�[�V�������I�����Ă��������B
		/// false: ���s�B
		/// </returns>
		public static bool RestartApplicationWithAdministratorRole()
		{
			if(Environment.OSVersion.Version.Major < 6)
				return false;

			try
			{
				Process.Start(new ProcessStartInfo()
				{
					UseShellExecute = true,
					WorkingDirectory = Environment.CurrentDirectory,
					FileName = Assembly.GetEntryAssembly().Location,
					Verb = "runas",
				});
				return true;
			}
			catch { }
			return false;
		}

		/// <summary>
		/// �A�v���P�[�V�������J�n�������s�\�t�@�C���̃f�B���N�g���p�X���擾�B
		/// </summary>
		public static string StartupPath
		{
			get { return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location); }
		}

		/// <summary>
		/// �A�v���P�[�V�������J�n�������s�\�t�@�C����
		/// �f�B���N�g���p�X�Ƀt�@�C������ǉ����ĕԂ��B
		/// </summary>
		public static string StartupPathWith(string filename)
		{
			return Path.Combine(StartupPath, filename);
		}

		// TODO:
		// �uC:\TEST�v�Ȃǂ̏ꏊ�͕W�����[�U�[�ł�����������͂��Ȃ̂ɖ����Ƃ�����c�B
		public static bool IsGotWriteAccessPermission(string path)
		{
			var rule = GetCurrentAccessRule(path);
			return ((rule != null) && ((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write));
		}

		// ���݂̃��[�U�[�������Ă���w��p�X��FileSystemAccessRule�𓾂�
		public static FileSystemAccessRule GetCurrentAccessRule(string path)
		{
			var fileSecurity = File.GetAccessControl(path);
			var rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier)).OfType<FileSystemAccessRule>();
			var currentIdentity = WindowsIdentity.GetCurrent();
			var sids = new[] { currentIdentity.User }.Concat(currentIdentity.Groups);

			// �A�N�Z�X���[�����Ƀ��[�U�[SID������H������΃O���[�vSID���T��
			return rules.FirstOrDefault(rule => sids.Contains(rule.IdentityReference));
		}
	
	}
}

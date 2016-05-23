using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Quala.Win32
{
	partial class WinAPI
	{
		partial class COM
		{
			[ComImport]
			[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
			[Guid("973510db-7d7f-452b-8975-74a85828d354")]
			[SuppressUnmanagedCodeSecurity]
			public interface IFileDialogEvents
			{
				void OnFileOk(IFileDialog pfd);
				void OnFolderChanging(IFileDialog pfd, IShellItem psiFolder);
				void OnFolderChange(IFileDialog pfd);
				void OnSelectionChange(IFileDialog pfd);
				void OnShareViolation(IFileDialog pfd, IShellItem psi,
					out FDE_SHAREVIOLATION_RESPONSE pResponse);
				void OnTypeChange(IFileDialog pfd);
				void OnOverwrite(IFileDialog pfd, IShellItem psi,
					out FDE_OVERWRITE_RESPONSE pResponse);
			}

			public enum FDE_SHAREVIOLATION_RESPONSE
			{
				DEFAULT = 0,
				ACCEPT = 0x1,
				REFUSE = 0x2
			}

			public enum FDE_OVERWRITE_RESPONSE
			{
				DEFAULT = 0,
				ACCEPT = 0x1,
				REFUSE = 0x2
			}
		}
	}
}

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
			[Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
			[SuppressUnmanagedCodeSecurity]
			public interface IFileDialog
			{
				// IModalWindow
				void Show(IntPtr hwndParent);

				// IFileDialog
				void SetFileTypes(uint cFileTypes,
					[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
			COMDLG_FILTERSPEC[] rgFilterSpec);
				void SetFileTypeIndex(uint iFileType);
				void GetFileTypeIndex(out uint piFileType);
				void Advise(IFileDialogEvents pfde, out uint pdwCookie);
				void Unadvise(uint dwCookie);
				void SetOptions(uint fos);
				void GetOptions(out uint pfos);
				void SetDefaultFolder(IShellItem psi);
				void SetFolder(IShellItem psi);
				void GetFolder(out IShellItem ppsi);
				void GetCurrentSelection(out IShellItem ppsi);
				void SetFileName(string pszName);
				void GetFileName(out string pszName);
				void SetTitle(string pszTitle);
				void SetOkButtonLabel(string pszText);
				void SetFileNameLabel(string pszLabel);
				void GetResult(out IShellItem ppsi);
				void AddPlace(IShellItem psi, FDAP fdap);
				void SetDefaultExtension(string pszDefaultExtension);
				void Close(int hr);
				void SetClientGuid(Guid guid);
				void ClearClientData();
				void SetFilter(IShellItemFilter pFilter);
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct COMDLG_FILTERSPEC
			{
				public string pszName;
				public string pszSpec;
			}

			public enum FDAP
			{
				FDAP_BOTTOM = 0,
				FDAP_TOP = 0x1
			}
		}
	}
}

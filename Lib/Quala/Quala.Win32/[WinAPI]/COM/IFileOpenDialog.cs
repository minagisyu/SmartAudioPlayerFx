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
			[Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
			[SuppressUnmanagedCodeSecurity]
			public interface IFileOpenDialog
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
				void SetOptions(FILEOPENDIALOGOPTIONS fos);
				void GetOptions(out FILEOPENDIALOGOPTIONS pfos);
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

				// IFileOpenDialog
				void GetResults(out IShellItemArray ppenum);
				void GetSelectedItems(out IShellItemArray ppsai);
			}

			[Flags]
			public enum FILEOPENDIALOGOPTIONS : uint
			{
				OVERWRITEPROMPT = 0x2,
				STRICTFILETYPES = 0x4,
				NOCHANGEDIR = 0x8,
				PICKFOLDERS = 0x20,
				FORCEFILESYSTEM = 0x40,
				ALLNONSTORAGEITEMS = 0x80,
				NOVALIDATE = 0x100,
				ALLOWMULTISELECT = 0x200,
				PATHMUSTEXIST = 0x800,
				FILEMUSTEXIST = 0x1000,
				CREATEPROMPT = 0x2000,
				SHAREAWARE = 0x4000,
				NOREADONLYRETURN = 0x8000,
				NOTESTFILECREATE = 0x10000,
				HIDEMRUPLACES = 0x20000,
				HIDEPINNEDPLACES = 0x40000,
				NODEREFERENCELINKS = 0x100000,
				DONTADDTORECENT = 0x2000000,
				FORCESHOWHIDDEN = 0x10000000,
				DEFAULTNOMINIMODE = 0x20000000,
				FORCEPREVIEWPANEON = 0x40000000
			}
		}
	}
}

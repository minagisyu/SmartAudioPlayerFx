using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.IO;

namespace Quala.Win32
{
	partial class WinAPI
	{
		const string Shell32 = "shell32.dll";

		[DllImport(Shell32)]
		public static extern IntPtr ILCombine(IntPtr pIDLParent, IntPtr pIDLChild);

		[DllImport(Shell32)]
		public static extern int SHGetDesktopFolder(out COM.IShellFolder ppshf);

		[DllImport(Shell32)]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttribs, out SHFILEINFO psfi, uint cbFileInfo, SHGFI uFlags);

		[DllImport(Shell32)]
		public static extern IntPtr SHGetFileInfo(IntPtr pIDL, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, SHGFI uFlags);

		[DllImport(Shell32)]
		public static extern int SHGetPathFromIDList(IntPtr pIDL, StringBuilder strPath);

		[DllImport(Shell32)]
		public static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, CSIDL nFolder, out IntPtr ppidl);

		// 2番目の引数(PIDLIST_ABSOLUTE* ppidl)は型宣言が面倒なので手抜きで。
		// 戻り値はHRESULTだけど…。
		[DllImport(Shell32)]
		public static extern int SHILCreateFromPath(string pszPath, out IntPtr ppidl, ref uint rgflnOut);

		// 1番目の引数(PIDLIST_ABSOLUTE pidlParent)は型宣言が面倒なので手抜きで。
		// 2番目の引数(IShellFolder psfParent)は型宣言が面倒なので手抜きで。
		// 3番目の引数(PCUITEMID_CHILD pidl)は型宣言が面倒なので手抜きで。
		// 戻り値はHRESULTだけど…。
		[DllImport(Shell32)]
		public static extern int SHCreateShellItem(IntPtr pidlParent, IntPtr psfParent, IntPtr pidl, out COM.IShellItem ppsi);

		// SHCreateShellItemがWin7x64で変？ こっちはVista専用
		[DllImport(Shell32)]
		private static extern int SHCreateItemFromParsingName(string pszPath, COM.IBindCtx pbc, ref Guid riid, out COM.IShellItem ppsi);

		/// <summary>
		/// ファイルパスからIShellItemを得るためのヘルパーメソッド
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static COM.IShellItem GetShellItemFromPath(string path)
		{
			COM.IShellItem ppsi = null;
			IntPtr ppIdl = IntPtr.Zero;
			uint rgflnOut = 0;

			var ret = SHILCreateFromPath(path, out ppIdl, ref rgflnOut);
			if (ret < 0)
				ret = SHCreateShellItem(IntPtr.Zero, IntPtr.Zero, ppIdl, out ppsi);
			if (ret < 0)
			{
				if (Environment.OSVersion.Version.Major >= 6)
				{
					// IShellItemのGUID(IID)
					var guid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
					guid = typeof(COM.IShellItem).GUID;
					ret = SHCreateItemFromParsingName(path, null, ref guid, out ppsi);
				}
			}

			if (ret < 0)
				throw new FileNotFoundException();

			return ppsi;
		}

		[Flags]
		public enum CSIDL : uint
		{
			DESKTOP = 0x0000,
			WINDOWS = 0x0024
		}

		[Flags]
		public enum SHGFI : uint
		{
			ICON = 0x000000100,
			DISPLAYNAME = 0x000000200,
			TYPENAME = 0x000000400,
			ATTRIBUTES = 0x000000800,
			ICONLOCATION = 0x000001000,
			EXETYPE = 0x000002000,
			SYSICONINDEX = 0x000004000,
			LINKOVERLAY = 0x000008000,
			SELECTED = 0x000010000,
			ATTR_SPECIFIED = 0x000020000,
			LARGEICON = 0x000000000,
			SMALLICON = 0x000000001,
			OPENICON = 0x000000002,
			SHELLICONSIZE = 0x000000004,
			PIDL = 0x000000008,
			USEFILEATTRIBUTES = 0x000000010,
			ADDOVERLAYS = 0x000000020,
			OVERLAYINDEX = 0x000000040
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}
	}
}

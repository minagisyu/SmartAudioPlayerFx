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
			[Guid("000214E6-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IShellFolder
			{
				void ParseDisplayName(IntPtr hwnd, IntPtr pbc,
					[In, MarshalAs(UnmanagedType.LPWStr)]  string pszDisplayName,
					out uint pchEaten, out IntPtr ppidl, ref uint pdwAttributes);
				void EnumObjects(IntPtr hwnd, SHCONTF grfFlags, out IEnumIDList ppenumIDList);
				void BindToObject(IntPtr pidl, IntPtr pbc, [In] ref Guid riid, out IShellFolder ppv);
				void BindToStorage(IntPtr pidl, IntPtr pbc, [In] ref Guid riid,
					[MarshalAs(UnmanagedType.Interface)] out object ppv);

				[PreserveSig]
				int CompareIDs(int lParam, IntPtr pidl1, IntPtr pidl2);
				void CreateViewObject(IntPtr hwndOwner, [In] ref Guid riid,
					[MarshalAs(UnmanagedType.Interface)] out object ppv);
				void GetAttributesOf(int cidl, [In] ref IntPtr apidl, ref SFGAO rgfInOut);
				void GetUIObjectOf(IntPtr hwndOwner, int cidl,
					[In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
					[In] ref Guid riid,
					IntPtr rgfReserved,
					[MarshalAs(UnmanagedType.Interface)] out object ppv);
				uint GetDisplayNameOf(IntPtr pidl, SHGDN uFlags, out STRRET pName);
				uint SetNameOf(IntPtr hwnd, IntPtr pidl,
					[In, MarshalAs(UnmanagedType.LPWStr)] string pszName,
					SHGDN uFlags, out IntPtr ppidlOut);
			}

			[Flags]
			public enum SHCONTF : uint
			{
				FOLDERS = 0x0020,              // Only want folders enumerated (SFGAO_FOLDER)
				NONFOLDERS = 0x0040,           // Include non folders
				INCLUDEHIDDEN = 0x0080,        // Show items normally hidden
				INIT_ON_FIRST_NEXT = 0x0100,   // Allow EnumObject() to return before validating enum
				NETPRINTERSRCH = 0x0200,       // Hint that client is looking for printers
				SHAREABLE = 0x0400,            // Hint that client is looking sharable resources (remote shares)
				STORAGE = 0x0800,              // Include all items with accessible storage and their ancestors
			}

			[Flags]
			public enum SFGAO : uint
			{
				CANCOPY = 0x1,                   // Objects can be copied  (DROPEFFECT_COPY)
				CANMOVE = 0x2,                   // Objects can be moved   (DROPEFFECT_MOVE)
				CANLINK = 0x4,                   // Objects can be linked  (DROPEFFECT_LINK)
				STORAGE = 0x00000008,            // Supports BindToObject(IID_IStorage)
				CANRENAME = 0x00000010,          // Objects can be renamed
				CANDELETE = 0x00000020,          // Objects can be deleted
				HASPROPSHEET = 0x00000040,       // Objects have property sheets
				DROPTARGET = 0x00000100,         // Objects are drop target
				CAPABILITYMASK = 0x00000177,
				ENCRYPTED = 0x00002000,          // Object is encrypted (use alt color)
				ISSLOW = 0x00004000,             // 'Slow' object
				GHOSTED = 0x00008000,            // Ghosted icon
				LINK = 0x00010000,               // Shortcut (link)
				SHARE = 0x00020000,              // Shared
				READONLY = 0x00040000,           // Read-only
				HIDDEN = 0x00080000,             // Hidden object
				DISPLAYATTRMASK = 0x000FC000,
				FILESYSANCESTOR = 0x10000000,    // May contain children with SFGAO_FILESYSTEM
				FOLDER = 0x20000000,             // Support BindToObject(IID_IShellFolder)
				FILESYSTEM = 0x40000000,         // Is a win32 file system object (file/folder/root)
				HASSUBFOLDER = 0x80000000,       // May contain children with SFGAO_FOLDER
				CONTENTSMASK = 0x80000000,
				VALIDATE = 0x01000000,           // Invalidate cached information
				REMOVABLE = 0x02000000,          // Is this removeable media?
				COMPRESSED = 0x04000000,         // Object is compressed (use alt color)
				BROWSABLE = 0x08000000,          // Supports IShellFolder, but only implements CreateViewObject() (non-folder view)
				NONENUMERATED = 0x00100000,      // Is a non-enumerated object
				NEWCONTENT = 0x00200000,         // Should show bold in explorer tree
				CANMONIKER = 0x00400000,         // Defunct
				HASSTORAGE = 0x00400000,         // Defunct
				STREAM = 0x00400000,             // Supports BindToObject(IID_IStream)
				STORAGEANCESTOR = 0x00800000,    // May contain children with SFGAO_STORAGE or SFGAO_STREAM
				STORAGECAPMASK = 0x70C50008,     // For determining storage capabilities, ie for open/save semantics
			}

			[Flags]
			public enum SHGDN : uint
			{
				NORMAL = 0x0000,                 // Default (display purpose)
				INFOLDER = 0x0001,               // Displayed under a folder (relative)
				FOREDITING = 0x1000,             // For in-place editing
				FORADDRESSBAR = 0x4000,          // UI friendly parsing name (remove ugly stuff)
				FORPARSING = 0x8000,             // Parsing name for ParseDisplayName()
			}

			[Flags]
			public enum STRRET : uint
			{
				WSTR = 0,
				OFFSET = 0x1,
				CSTR = 0x2,
			}
		}
	}
}

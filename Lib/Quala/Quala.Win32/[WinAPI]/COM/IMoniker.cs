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
			[Guid("0000000f-0000-0000-C000-000000000046")]
			[SuppressUnmanagedCodeSecurity]
			public interface IMoniker
			{
				// IPersist
				void GetClassID(out Guid pClassID);

				// IPersistStream
				[PreserveSig]
				COMRESULT IsDirty();
				void Load([MarshalAs(UnmanagedType.Interface)] object pStream);
				void Save([MarshalAs(UnmanagedType.Interface)] object pStream,
					[MarshalAs(UnmanagedType.Bool)] bool fClearDirty);
				void GetSizeMax(out long pcbSize);

				// IMoniker
				void BindToObject(IBindCtx pbc, IMoniker pmkToLeft, Guid riidResult,
					[Out, MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 2)]
			object ppvResult);
				void BindToStorage(IBindCtx pbc, IMoniker pmkToLeft, Guid riid,
					[Out, MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 2)]
			object ppvObj);
				void Reduce(IBindCtx pbc, uint dwReduceHowFar,
					ref IMoniker ppmkToLeft, out IMoniker ppmkReduced);
				void ComposeWith(IMoniker pmkRight, bool fOnlyIfNotGeneric, out IMoniker ppmkComposite);
				void Enum(bool fForward, out IEnumMoniker ppenumMoniker);
				void IsEqual(IMoniker pmkOtherMoniker);
				void Hash(out uint pdwHash);
				void IsRunning(IBindCtx pbc, IMoniker pmkToLeft, IMoniker pmkNewlyRunning);
				void GetTimeOfLastChange(IBindCtx pbc, IMoniker pmkToLeft, out FILETIME pFileTime);
				void Inverse(out IMoniker ppmk);
				void CommonPrefixWith(IMoniker pmkOther, out IMoniker ppmkPrefix);
				void RelativePathTo(IMoniker pmkOther, out IMoniker ppmkRelPath);
				void GetDisplayName(IBindCtx pbc, IMoniker pmkToLeft, out string ppszDisplayName);
				void ParseDisplayName(IBindCtx pbc, IMoniker pmkToLeft,
					string pszDisplayName, out uint pchEaten, out IMoniker ppmkOut);
				void IsSystemMoniker(out uint pdwMksys);
			}
		}
	}
}

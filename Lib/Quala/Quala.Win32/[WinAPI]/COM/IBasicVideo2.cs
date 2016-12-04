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
			[InterfaceType(ComInterfaceType.InterfaceIsDual)]
			[Guid("329bb360-f6ea-11d1-9038-00a0c9697298")]
			[SuppressUnmanagedCodeSecurity]
			public interface IBasicVideo2
			{
				// IBasicVideo
				void get_AvgTimePerFrame(out double pAvgTimePerFrame);
				void get_BitRate(out int pBitRate);
				void get_BitErrorRate(out int pBitErrorRate);
				void get_VideoWidth(out int pVideoWidth);
				void get_VideoHeight(out int pVideoHeight);
				void put_SourceLeft(int SourceLeft);
				void get_SourceLeft(out int pSourceLeft);
				void put_SourceWidth(int SourceWidth);
				void get_SourceWidth(out int pSourceWidth);
				void put_SourceTop(int SourceTop);
				void get_SourceTop(out int pSourceTop);
				void put_SourceHeight(int SourceHeight);
				void get_SourceHeight(out int pSourceHeight);
				void put_DestinationLeft(int DestinationLeft);
				void get_DestinationLeft(out int pDestinationLeft);
				void put_DestinationWidth(int DestinationWidth);
				void get_DestinationWidth(out int pDestinationWidth);
				void put_DestinationTop(int DestinationTop);
				void get_DestinationTop(out int pDestinationTop);
				void put_DestinationHeight(int DestinationHeight);
				void get_DestinationHeight(out int pDestinationHeight);
				void SetSourcePosition(int Left, int Top, int Width, int Height);
				void GetSourcePosition(out int pLeft, out int pTop, out int pWidth, out int pHeight);
				void SetDefaultSourcePosition();
				void SetDestinationPosition(int Left, int Top, int Width, int Height);
				void GetDestinationPosition(out int pLeft, out int pTop, out int pWidth, out int pHeight);
				void SetDefaultDestinationPosition();
				void GetVideoSize(out int pWidth, out int pHeight);
				void GetVideoPaletteEntries(int StartIndex, int Entries, out int pRetrieved, out int pPalette);
				void GetCurrentImage(ref int pBufferSize, out int pDIBImage);
				[PreserveSig]
				[return: MarshalAs(UnmanagedType.Bool)]
				bool IsUsingDefaultSource();
				[PreserveSig]
				[return: MarshalAs(UnmanagedType.Bool)]
				bool IsUsingDefaultDestination();

				// IBasicVideo2
				void GetPreferredAspectRatio(out int plAspectX, out int plAspectY);
			}
		}
	}
}

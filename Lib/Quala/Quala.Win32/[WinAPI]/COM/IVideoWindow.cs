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
			[Guid("56a868b4-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IVideoWindow
			{
				void put_Caption(string strCaption);
				void get_Caption(out string strCaption);
				void put_WindowStyle(int WindowStyle);
				void get_WindowStyle(out int WindowStyle);
				void put_WindowStyleEx(int WindowStyleEx);
				void get_WindowStyleEx(out int WindowStyleEx);
				void put_AutoShow(int AutoShow);
				void get_AutoShow(out int AutoShow);
				void put_WindowState(int WindowState);
				void get_WindowState(out int WindowState);
				void put_BackgroundPalette(int BackgroundPalette);
				void get_BackgroundPalette(out int pBackgroundPalette);
				void put_Visible(int Visible);
				void get_Visible(out int pVisible);
				void put_Left(int Left);
				void get_Left(out int pLeft);
				void put_Width(int Width);
				void get_Width(out int pWidth);
				void put_Top(int Top);
				void get_Top(out int pTop);
				void put_Height(int Height);
				void get_Height(out int pHeight);
				void put_Owner(IntPtr Owner);
				void get_Owner(out IntPtr Owner);
				void put_MessageDrain(IntPtr Drain);
				void get_MessageDrain(out IntPtr Drain);
				void get_BorderColor(out int Color);
				void put_BorderColor(int Color);
				void get_FullScreenMode(out int FullScreenMode);
				void put_FullScreenMode(int FullScreenMode);
				void SetWindowForeground(int Focus);
				void NotifyOwnerMessage(IntPtr hwnd, int uMsg, IntPtr wParam, IntPtr lParam);
				void SetWindowPosition(int Left, int Top, int Width, int Height);
				void GetWindowPosition(out int pLeft, out int pTop, out int pWidth, out int pHeight);
				void GetMinIdealImageSize(out int pWidth, out int pHeight);
				void GetMaxIdealImageSize(out int pWidth, out int pHeight);
				void GetRestorePosition(out int pLeft, out int pTop, out int pWidth, out int pHeight);
				void HideCursor(int HideCursor);
				void IsCursorHidden(out int CursorHidden);
			}
		}
	}
}

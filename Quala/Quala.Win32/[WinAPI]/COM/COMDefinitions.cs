using System;
using System.Runtime.InteropServices;

// 「warning CS0649: フィールド 'xxx' は割り当てられません。常に既定値 を使用します。」の抑制。
#pragma warning disable 649

namespace Quala.Win32
{
	partial class WinAPI
	{
		public static partial class COM
		{
			public static class CLSID
			{
				public static readonly Guid FilterGraph = new Guid("e436ebb3-524f-11ce-9f53-0020af0ba770");

				public static readonly Guid FileOperation = new Guid("3ad05575-8857-4850-9277-11b85bdb8e09");
				public static readonly Guid FileOpenDialog = new Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7");
				public static readonly Guid FileSaveDialog = new Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B");
			}

			public static class IID
			{
				public static readonly Guid IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
			}

			[Flags]
			public enum COMRESULT : int
			{
				NOERROR = 0,
				S_OK = 0x00000000,
				S_FALSE = 0x00000001,
				SEVERITY_SUCCESS = 0,
				SEVERITY_ERROR = 1,

				// COM?
				E_UNEXPECTED = unchecked((int)0x8000FFFF),
				E_ABORT = unchecked((int)0x80004004),

				// VFW
				VFW_E_ENUM_OUT_OF_SYNC = unchecked((int)0x80040203),
				VFW_E_NOT_FOUND = unchecked((int)0x80040216),
				VFW_E_ALREADY_CONNECTED = unchecked((int)0x80040204),
				VFW_E_NOT_STOPPED = unchecked((int)0x80040224),
				VFW_E_TYPE_NOT_ACCEPTED = unchecked((int)0x8004022A),
				VFW_E_INVALID_DIRECTION = unchecked((int)0x80040208),
				VFW_E_NOT_CONNECTED = unchecked((int)0x80040209),
				VFW_S_NO_MORE_ITEMS = unchecked((int)0x00040103),
				VFW_E_ALREADY_COMMITTED = unchecked((int)0x8004020F),
				VFW_E_NO_ACCEPTABLE_TYPES = unchecked((int)0x80040207),
			}

			public enum EC : int
			{
				USER = 0x8000,
				COMPLETE = 0x01,
				USERABORT = 0x02,
				ERRORABORT = 0x03,
				TIME = 0x04,
				REPAINT = 0x05,
				STREAM_ERROR_STOPPED = 0x06,
				STREAM_ERROR_STILLPLAYING = 0x07,
				ERROR_STILLPLAYING = 0x08,
				PALETTE_CHANGED = 0x09,
				VIDEO_SIZE_CHANGED = 0x0A,
				QUALITY_CHANGE = 0x0B,
				SHUTTING_DOWN = 0x0C,
				CLOCK_CHANGED = 0x0D,
				PAUSED = 0x0E,
				OPENING_FILE = 0x10,
				BUFFERING_DATA = 0x11,
				FULLSCREEN_LOST = 0x12,
				ACTIVATE = 0x13,
				NEED_RESTART = 0x14,
				WINDOW_DESTROYED = 0x15,
				DISPLAY_CHANGED = 0x16,
				STARVATION = 0x17,
				OLE_EVENT = 0x18,
				NOTIFY_WINDOW = 0x19,
				STREAM_CONTROL_STOPPED = 0x1A,
				STREAM_CONTROL_STARTED = 0x1B,
				END_OF_SEGMENT = 0x1C,
				SEGMENT_STARTED = 0x1D,
				LENGTH_CHANGED = 0x1E,
				DEVICE_LOST = 0x1f,
				SAMPLE_NEEDED = 0x20,
				PROCESSING_LATENCY = 0x21,
				SAMPLE_LATENCY = 0x22,
				SCRUB_TIME = 0x23,
				STEP_COMPLETE = 0x24,
				TIMECODE_AVAILABLE = 0x30,
				EXTDEVICE_MODE_CHANGE = 0x31,
				STATE_CHANGE = 0x32,
				GRAPH_CHANGED = 0x50,
				CLOCK_UNSET = 0x51,
			}

			public enum PIN_DIRECTION
			{
				PINDIR_INPUT = 0,
				PINDIR_OUTPUT = PINDIR_INPUT + 1,
			}


			public struct AM_MEDIA_TYPE
			{
				public Guid majortype;
				public Guid subtype;
				public bool bFixedSizeSamples;
				public bool bTemporalCompression;
				public uint lSampleSize;
				public Guid formattype;
				public IntPtr pUnk; // IUnknown
				public uint cbFormat;
				public IntPtr pbFormat; // size_is
			}

			public struct AM_WMT_EVENT_DATA
			{
				public IntPtr hrStatus;	// status code
				public IntPtr pData;	// event data
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct FILTER_INFO
			{
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
				public string achName;
				public IFilterGraph pGraph;
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			public struct PIN_INFO
			{
				public IBaseFilter pFilter;
				public PIN_DIRECTION dir;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
				public string achName;
			}
		}
	}
}

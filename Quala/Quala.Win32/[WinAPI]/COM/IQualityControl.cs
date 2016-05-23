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
			[Guid("56a868a5-0ad4-11ce-b03a-0020af0ba770")]
			[SuppressUnmanagedCodeSecurity]
			public interface IQualityControl
			{
				void Notify(IBaseFilter pSelf, Quality q);
				void SetSink(IQualityControl piqc);
			}

			public enum QualityMessageType
			{
				Famine = 0,
				Flood = Famine + 1
			}

			public struct Quality
			{
				public QualityMessageType Type;
				public int Proportion;
				public long Late;
				public long TimeStamp;

				public Quality(QualityMessageType type, int proportion, long late, long timeStamp)
				{
					this.Type = type;
					this.Proportion = proportion;
					this.Late = late;
					this.TimeStamp = timeStamp;
				}

			}
		}
	}
}

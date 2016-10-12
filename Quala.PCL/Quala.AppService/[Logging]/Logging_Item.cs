using System;

namespace Quala
{
	partial class Logging
	{
		sealed class Item
		{
			public DateTime Time { get; set; }
			public LogType Type { get; set; }
			public string Source { get; set; }
			public string Message { get; set; }
			public override string ToString() => $"[{Time:G}][{Type}]<{Source}> {Message}";
		}
	}
}

using System;

namespace Quala
{
	partial class LogManager
	{
		sealed class Item
		{
			public DateTime Time { get; set; }
			public Level Level { get; set; }
			public string Source { get; set; }
			public string Message { get; set; }
			public override string ToString() => $"[{Time:G}][{Level}]<{Source}> {Message}";
		}
	}
}

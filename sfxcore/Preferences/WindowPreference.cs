using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sfxcore.Preferences
{
	class WindowPreference
	{
		public object WindowPlacenent { get; set; }
		public int InactiveOpacity { get; set; }
		public int DeactiveOpacity { get; set; }
		public bool IsVisible { get; set; }
		public object DynamicWindowBounds { get; set; }
		public bool IsVisdeoDrawing { get; set; }
		public bool IsSoundFadeEffect { get; set; }
	}
}

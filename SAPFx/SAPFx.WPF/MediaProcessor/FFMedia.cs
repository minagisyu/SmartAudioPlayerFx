using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	public unsafe sealed partial class FFMedia : IDisposable
	{
		static FFMedia()
		{
		}

		public FFMedia()
		{

		}

		#region Dispose

		~FFMedia() => Dispose(false);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		void Dispose(bool disposing)
		{
			if(disposing)
			{
				// マネージリソースの破棄
			}

			// アンマネージリソースの破棄
		}

	}
}

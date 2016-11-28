using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe sealed class VideoDecoder : IDisposable
		{
			public VideoDecoder()
			{

			}

			#region Dispose

			~VideoDecoder() => Dispose(false);

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion

			void Dispose(bool disposing)
			{
				if (disposing)
				{
					// マネージリソースの破棄
				}

				// アンマネージリソースの破棄
			}

		}
	}
}

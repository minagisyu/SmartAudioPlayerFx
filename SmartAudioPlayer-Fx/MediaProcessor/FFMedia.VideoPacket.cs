using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe sealed class VideoPacket : Packet
		{
			public VideoPacket()
			{

			}

			protected override void Dispose(bool disposing)
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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	partial class FFMedia
	{
		public unsafe sealed class AudioDecoder : IDisposable
		{
			public AudioDecoder()
			{

			}

			#region Dispose

			~AudioDecoder() => Dispose(false);

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

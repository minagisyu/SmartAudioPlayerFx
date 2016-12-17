using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartAudioPlayer.MediaProcessor
{
	class FFMediaPlayback
	{
		public event Action<FFMedia.VideoFrame> VideoFrameUpdated;
		public event Action<FFMedia.AudioFrame> AudioFrameUpdated;

		public void PlayAsync()
		{
		}


	}
}

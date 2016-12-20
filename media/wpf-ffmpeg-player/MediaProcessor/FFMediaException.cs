using System;

namespace SmartAudioPlayer.MediaProcessor
{
	public class FFMediaException : Exception
	{
		public FFMediaException() { }
		public FFMediaException(string message) : base(message) { }
		public FFMediaException(string message, Exception inner) : base(message, inner) { }
	}
}

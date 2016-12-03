using OpenAL;
using System;
using System.Threading.Tasks;
using static OpenAL.AL10;
using static OpenAL.ALC10;

namespace SmartAudioPlayer.Sound
{
	partial class ALDevice
	{
		public sealed class Source : IDisposable
		{
			// Source, Buffer (for Streaming Playing)
			readonly ALDevice device;
			readonly int buffer_num;
			bool disposed = false;

			uint source_id;
			uint[] buffers;

			public Source(ALDevice device, int buffer_num)
			{
				this.device = device;
				this.buffer_num = buffer_num;

				buffers = new uint[buffer_num];
				alGenBuffers(buffer_num, buffers);

				alGenSources(1, out source_id);
				if (alGetError() != AL_NO_ERROR)
				{
					Console.WriteLine("Error generating :(");
					return;
				}
			}

			public async void WaitBuffersProcessedAsync()
			{
				// キューがあくのを待機する
				await Task.Delay(1);
			}

			public bool WriteData()
			{
				// 空いてるキューを取り除き、データ書き込みキューに入れる
				// キューに空きがない場合は失敗する
				return false;
			}

			void proc()
			{
				byte* audioBuf = stackalloc byte[192000];//AVCODEC_MAX_AUDIO_FRAME_SIZE];
				int audioBufSize = 0;

				// NUM_BUFFERSの数だけ、あらかじめバッファを準備する。
				int i;
				for (i = 0; i < buffer_num; i++)
				{

					// デコード、変換したデータを、OpenALのバッファに書き込む。
					alBufferData(buffers[i], AL_FORMAT_STEREO16, (IntPtr)audioBuf, audioBufSize, 48000);
					if (alGetError() != AL_NO_ERROR)
					{
						Console.WriteLine("Error Buffer :(");
						continue;
					}
				}

				alSourceQueueBuffers(source_id, buffer_num, buffers);
				alSourcePlay(source_id);
				bool playing = false;
				if (alGetError() != AL_NO_ERROR)
				{
					Console.WriteLine("Error starting.");
					return;
				}
				else
				{
					Console.WriteLine("Playing..");
					playing = true;
				}

				// パケットキューがなくなるまで、繰り返す。
				while (pktQueue.numOfPackets > 0 && playing)
				{

					// 使用済みバッファの数を取得する。
					// 使用済みバッファがない場合は、できるまで繰り返す。
					alGetSourcei(source_id, AL_BUFFERS_PROCESSED, out var val);
					if (val <= 0)
					{
						// 少しスリープさせて処理を減らす。
						/*   struct timespec ts = {0, 1 * 1000000}; // 1msec

						   nanosleep(&ts, NULL);
						  */
						Thread.Sleep(1);
						continue;
					}

					// 再生済みのバッファをデキューする。
					var buffer = default(uint);

					alSourceUnqueueBuffers(source_id, 1, ref buffer);

					// デキューしたバッファに、新しい音楽データを書き込む。
					alBufferData(buffer, AL_FORMAT_STEREO16, (IntPtr)audioBuf, audioBufSize, 48000);
					if (alGetError() != AL_NO_ERROR)
					{

						Console.WriteLine("Error Buffer :(");
						return;
					}

					// 新しいデータを書き込んだバッファをキューする。
					alSourceQueueBuffers(source_id, 1, ref buffer);
					if (alGetError() != AL_NO_ERROR)
					{

						Console.WriteLine("Error buffering :(");
						return;
					}

					// もし再生が止まっていたら、再生する。
					alGetSourcei(source_id, AL_SOURCE_STATE, out val);
					if (val != AL_PLAYING)
						alSourcePlay(source_id);

					// 掃除
				}
			}

			#region Dispose

			~Source() => Dispose(false);

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion

			void Dispose(bool disposing)
			{
				if (disposed) return;

				if (disposing)
				{
					// マネージリソースの破棄
				}

				// アンマネージリソースの破棄
				alDeleteSources(1, ref source_id);
				alDeleteBuffers(buffer_num, buffers);

				disposed = true;
			}

		}
	}
}

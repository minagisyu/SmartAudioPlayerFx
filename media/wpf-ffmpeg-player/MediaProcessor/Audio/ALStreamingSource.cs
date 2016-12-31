﻿using System;
using System.Threading.Tasks;
using static OpenAL.AL10;

namespace SmartAudioPlayer.MediaProcessor.Audio
{
	public sealed class ALStreamingSource : ALSource
	{
		// Source, Buffer (for Streaming Playing)
		readonly int buffer_num;

		uint source_id;
		uint[] buffers;

		public ALStreamingSource(int buffer_num)
		{
			this.buffer_num = buffer_num;

			buffers = new uint[buffer_num];
			alGenBuffers(buffer_num, buffers);
			alGenSources(1, out source_id);

			alSourcei(source_id, AL_SOURCE_RELATIVE, AL_TRUE);
			alSourcei(source_id, AL_ROLLOFF_FACTOR, 0);

			if (alGetError() != AL_NO_ERROR)
			{
				Console.WriteLine("Error generating :(");
				return;
			}
			for (var i = 0; i < buffer_num; i++)
			{
				alBufferData(buffers[i], SourceFormat.STEREO_16, IntPtr.Zero, 0, 48000);
				if (alGetError() != AL_NO_ERROR)
				{
					// Console.WriteLine("Error Buffer :(");
				}

				alSourceQueueBuffers(source_id, 1, ref buffers[i]);
				if (alGetError() != AL_NO_ERROR)
				{

					// Console.WriteLine("Error buffering :(");
				}
			}

			alSourcePlay(source_id);
			if (alGetError() != AL_NO_ERROR)
			{

				// Console.WriteLine("Error buffering :(");
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (Disposed == false)
			{
				alDeleteSources(1, ref source_id);
				alDeleteBuffers(buffer_num, buffers);
			}
			base.Dispose(disposing);
		}

		public async Task WaitBuffersProcessedAsync()
		{
			// キューがあくのを待機する
			while (Disposed == false)
			{
				alGetSourcei(source_id, AL_BUFFERS_PROCESSED, out var val);
				if (val > 0)
				{
					// 少しスリープさせてバッファが空くのを待つ
					await Task.Delay(1);
					break;
				}

				// もし再生が止まっていたら、再生する。
				alGetSourcei(source_id, AL_SOURCE_STATE, out val);
				if (val != AL_PLAYING)
					alSourcePlay(source_id);
			}
		}

		public async Task<bool> WriteDataAsync(int source_format, IntPtr data, int size, int freq)
		{
			// TODO:
			// キューがない状態で呼び出されたとき、きちんと空きキューが取得できるか？
			//
			await WaitBuffersProcessedAsync();

			// 空いてるキューを取り除き、データ書き込みキューに入れる
			// キューに空きがない場合は失敗する
			var buffer_id = default(uint);
			while (true)
			{
				alSourceUnqueueBuffers(source_id, 1, ref buffer_id);
				if (alGetError() != AL_NO_ERROR) // AL_INVALID_VALUE?
				{
				}
				break;
			}

			alBufferData(buffer_id, source_format, data, size, freq);
			if (alGetError() != AL_NO_ERROR)
			{
				// Console.WriteLine("Error Buffer :(");
				return false;
			}

			alSourceQueueBuffers(source_id, 1, ref buffer_id);
			if (alGetError() != AL_NO_ERROR)
			{

				// Console.WriteLine("Error buffering :(");
				return false;
			}

			return true;
		}

	}
}

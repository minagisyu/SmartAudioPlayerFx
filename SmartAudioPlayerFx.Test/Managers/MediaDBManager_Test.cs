﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartAudioPlayerFx.Data;

namespace SmartAudioPlayerFx.Managers
{
	[TestFixture]
	class MediaDBManager_Test
	{
		const string TESTDB = @"C:\Users\WindowsUser\AppData\Roaming\SmartAudioPlayer Fx\data\media.testdb";
		const string TESTDIR = @"E:\User\Musics\SAP用";

		[Test]
		public void GetFromFilePath_PerformanceTest()
		{
			using(var manager = new MediaDBManager(TESTDB))
			{
				var items = manager.GetFromFilePath(TESTDIR).ToArray();
			}
		}

		[Test]
		public void DoubleTransaction_Test()
		{
			using(var manager = new MediaDBManager(TESTDB))
			{
				var ev0 = new ManualResetEvent(false);
				var action = new Action<ManualResetEvent>((ev) =>
				{
					ev0.WaitOne();
					using (var w = manager.BeginTransaction())
					{
						w.Insert(new[] { new MediaItem() { FilePath = "test.mp3", } });
						w.Commit();
					}
					ev.WaitOne();
				});

				var ev1 = new ManualResetEvent(false);
				var ev2 = new ManualResetEvent(false);
				new Thread(() => action(ev1)).Start();
				new Thread(() => action(ev2)).Start();

				// 同時にトランザクション開始
				ev0.Set();
				Thread.Sleep(200);
				// 個別にコミット開始
				ev1.Set();
				ev2.Set();
				Thread.Sleep(200);
			}
		}

		[Test]
		public void DoubleTransaction_Test2()
		{
			using (var manager = new MediaDBManager(TESTDB))
			{
				using (var w = manager.BeginTransaction())
				{
					w.Insert(new[] { new MediaItem() { FilePath = "test.mp3", } });

					Task.Factory.StartNew(() =>
					{
						using (var w2 = manager.BeginTransaction())
						{
							w2.Insert(new[] { new MediaItem() { FilePath = "test.mp3", } });
							w2.Commit();
						}
					}).Wait();

					w.Commit();
				}
			}
		}

		[Test]
		public void DoubleTransaction_Test3()
		{
			using (var manager = new MediaDBManager(TESTDB))
			{
				using (var w = manager.BeginTransaction())
				{
					w.Insert(new[] { new MediaItem() { FilePath = "test.mp3", } });

					using (var w2 = manager.BeginTransaction())
					{
						w2.Insert(new[] { new MediaItem() { FilePath = "test.mp3", } });
						w2.Commit();
					}

					w.Commit();
				}
			}
		}

	}
}

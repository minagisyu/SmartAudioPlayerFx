using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SmartAudioPlayerFx.Data
{
	[TestFixture]
	class VersionedCollection_Test
	{
		[Test]
		public void Basic_Test()
		{
			var c = new VersionedCollection<int>();

			// deafult
			c.Version.Is(0u);
			c.Count.Is(0);
			c.Contains(100).Is(false);

			// add
			c.AddOrReplace(100).Is(1u);
			c.Version.Is(1u);
			c.Count.Is(1);
			c.Contains(100).Is(true);

			// replace
			c.AddOrReplace(100).Is(2u);
			c.Version.Is(2u);
			c.Count.Is(1);
			c.Contains(100).Is(true);

			// remove
			c.Remove(100).Is(3u);
			c.Version.Is(3u);
			c.Count.Is(0);
			c.Contains(100).Is(false);

			// add
			c.AddOrReplace(100).Is(4u);
			c.Version.Is(4u);
			c.Count.Is(1);
			c.Contains(100).Is(true);

			// clear
			c.Clear().Is(5u);
			c.Version.Is(5u);
			c.Count.Is(0);
			c.Contains(100).Is(false);
		}

		[Test]
		public void EqualityCtor_Test()
		{
			var c = new VersionedCollection<string>(StringComparer.CurrentCulture);
			c.AddOrReplace("ABC");
			c.Contains("ABC").Is(true);
			c.Contains("abc").Is(false);

			c = new VersionedCollection<string>(StringComparer.CurrentCultureIgnoreCase);
			c.AddOrReplace("ABC");
			c.Contains("ABC").Is(true);
			c.Contains("abc").Is(true);
			c.Contains("aBc").Is(true);
			c.Contains("Abc").Is(true);
			c.Contains("123").Is(false);
		}

		[Test]
		public void GetTest()
		{
			var c = new VersionedCollection<int>();
			
			// default
			var ret = c.Get();
			ret.IsNotNull();
			ret.Item1.Length.Is(0);
			ret.Item2.Is(0u);

			// v1
			c.AddOrReplace(1);
			ret = c.Get();
			ret.Item1.Length.Is(1);
			ret.Item1.Is(1);
			ret.Item2.Is(1u);

			// v2
			c.AddOrReplace(3);
			ret = c.Get();
			ret.Item1.Length.Is(2);
			ret.Item1.Is(1, 3);
			ret.Item2.Is(2u);

			// Get(minlimit:0)
			ret = c.Get(0);
			ret.Item1.Length.Is(2);
			ret.Item1.Is(1, 3);
			ret.Item2.Is(2u);

			// Get(minlimit:1)
			ret = c.Get(1);
			ret.Item1.Length.Is(2);
			ret.Item1.Is(1, 3);
			ret.Item2.Is(2u);

			// Get(minlimit:2)
			ret = c.Get(2);
			ret.Item1.Length.Is(1);
			ret.Item1.Is(3);
			ret.Item2.Is(2u);

			// Get(minlimit:3)
			ret = c.Get(3);
			ret.Item1.Length.Is(0);
			ret.Item2.Is(2u);
		}

		[Test]
		public void MultiAccessTest()
		{
			var tests = 1000;
			var c = new VersionedCollection<int>();

			Enumerable.Range(0, tests)
				.AsParallel()
				.ForAll(x => c.AddOrReplace(x));

			var ret = c.Get();
			ret.Item1.Length.Is(tests);
			ret.Item2.Is((ulong)tests);

			var list = ret.Item1.ToList();
			for (var i = 0; i < tests; i++)
			{
				list.Contains(i).Is(true);
			}
			list.Contains(tests + 1).Is(false);


		}

		[Test]
		public void Notify_Test()
		{
			var c = new VersionedCollection<int>();
			var notify = new Queue<VersionedCollection<int>.NotifyInfo>();
			c.GetNotifyObservable().Subscribe(x => notify.Enqueue(x));

			// add
			c.AddOrReplace(100);
			var n = notify.Dequeue();
			n.Type.Is(VersionedCollection<int>.NotifyType.Add);
			n.Item.Is(100);

			// replace
			c.AddOrReplace(100);
			n = notify.Dequeue();
			n.Type.Is(VersionedCollection<int>.NotifyType.Update);
			n.Item.Is(100);

			// remove (アイテムが削除できた)
			c.Remove(100);
			n = notify.Dequeue();
			n.Type.Is(VersionedCollection<int>.NotifyType.Remove);
			n.Item.Is(100);

			// remove (アイテムが削除できなかった)
			c.Remove(100);
			notify.Count.Is(0);

			// clear (削除できた)
			c.AddOrReplace(100);
			n = notify.Dequeue();
			c.Clear();
			n = notify.Dequeue();
			n.Type.Is(VersionedCollection<int>.NotifyType.Clear);
			n.Item.Is(default(int));

			// clear (削除できなかった)
			c.Clear();
			notify.Count.Is(0);

		}

	}
}

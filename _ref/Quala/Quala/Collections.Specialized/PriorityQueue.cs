using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Quala.Collections.Specialized
{
	public class PriorityQueue<T>
	{
		List<KeyValuePair<int, T>> buffer = new List<KeyValuePair<int, T>>();

		public void Enqueue(int priority, T value)
		{
			buffer.Add(new KeyValuePair<int, T>(priority, value));
			buffer.Sort((l, r) => l.Key - r.Key);
		}
		public T Peek()
		{
			if (buffer.Count == 0)
				return default(T);
			return buffer[0].Value;
		}
		public T Dequeue()
		{
			if (buffer.Count == 0)
				return default(T);
			var value = buffer[0].Value;
			buffer.RemoveAt(0);
			buffer.Sort((l, r) => l.Key - r.Key);
			return value;
		}

		public int Count
		{
			get { return buffer.Count; }
		}

		[System.Diagnostics.Conditional("DEBUG")]
		internal static void TEST()
		{
			var pq = new PriorityQueue<string>();

			if (pq.Peek() != null)
				Console.WriteLine("error: 1");
			if (pq.Dequeue() != null)
				Console.WriteLine("error: 2");

			pq.Enqueue(1, "1");
			pq.Enqueue(1, "2");
			pq.Enqueue(5, "3");
			pq.Enqueue(3, "4");
			pq.Enqueue(-2, "5");
			Console.WriteLine(pq.Peek());		// 5
			Console.WriteLine(pq.Dequeue());	// 5
			Console.WriteLine(pq.Dequeue());	// 1
			Console.WriteLine(pq.Dequeue());	// 2
			Console.WriteLine(pq.Dequeue());	// 4
			Console.WriteLine(pq.Dequeue());	// 3

			pq.Enqueue(-7, "0");
			pq.Enqueue(5, "1");
			pq.Enqueue(1, "2");
			pq.Enqueue(3, "3");
			pq.Enqueue(1, "4");
			Console.WriteLine(pq.Dequeue());	// 0
			Console.WriteLine(pq.Dequeue());	// 2
			Console.WriteLine(pq.Dequeue());	// 4
			Console.WriteLine(pq.Dequeue());	// 3
			Console.WriteLine(pq.Dequeue());	// 1
		}

	}
}

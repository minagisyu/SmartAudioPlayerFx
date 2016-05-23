using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace SmartAudioPlayerFx
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
	sealed class FocusTestFixtureAttribute : Attribute { }

	class Program
	{
		[STAThread]
		static void Main()
		{
			var sw = Stopwatch.StartNew();
			Assembly.GetExecutingAssembly()
				.GetModules()
				.First()
				.FindTypes(
					(t, _) => t.GetCustomAttributes(typeof(TestFixtureAttribute), false).Length > 0,
				//	(t, _) => t.GetCustomAttributes(typeof(FocusTestFixtureAttribute), false).Length > 0,
					null)
				.ForEach(t =>
				{
					Console.WriteLine("{0}", t.Name);
					t.GetMethods()
						.Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
						.ForEach(m =>
						{
							sw.Restart();
							Console.Write("...{0} ", m.Name);
							var obj = Activator.CreateInstance(t);
							try
							{
								m.Invoke(obj, null);
							//	m.Invoke(obj, null);
							}
							catch { }
							sw.Stop();
							Console.WriteLine("[{0}ms]", sw.ElapsedMilliseconds);
						});
				});

			Console.WriteLine(">> END <<");
			Console.ReadKey();
		}

		public static void SafeDelete(string path)
		{
			while (true)
			{
				try { File.Delete(path); }
				catch
				{
					Thread.Sleep(1);
					continue;
				}
				break;
			}
		}

	}
}

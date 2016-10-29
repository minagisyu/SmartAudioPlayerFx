using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test
{
	class Program
	{
		static void Main(string[] args)
		{
			var container = new Container();
			container.Register<InjectClass>();
			container.RegisterInitializer<InjectClass>(x => x.Prop = "2");
			Console.WriteLine("registered.");

			Console.WriteLine(container.GetInstance<InjectClass>().Prop);
			Console.WriteLine(container.GetInstance<InjectClass>().Prop);
			Console.WriteLine(container.GetInstance<InjectClass>().Prop);

		}
	}

	class InjectClass
	{
		public InjectClass()
		{
			Console.WriteLine("Inject.ctor");
		}

		string pr = "pr";
		public string Prop
		{
			get { return pr; }
			set
			{
				pr = value;
				Console.WriteLine("Inject.Prop.set");
			}
		} 


	}
}

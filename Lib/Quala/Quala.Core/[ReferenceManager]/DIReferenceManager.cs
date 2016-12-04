using SimpleInjector;

namespace Quala
{
	sealed class DIReferenceManager
	{
		Container container;

		public DIReferenceManager()
		{
			container = new Container();
		}

		public DIReferenceManager AddTransient<TConcreate>()
			where TConcreate : class
		{
			container.Register<TConcreate>();
			return this;
		}

		public DIReferenceManager AddTransient<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			container.Register<TService, TImplementation>();
			return this;
		}

		public DIReferenceManager AddSingleton<TConcrete>()
			where TConcrete : class
		{
			container.RegisterSingleton<TConcrete>();
			return this;
		}

		public DIReferenceManager AddSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			container.RegisterSingleton<TService, TImplementation>();
			return this;
		}


	}
}

using System;

namespace Quala
{
	// シングルトンでホストされることを前提としたクラスにつけられる属性
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
	public sealed class SingletonServiceAttribute : Attribute
	{
	}

}

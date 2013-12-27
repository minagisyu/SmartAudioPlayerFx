using System;

namespace SmartAudioPlayer
{
	// Managerは何も依存しない
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class StandaloneAttribute : Attribute
	{
		public StandaloneAttribute()
		{
		}
	}

	// ManagerはTypeに依存する
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public sealed class RequireAttribute : Attribute
	{
		public RequireAttribute(Type requireType)
		{
		}
	}
}

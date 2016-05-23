using System;
using System.IO;
using System.Reflection;

namespace Quala
{
	public sealed partial class Storage : IDisposable
	{
		public AppDataPath AppDataRoaming { get; private set; }
		public AppDataPath AppDataLocal { get; private set; }
		public AppDataPath AppCurrent { get; private set; }

		public Storage()
		{
			// BaseDirectoryの設定
			var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var asmName = asm.GetName().Name;
			AppDataRoaming = new AppDataPath(new DirectoryInfo(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), asmName)));
			AppDataLocal = new AppDataPath(new DirectoryInfo(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), asmName)));
			AppCurrent = new AppDataPath(new DirectoryInfo(
				Path.Combine(Path.GetDirectoryName(asm.Location), asmName)));
		}

		void IDisposable.Dispose() { }
	}
}

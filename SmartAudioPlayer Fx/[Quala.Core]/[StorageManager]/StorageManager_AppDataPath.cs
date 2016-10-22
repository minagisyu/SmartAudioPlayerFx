using System.IO;

namespace Quala
{
	partial class StorageManager
	{
		public sealed class DataPath
		{
			public DirectoryInfo PathInfo { get; }
			public string PathName { get { return PathInfo.FullName; } }

			public DataPath(DirectoryInfo directoryPath)
			{
				PathInfo = directoryPath;
			}

			public string CreateFilePath(params string[] name)
				=> Path.Combine(PathName, Path.Combine(name));

			public string CreateDirectoryPath(params string[] name)
				=> Path.Combine(PathName, Path.Combine(name));

			public FileInfo CreateFilePathInfo(params string[] name)
				=> new FileInfo(CreateFilePath(name));

			public DirectoryInfo CreateDirectoryPathInfo(params string[] name)
				=> new DirectoryInfo(CreateDirectoryPath(name));

		}
	}
}

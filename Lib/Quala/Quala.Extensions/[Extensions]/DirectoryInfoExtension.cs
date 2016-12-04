using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Quala.Extensions
{
	public static class DirectoryInfoExtension
	{
		public static IEnumerable<FileInfo> EnumerateFilesSafe(this DirectoryInfo path)
		{
			var files = default(IEnumerable<FileInfo>);
			try { files = path.EnumerateFiles(); }
			catch (UnauthorizedAccessException) { }
			catch (DirectoryNotFoundException) { }
			catch (IOException) { }

			return (files ?? Enumerable.Empty<FileInfo>());
		}
		public static IEnumerable<DirectoryInfo> EnumerateDirectoriesSafe(this DirectoryInfo path)
		{
			var dirs = default(IEnumerable<DirectoryInfo>);
			try { dirs = path.EnumerateDirectories(); }
			catch (UnauthorizedAccessException) { }
			catch (DirectoryNotFoundException) { }
			catch (IOException) { }

			return (dirs ?? Enumerable.Empty<DirectoryInfo>());
		}

		public static IEnumerable<EnumerateFilesEnumerateAllFilesNotify> EnumerateAllFiles(this DirectoryInfo path)
		{
			if (path.Exists == false) yield break;
			yield return new EnumerateFilesEnumerateAllFilesNotify(path.FullName, EnumerateFilesSafe(path).Select(x => x.FullName).ToList().AsReadOnly());

			foreach (var dir in EnumerateDirectoriesSafe(path))
			{
				foreach (var x in EnumerateAllFiles(dir))
				{
					yield return x;
				}
			}
		}

		public struct EnumerateFilesEnumerateAllFilesNotify
		{
			public string DirectoryName { get; private set; }
			public IReadOnlyCollection<string> Files { get; private set; }
			public EnumerateFilesEnumerateAllFilesNotify(string dirName, IReadOnlyCollection<string> files)
			{
				DirectoryName = dirName;
				Files = files;
			}
		}
	}
}

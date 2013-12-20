using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SmartAudioPlayerFx.Data
{
	static class FileSystemUtil
	{
		static IEnumerable<string> EnumerateFiles(string path, CancellationToken token)
		{
			var files = default(IEnumerable<string>);
			try { files = Directory.EnumerateFiles(path); }
			catch (UnauthorizedAccessException) { }
			catch (DirectoryNotFoundException) { }
			catch (IOException) { }

			return
				(files ?? Enumerable.Empty<string>())
				.TakeWhile(_ => token.IsCancellationRequested == false);
		}
		static IEnumerable<string> EnumerateDirectories(string path, CancellationToken token)
		{
			var dirs = default(IEnumerable<string>);
			try { dirs = Directory.EnumerateDirectories(path); }
			catch (UnauthorizedAccessException) { }
			catch (DirectoryNotFoundException) { }
			catch (IOException) { }

			return
				(dirs ?? Enumerable.Empty<string>())
				.TakeWhile(_ => token.IsCancellationRequested == false);
		}

		public static IEnumerable<EnumerateFilesNotify> EnumerateAllFiles(string path, CancellationToken token)
		{
			if (Directory.Exists(path) == false) yield break;
			yield return new EnumerateFilesNotify(path, EnumerateFiles(path, token));

			foreach (var dir in EnumerateDirectories(path, token))
			{
				foreach (var x in EnumerateAllFiles(dir, token))
				{
					yield return x;
				}
			}
		}

		public struct EnumerateFilesNotify
		{
			public string DirectoryName { get; private set; }
			public IEnumerable<string> Files { get; private set; }
			public EnumerateFilesNotify(string dirName, IEnumerable<string> files)
			{
				this = new FileSystemUtil.EnumerateFilesNotify();
				this.DirectoryName = dirName;
				this.Files = files;
			}
		}
	}
}

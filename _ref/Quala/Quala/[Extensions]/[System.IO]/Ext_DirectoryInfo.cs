using System;
using System.Collections.Generic;
using System.IO;

namespace Quala
{
	partial class Extension
	{
		/// <summary>
		/// 指定されたディレクトリと全サブディレクリのファイルパスを取得する。
		/// 機能的にはDirectoryInfo.GetFiles()と同じだが、例外を処理する点が異なる。
		/// </summary>
		/// <param name="di"></param>
		/// <returns></returns>
		public static IEnumerable<FileInfo> GetAllFiles(this DirectoryInfo di)
		{
			FileInfo[] files;
			DirectoryInfo[] dirs;
			try
			{
				files = di.GetFiles();
				dirs = di.GetDirectories();
			}
			catch(UnauthorizedAccessException) { yield break; }
			catch(DirectoryNotFoundException) { yield break; }

			foreach(var file in files)
				yield return file;
			foreach(var dir in dirs)
				foreach(var d in dir.GetAllFiles())
					yield return d;
		}

		/// <summary>
		/// 指定されたディレクトリと全サブディレクリのファイルパスを取得する。
		/// 機能的にはDirectoryInfo.GetDirectories()と同じだが、例外を処理する点が異なる。
		/// </summary>
		/// <param name="di"></param>
		/// <returns></returns>
		public static IEnumerable<DirectoryInfo> GetDirectories_Catched(this DirectoryInfo di)
		{
			DirectoryInfo[] dirs;
			try { dirs = di.GetDirectories(); }
			catch(UnauthorizedAccessException) { yield break; }
			catch(DirectoryNotFoundException) { yield break; }

			foreach(var dir in dirs)
				yield return dir;
		}

		/// <summary>
		/// 指定されたディレクトリのファイルパスを取得する。
		/// 機能的にはDirectoryInfo.GetFiles()と同じだが、例外を処理する点が異なる。
		/// </summary>
		/// <param name="di"></param>
		/// <returns></returns>
		public static IEnumerable<FileInfo> GetFiles_Catched(this DirectoryInfo di)
		{
			FileInfo[] files;
			try { files = di.GetFiles(); }
			catch(UnauthorizedAccessException) { yield break; }
			catch(DirectoryNotFoundException) { yield break; }

			foreach(var file in files)
				yield return file;
		}

		/// <summary>
		/// 指定されたディレクトリと全サブディレクリのファイルパスを取得する。
		/// 列挙時の進捗具合を0～100(%)の間で報告する。
		/// </summary>
		/// <remarks>
		/// ただし、完全に100%にならない可能性があることに注意。
		/// ディレクトリ単位報告なので階層が深くなるほど進捗が遅くなりますが、
		/// 無駄な動きはそんなにないかと。
		/// </remarks>
		/// <param name="di"></param>
		/// <returns></returns>
		public static IEnumerable<EnumerationProgress<FileInfo>> GetAllFilesWithProgress(this DirectoryInfo di)
		{
			// phase.1
			// ファイルとディレクトリの取得。
			// ディレクトリ数+1(現在のディレクトリ)が全体の工数。
			// 100.0 / nで一回の進捗数を計算。
			FileInfo[] files;
			DirectoryInfo[] dirs;
			try
			{
				files = di.GetFiles();
				dirs = di.GetDirectories();
			}
			catch(UnauthorizedAccessException) { yield break; }
			catch(DirectoryNotFoundException) { yield break; }
			var phase_incl = 100.0 / (double)(dirs.Length + (files.Length == 0 ? 0 : 1));
			double progress = 0;

			// phase.2
			// 現在のディレクトリのファイルを取得。
			// phase_incl / nでファイル一つの進捗数を計算。
			var file_incl = phase_incl / (double)files.Length;
			foreach(var file in files)
			{
				progress += file_incl;
				yield return new EnumerationProgress<FileInfo>(file, progress);
			}

			// phase.3
			// サブディレクトリのファイルを取得。
			// ((phase_incl / 100.0) * d.Progress)で、1つのディレクトリにおける
			// d.Progressのインクリメント単位をphase_incl内になるように補正。
			foreach(var dir in dirs)
			{
				foreach(var d in dir.GetAllFilesWithProgress())
					yield return new EnumerationProgress<FileInfo>(d.Value,
						progress + ((phase_incl / 100.0) * d.Progress));

				progress += phase_incl;
			}

		}

		/// <summary>
		/// 指定されたディレクトリと全サブディレクリのファイルパスを取得する。
		/// 複数パス対応版。
		/// </summary>
		/// <param name="di"></param>
		/// <returns></returns>
		public static IEnumerable<EnumerationProgress<FileInfo>> GetAllFilesWithProgress(this DirectoryInfo[] di)
		{
			// phase.1
			// ディレクトリ数が全体の工数。
			// 100.0 / nで一回の進捗数を計算。
			var phase_incl = 100.0 / (double)di.Length;

			// phase.2
			double progress = 0;
			foreach(var d_ in di)
			{
				foreach(var d in d_.GetAllFilesWithProgress())
					yield return new EnumerationProgress<FileInfo>(d.Value,
						progress + ((phase_incl / 100.0) * d.Progress));

				progress += phase_incl;
			}
		}


		/// <summary>
		/// 列挙の進捗具合を報告するクラス
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public sealed class EnumerationProgress<T>
		{
			/// <summary>
			/// 列挙された値
			/// </summary>
			public T Value { get; private set; }

			/// <summary>
			/// 現在の進捗具合
			/// </summary>
			public double Progress { get; private set; }

			public EnumerationProgress(T value, double progress)
			{
				this.Value = value;
				this.Progress = progress;
			}
		}
	}
}

using Reactive.Bindings;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Quala
{
	public sealed partial class LogManager
	{
		// Outputに出力されるログの最低レベル
		public Level MinLevel { get; set; } = Level.INFO;

		// 出力用IF、各プラットフォームで都合のいい実装で対応する
		ReactiveProperty<string> outputInternal;
		public ReadOnlyReactiveProperty<string> Output { get; }

		public LogManager()
		{
			// ヘッダを用意
			var headerText = new StringBuilder();
			var asm = Assembly.GetEntryAssembly();
			var asmName = new AssemblyName(asm.FullName);
			var version = asmName.Version;
			headerText.AppendLine();
			headerText.AppendLine("--------------------------------------------------------------------------------");
			headerText.AppendLine($"{asmName.Name} ver.{version}");
			//sb.AppendLine($"{version.FileDescription} ver.{version.ProductVersion}");
			headerText.AppendLine();

			// 出力用IF
			outputInternal = new ReactiveProperty<string>(headerText.ToString(), ReactivePropertyMode.RaiseLatestValueOnSubscribe);
			Output = outputInternal.ToReadOnlyReactiveProperty();
		}

		// カスタムログ
		public void AddLog(Level level, string message,
			[CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
		{
			if (level < MinLevel) return;

			var log = new Item()
			{
				Time = DateTime.Now,
				Level = level,
			//	Source = new StackTrace().GetFrame(2).GetMethod().DeclaringType.Name,
				Source = $"{Path.GetFileName(file)}:{line} - {member}",
				Message = message,
			};

		//	Debug.WriteLine($"[{log.Source}] {log}{Environment.NewLine}");
			outputInternal.Value = log.ToString();
		}
	}
}

using Quala;
using System;
using System.Collections.Generic;
using static System.Console;

namespace SmartAudioPlayerFx
{
	class Program
	{
		static AppServiceManager appServices = new AppServiceManager();

		static void Main(string[] args)
		{
			new List<string>()
			{
				$"========================================",
				$" sfxcore console.",
				$"========================================",
			}.ForEach(WriteLine);
			PrintCommand();

			bool enterExit = false;
			while (enterExit == false)
			{
				Write("> ");
				switch (ReadKey().KeyChar)
				{
					case '?':
						WriteLine();
						PrintCommand();
						break;

					case 'E':
					case 'e':
						enterExit = true;
						break;

					case 'I':
					case 'i':
						WriteLine();
						ManagerInitialize();
						break;
				}
				WriteLine();
			}

			WriteLine("shutdown...");
			appServices.Dispose();


			WriteLine("press enter to exit");
			ReadLine();
		}

		static void PrintCommand()
		{
			new List<string>()
			{
				$"i:initialize app services",
				$"e:exit",
				$"?:command help",
			}.ForEach(WriteLine);
		}

		static void ManagerInitialize()
		{
			var xmlPreferences = appServices.Get<XmlPreferencesService>();
		}

	}
}

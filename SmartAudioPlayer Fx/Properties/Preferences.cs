using System;
using System.ComponentModel;
using System.Xml.Linq;
using Quala;

namespace SmartAudioPlayerFx.Properties
{
	partial class Preferences
	{
		const string DATADIRNAME = "data";
		const string PLAYER_ELEMENTNAME = "Player";
		const string PLAYER_FILENAME = "player.xml";
		const string WINDOW_ELEMENTNAME = "Window";
		const string WINDOW_FILENAME = "window.xml";
		const string UPDATE_ELEMENTNAME = "UpdateSettings";
		const string UPDATE_FILENAME = "update_settings.xml";

		public static event EventHandler Loaded;
		public static event CancelEventHandler Saving;

		public static XElement PlayerSettings { get; private set; }
		public static XElement WindowSettings { get; private set; }
		public static XElement UpdateSettings { get; private set; }

		static Preferences()
		{
			Loaded += delegate { };
			Saving += delegate { };
			PlayerSettings = new XElement(PLAYER_ELEMENTNAME);
			WindowSettings = new XElement(WINDOW_ELEMENTNAME);
			UpdateSettings = new XElement(UPDATE_ELEMENTNAME);
		}

		public static void Load()
		{
			PlayerSettings = PreferenceService.Load(PLAYER_ELEMENTNAME, DATADIRNAME, PLAYER_FILENAME);
			WindowSettings = PreferenceService.Load(WINDOW_ELEMENTNAME, DATADIRNAME, WINDOW_FILENAME);
			UpdateSettings = PreferenceService.Load(UPDATE_ELEMENTNAME, DATADIRNAME, UPDATE_FILENAME);
			Loaded(null, EventArgs.Empty);
		}

		public static void Save()
		{
			var e = new CancelEventArgs();
			Saving(null, e);
			if (e.Cancel == false)
			{
				PreferenceService.Save(PlayerSettings, DATADIRNAME, PLAYER_FILENAME);
				PreferenceService.Save(WindowSettings, DATADIRNAME, WINDOW_FILENAME);
				PreferenceService.Save(UpdateSettings, DATADIRNAME, UPDATE_FILENAME);
			}
		}

	}
}

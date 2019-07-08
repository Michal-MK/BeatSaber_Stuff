using System;
using System.Diagnostics;
using System.IO;

namespace MTDBCore {
	public class SettingsManager {
		#region Singleton Instance

		public static SettingsManager Instance { get; private set; }

		public int SettingsInitFailure { get; private set; } = 0;

		public static void Initialize() {
			Instance = new SettingsManager();
		}

		private SettingsManager() {
			if (!Directory.Exists(settingsPath)) {
				Directory.CreateDirectory(settingsPath);
			}

			try {
				CurrentSettings = ParseSettings();
			}
			catch {
				SettingsInitFailure = 1;
				GenerateSettings();
				CurrentSettings = ParseSettings();
			}
		}

		#endregion

		private string settingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings") + Path.DirectorySeparatorChar;
		private string configFile => settingsPath + ".config.cfg";

		public Settings CurrentSettings { get; private set; }

		private void GenerateSettings() {
			try {
				if (File.Exists(configFile)) {
					File.Delete(configFile);
				}

				File.WriteAllText(configFile, new Settings().ToFileString());
			}
			catch {
				SettingsInitFailure = 2;
			}
		}

		private Settings ParseSettings() {
			if (!File.Exists(configFile)) {
				throw new IOException("No settings exist!");
			}

			Settings ret = new Settings();

			try {
				using (StreamReader reader = new StreamReader(configFile)) {
					while (!reader.EndOfStream) {
						SettingsParser.ParseLine(ret, reader.ReadLine());
					}
				}
			}
			catch (Exception e) {
				Debugger.Break();
				throw e;
				/*Handled in call above*/
			}
			return ret;
		}

		public int Save() {
			try {
				if (File.Exists(configFile)) {
					File.Delete(configFile);
				}
				File.WriteAllText(configFile, CurrentSettings.ToFileString());
				return 0;
			}
			catch {
				return 3;
			}
		}
	}
}

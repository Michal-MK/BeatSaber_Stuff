using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO.Compression;
using Newtonsoft.Json;

namespace BeatSaberRenaming {
	class Program {

		private static FileInfo[] zips;

		private static Settings Current;

		static void Main() {
			bool newGeneration = !SettingsManager.Initialize();
			if (newGeneration) {
				Console.WriteLine("Settings were generated, edit them and launch again");
				Console.ReadLine();
				return;
			}
			Current = SettingsManager.Instance.CurrentSettings;


			Console.WriteLine("This is a simple program to ease song/folder renaming");
			Console.WriteLine("There are 3 possible choices: 'y' - accept, 'n' - refuse/swap, 'e' - open default JSON (the .dat is just json lol) editor");
			Console.WriteLine();

			Console.WriteLine("Select folder with downloaded songs:");
			Console.WriteLine(string.Join("\r\n", new DirectoryInfo(Directory.GetCurrentDirectory()).EnumerateDirectories().Select(s => s.Name)));

			string directory = Console.ReadLine();
			string fullDirPath = Path.Combine(Directory.GetCurrentDirectory(), directory);
			zips = new DirectoryInfo(fullDirPath).GetFiles("*.zip", SearchOption.AllDirectories);

			List<string> directories = new List<string>();

			foreach (FileInfo zip in zips) {
				string dirPath = fullDirPath + Path.DirectorySeparatorChar + zip.Name.Replace(".zip", "");
				directories.Add(dirPath);
				try {
					ZipFile.ExtractToDirectory(zip.FullName, dirPath);
				}
				catch (Exception e) {
					if(e.Message.Contains("already exists")) {
						Console.WriteLine(e.Message);
					}
					throw e;
				}
			}

			Console.WriteLine("Extracted");

			foreach (string dir in directories) {
				DirectoryInfo info = new DirectoryInfo(dir);
				if (info.GetFiles().Length == 0) {
					FileInfo[] filesToMove = info.GetDirectories()[0].GetFiles();
					foreach (FileInfo toMove in filesToMove) {
						File.Move(toMove.FullName, Path.Combine(info.FullName, toMove.Name));
					}
					info.GetDirectories()[0].Delete();
				}
			}

			const string SONG_NAME = "_songName";
			const string COMPOSER_NAME = "_songAuthorName";
			const string LEVEL_AUTHOR = "_levelAuthorName";

			foreach (string dir in directories) {
				DirectoryInfo info = new DirectoryInfo(dir);
				FileInfo[] nf = info.GetFiles("info.dat");
				if (nf.Length == 0) {
					return;
				}

				JObject jsonObject;

				using (StreamReader reader = nf[0].OpenText()) {
					using (JsonTextReader json = new JsonTextReader(reader)) {
						jsonObject = JObject.Load(json);
					}
				}

				string name = jsonObject[SONG_NAME].Value<string>();
				string composer = jsonObject[COMPOSER_NAME].Value<string>();
				string levelMaker = jsonObject[LEVEL_AUTHOR].Value<string>();

				if (Validate(name) && Validate(composer) && Validate(levelMaker)) {
					if (Current.CopyAfterConversion && Current.DeleteExtractedAndRenamed) {
						try {
							info.MoveTo(Path.Combine(Current.CustomSongsFolder, $"{composer} - {name} (by {levelMaker})"));
						}
						catch (Exception e) {
							Console.WriteLine(e.Message);
							Directory.Delete(dir, true);
						}
					}
					else if (Current.CopyAfterConversion) {
						try {
							info.MoveTo(Path.Combine(info.Parent.FullName, $"{composer} - {name} (by {levelMaker})"));
							CopyFolder(info, Current.CustomSongsFolder);
						}
						catch (Exception e) {
							Console.WriteLine(e.Message);
						}
					}
					if (Current.DeleteZips) {
						foreach (FileInfo infos in info.GetFiles("*.zip")) {
							infos.Delete();
						}
					}
				}
				else {
					Console.BackgroundColor = ConsoleColor.Red;
					Console.WriteLine($"Invalid! -> {name} - {composer} ({levelMaker})");
				}

				Console.WriteLine($"{name} - {composer} Processed!");
			}

			Console.WriteLine("Done");
			Console.ReadLine();
		}

		private static void CopyFolder(DirectoryInfo from, string to) {
			ZipFile.CreateFromDirectory(from.FullName, from + ".zip");
			string toComplete = Path.Combine(to, from.Name + ".zip");
			File.Move(from + ".zip", toComplete);
			ZipFile.ExtractToDirectory(toComplete, toComplete.Replace(".zip", ""));
			File.Delete(toComplete);
		}

		private static bool Validate(string text) {
			char[] invalidName = Path.GetInvalidFileNameChars();
			foreach (char c in invalidName) {
				if (text.Contains(c.ToString())) {
					Console.BackgroundColor = ConsoleColor.Red;
					Console.WriteLine("!!!!!!!!!!!");
					Console.WriteLine("While processing '" + text + "' I found '" + c + "' which is not a valid FileName character");
					Console.WriteLine("!!!!!!!!!!!");
					Console.BackgroundColor = ConsoleColor.Black;
					return false;
				}
			}
			return true;
		}
	}
}


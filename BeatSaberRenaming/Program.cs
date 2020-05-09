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
using System.Text;

namespace BeatSaberRenaming {
	class Program {

		private static FileInfo[] zips;

		private static Settings Current;

		static void Main(string[] args) {
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
					if (e.Message.Contains("already exists")) {
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
					Process(name, composer, levelMaker, info);
				}
				else {
					string replaced = ReplaceInvalid($"{composer} - {name} (by {levelMaker})");
					Process(replaced, info);
				}
				Console.WriteLine($"{name} - {composer} Processed!");
			}

			Console.WriteLine("Done");
			Console.ReadLine();
		}

		private static string ReplaceInvalid(string curr) {
			StringBuilder sb = new StringBuilder();
			List<char> invalidName = new List<char>(Path.GetInvalidFileNameChars());
			foreach (char c in curr) {
				if (invalidName.Contains(c)) {
					Console.BackgroundColor = ConsoleColor.Red;
					Console.WriteLine($"Replace '{c}':");
					Console.BackgroundColor = ConsoleColor.Black;
					bool suitableReplacement = false;
					while (!suitableReplacement) {
						Console.Write("$ ");

						string s = Console.ReadLine();
						if (s.Length > 1) { Console.WriteLine("One character only!"); }
						if (s.Length == 0) { Console.WriteLine($"Removed '{c}'"); break; }
						char replacement = s[0];
						if (invalidName.Contains(replacement)) { Console.WriteLine($"{replacement} is also invalid!"); }
						sb.Append(replacement);
						suitableReplacement = true;
					}
				}
				else {
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		public static void Process(string name, string composer, string levelMaker, DirectoryInfo info) {
			Process($"{composer} - {name} (by {levelMaker})", info);
		}

		private static void Process(string fullName, DirectoryInfo info) {
			if (Current.CopyAfterConversion && Current.DeleteExtractedAndRenamed) {
				try {
					info.MoveTo(Path.Combine(Current.CustomSongsFolder, fullName));
				}
				catch (Exception e) {
					Console.WriteLine(e.Message);
					info.Delete(true);
				}
			}
			else if (Current.CopyAfterConversion) {
				try {
					info.MoveTo(Path.Combine(info.Parent.FullName, fullName));
					CopyFolder(info, Current.CustomSongsFolder);
				}
				catch (Exception e) {
					Console.WriteLine(e.Message);
				}
			}
			else if (Current.DeleteZips) {
				foreach (FileInfo infos in info.GetFiles("*.zip")) {
					infos.Delete();
				}
			}
			else {
				info.MoveTo(Path.Combine(info.Parent.FullName, fullName));
			}
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
					Console.WriteLine("While processing '" + text + "' I found '" + c + "' which is not a valid file name character");
					Console.WriteLine("!!!!!!!!!!!");
					Console.BackgroundColor = ConsoleColor.Black;
					return false;
				}
			}
			return true;
		}
	}
}


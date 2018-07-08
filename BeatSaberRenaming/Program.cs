using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace BeatSaberRenaming {
	class Program {
		private static FileInfo[] jsonFiles;

		private static ManualResetEventSlim main = new ManualResetEventSlim();
		private static ManualResetEventSlim thread = new ManualResetEventSlim();

		private static bool swapAuthorMeaning = false;

		private static Regex authorNameRegEx = new Regex("\"authorName\": ?\"(.+)\"");
		private static Regex songNameRegEx = new Regex("\"songName\": ?\"(.+)\"");
		private static Regex songSubNameRegEx = new Regex("\"songSubName\": ?\"(.+)\"");

		private static JObject current;

		private static Dictionary<string, string> dict = new Dictionary<string, string>();

		private static int index = 0;

		static void Main(string[] args) {

			Console.WriteLine("This is a simple program to ease song/folder renaming");
			Console.WriteLine("Place this into ../CustomSongs/");
			Console.WriteLine();
			Console.WriteLine("There are up to 3 choices: 'y' - accept, 'n' - refuse, 'e' - open default json editor");
			Console.WriteLine();

			Console.WriteLine("This program assumes that \"authorName\" is the name of the composer, and \"songSubName\" is the map creator");
			Accept.Answer a = Accept.AcceptQ("Use this behaviour?", false);

			if (a == Accept.Answer.REFUSE) {
				swapAuthorMeaning = true;
				Console.WriteLine("Swapped!");
				dict = new Dictionary<string, string>() {
					{"authorName", "Mapper"},
					{"songSubName", "Song Composer"}
				};
			}
			else {
				Console.WriteLine("Using default");
				dict = new Dictionary<string, string>() {
					{"songSubName", "Mapper"},
					{"authorName", "Song Composer"}
				};
			}

			jsonFiles = new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("info.json", SearchOption.AllDirectories);

			if (Accept.AcceptQ("Edit 'info.json' files?",false) == Accept.Answer.ACCEPT) {
				for (int i = 0; i < jsonFiles.Length; i++) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine(jsonFiles[i].FullName);
					Console.ForegroundColor = ConsoleColor.Gray;
					Thread t = new Thread(new ThreadStart(delegate { Open(i); }));
					t.Start();
					main.Wait();
					main.Reset();
					index++;
				}
			}

			if (Accept.AcceptQ("Rename folders?", false) == Accept.Answer.ACCEPT) {
				for (int i = 0; i < jsonFiles.Length; i++) {
					string text = File.ReadAllText(jsonFiles[i].FullName);

					Match authM = authorNameRegEx.Match(text);
					Match songM = songNameRegEx.Match(text);
					Match songSubM = songSubNameRegEx.Match(text);

					bool passOne = Validate(authM.Groups[1].Value);
					bool passTwo = Validate(songM.Groups[1].Value);
					bool passThree = Validate(songSubM.Groups[1].Value);

					if (!(passOne && passTwo & passThree)) {
						Console.WriteLine("\nExecution stopped, edit the invalid file and restart.");
						Console.WriteLine("Press Enter to exit and open the corrupt file...");
						Console.ReadLine();
						ProcessStartInfo info = new ProcessStartInfo(jsonFiles[i].FullName);
						Process.Start(info);
						Environment.Exit(0);
					}
					string combined;

					if (!swapAuthorMeaning) {
						combined = authM.Groups[1].Value + " - " + songM.Groups[1].Value + " (by " + songSubM.Groups[1].Value + ")";
					}
					else {
						combined = songSubM.Groups[1].Value + " - " + songM.Groups[1].Value + " (by " + authM.Groups[1].Value + ")";
					}

					string source = jsonFiles[i].DirectoryName;
					string dest = jsonFiles[i].Directory.Parent.FullName + Path.DirectorySeparatorChar + combined;

					if (source == dest) {
						continue;
					}
					Console.WriteLine("Name '" + combined + "' validated successfully.");
					Directory.Move(source, dest);
				}
			}
			Console.WriteLine("All done, press Enter to exit...");
			Console.ReadLine();
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

		private static void Open(int i) {
			current = JObject.Parse(File.ReadAllText(jsonFiles[i].FullName));

			IJEnumerable<JToken> songName = current["songName"];
			IJEnumerable<JToken> songSubName = current["songSubName"];
			IJEnumerable<JToken> authorName = current["authorName"];

			if (string.IsNullOrWhiteSpace(authorName.Value<string>()) && string.IsNullOrWhiteSpace(songSubName.Value<string>())) {
				Console.WriteLine("Both 'songSubName' and 'authorName' are empty. Manual resolution needed. Press enter to edit...");
				Console.ReadLine();
				LaunchEdit(i);
				thread.Wait();
			}
			else if (string.IsNullOrWhiteSpace(authorName.Value<string>()) && !string.IsNullOrWhiteSpace(songSubName.Value<string>())) {
				Accept.Answer a = VerifyPosition(songName.Value<string>(), "authorName", "songSubName");
				if (a == Accept.Answer.REFUSE) {
					Swap();
					current["songSubName"] = "Unknown";
				}
				else if (a == Accept.Answer.ACCEPT) {
					current["authorName"] = "Unknown";
				}
			}
			else if (string.IsNullOrWhiteSpace(songSubName.Value<string>()) && !string.IsNullOrWhiteSpace(authorName.Value<string>())) {
				Accept.Answer a = VerifyPosition(songName.Value<string>(), "authorName", "songSubName");
				if (a == Accept.Answer.REFUSE) {
					Swap();
					current["authorName"] = "Unknown";
				}
				else if(a == Accept.Answer.ACCEPT) {
					current["songSubName"] = "Unknown";
				}
			}
			else if (!string.IsNullOrWhiteSpace(songSubName.Value<string>()) && !string.IsNullOrWhiteSpace(authorName.Value<string>())) {
				Accept.Answer a = VerifyPosition(songName.Value<string>(), "authorName", "songSubName");
				if (a == Accept.Answer.REFUSE) {
					Swap();
				}
				else if (a == Accept.Answer.EDIT) {
					LaunchEdit(i);
					thread.Wait();
				}
			}

			if (!Validate(songName.Value<string>()) || !Validate(songSubName.Value<string>()) || !Validate(authorName.Value<string>())) {
				Console.WriteLine("This is NOT a problem if you do not want to rename folders for easier song recognition.");
				Accept.Answer a = Accept.AcceptQ("I want to auto-rename folders after this", true);

				if (a == Accept.Answer.ACCEPT) {
					Console.WriteLine("Removed all invalid characters");
					char[] invalid = Path.GetInvalidFileNameChars();
					foreach (char c in invalid) {
						current["songName"] = current["songName"].Value<string>().Replace("" + c, "");
						current["songSubName"] = current["songSubName"].Value<string>().Replace("" + c, "");
						current["authorName"] = current["authorName"].Value<string>().Replace("" + c, "");
					}
				}
				else if (a == Accept.Answer.EDIT) {
					LaunchEdit(i);
					thread.Wait();
				}
			}
			Console.WriteLine("File Processed!");
			File.WriteAllText(jsonFiles[i].FullName, current.ToString());
			main.Set();
		}

		private static Accept.Answer VerifyPosition(string songName, string first, string second) {
			Console.WriteLine("Song: {0} | \"{1}\" = '{2}', \"{3}\" = '{4}'", songName, dict[first], current[first], dict[second], current[second]);
			return Accept.AcceptQ("Keep?", true);
		}

		private static void Swap() {
			string temp = current["authorName"].Value<string>();
			current["authorName"] = current["songSubName"];
			current["songSubName"] = temp;
		}

		private static void LaunchEdit(int index) {
			thread.Reset();
			ProcessStartInfo info = new ProcessStartInfo(jsonFiles[index].FullName);
			Process p = Process.Start(info);
			p.EnableRaisingEvents = true;
			p.Exited += P_Exited;
		}

		private static void P_Exited(object sender, EventArgs e) {
			current = JObject.Parse(File.ReadAllText(jsonFiles[index].FullName));
			thread.Set();
		}
	}
}


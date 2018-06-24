using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace BeatSaberRenaming {
	class Program {
		private static FileInfo[] json;
		private static ManualResetEventSlim evnt = new ManualResetEventSlim();
		private static bool swapAuthorMeaning = false;

		static void Main(string[] args) {

			Console.WriteLine("This is a simple program to ease song/folder renaming");
			Console.WriteLine("Place this into ../CustomSongs/");
			Console.WriteLine();

			Console.WriteLine("This program assumes that \"authorName\" is the name of the composer, and \"songSubName\" is the map creator");
			Console.WriteLine("Use this behaviour? typing 'n' swaps the definition");

			if(Console.ReadLine() == "n") {
				swapAuthorMeaning = true;
				Console.WriteLine("Swapped!");
			}
			else {
				Console.WriteLine("Using default");
			}

			json = new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("info.json", SearchOption.AllDirectories);
			Console.WriteLine("Edit 'info.json' files?\ny/n");
			if (Console.ReadLine() == "y") {

				for (int i = 0; i < json.Length; i++) {
					Console.WriteLine(json[i].FullName);
					Thread t = new Thread(new ThreadStart(delegate { Open(i); }));
					t.Start();
					evnt.Wait();
					evnt.Reset();
				}
			}

			Console.WriteLine("Rename folders?\ny/n");
			if (Console.ReadLine() == "y") {
				for (int i = 0; i < json.Length; i++) {
					string text = File.ReadAllText(json[i].FullName);

					Regex auth = new Regex("\"authorName\": ?\"(.+)\"");
					Regex name = new Regex("\"songName\": ?\"(.+)\"");
					Regex chart = new Regex("\"songSubName\": ?\"(.+)\"");


					Match authM = auth.Match(text);
					Match songM = name.Match(text);
					Match chartM = chart.Match(text);

					bool passOne = Validate(authM.Groups[1].Value);
					bool passTwo = Validate(songM.Groups[1].Value);
					bool passThree = Validate(chartM.Groups[1].Value);

					if(!(passOne && passTwo & passThree)) {
						Console.WriteLine("\nExecution stopped, edit the invalid file and restart.");
						Console.WriteLine("Press Enter to exit and open the corrupt file...");
						Console.ReadLine();
						ProcessStartInfo info = new ProcessStartInfo(json[i].FullName);
						Process.Start(info);
						Environment.Exit(0);
					}
					string combined;

					if (!swapAuthorMeaning) {
						combined = authM.Groups[1].Value + " - " + songM.Groups[1].Value + " (by " + chartM.Groups[1].Value + ")";
					}
					else {
						combined = chartM.Groups[1].Value + " - " + songM.Groups[1].Value + " (by " + authM.Groups[1].Value + ")";
					}

					string source = json[i].DirectoryName;
					string dest = json[i].Directory.Parent.FullName + Path.DirectorySeparatorChar + combined;

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
			char[] invalidPath = Path.GetInvalidPathChars();
			char[] invalidName = Path.GetInvalidFileNameChars();

			foreach (char c in invalidPath) {
				if (text.Contains(c.ToString())) {
					Console.WriteLine("!!!!!!!!!!!");
					Console.WriteLine("While processing '" + text + "' I found '" + c + "' which is not a valid Path character");
					Console.WriteLine("!!!!!!!!!!!");
					return false;
				}
			}
			foreach (char c in invalidName) {
				if (text.Contains(c.ToString())) {
					Console.WriteLine("!!!!!!!!!!!");
					Console.WriteLine("While processing '" + text + "' I found '" + c + "' which is not a valid FileName character");
					Console.WriteLine("!!!!!!!!!!!");
					return false;
				}
			}
			return true;
		}

		private static void Open(int i) {
			ProcessStartInfo info = new ProcessStartInfo(json[i].FullName);
			Process p = Process.Start(info);
			p.EnableRaisingEvents = true;
			p.Exited += P_Exited;
		}

		private static void P_Exited(object sender, EventArgs e) {
			evnt.Set();
		}
	}
}


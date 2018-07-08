using System;
namespace BeatSaberRenaming {
	internal static class Accept {

		public enum Answer {
			REFUSE,
			ACCEPT,
			EDIT
		}
		public static Answer AcceptQ(string question, bool allowEdit) {
			Console.WriteLine(question);
			string s = "";
			while (true) {
				Console.Write("'y'/'n'");
				if (allowEdit) {
					Console.WriteLine("/'e'");
				}
				else {
					Console.WriteLine();
				}
				s = Console.ReadLine();
				if (s == "y" || s == "n" || (s == "e" && allowEdit)) {
					break;
				}
			}
			if (s == "e") {
				return Answer.EDIT;
			}
			return (Answer)(s == "y" ? 1 : 0);
		}
	}
}

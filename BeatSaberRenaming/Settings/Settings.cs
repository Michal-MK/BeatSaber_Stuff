﻿using System.Text;

namespace BeatSaberRenaming {
	public class Settings {

		public string CustomSongsFolder { get; set; } = @"E:\SteamLibrary\SteamApps\common\Beat Saber\Beat Saber_Data\CustomLevels";

		public bool CopyAfterConversion { get; set; } = true;

		public bool DeleteZips { get; set; } = true;

		public bool DeleteExtractedAndRenamed { get; set; } = false;

		public string ToFileString() {
			StringBuilder builder = new StringBuilder();
			builder.AppendLine("# Do not edit this file if you do not know what you are doing!");
			builder.AppendLine("# Path to BeatSaber CustomMaps folder in */SteamApps/");
			builder.AppendLine($"{typeof(string).Name}:{nameof(CustomSongsFolder)}={CustomSongsFolder}");

			builder.AppendLine("# Move to Custom Songs after the process completes?");
			builder.AppendLine($"{typeof(bool).Name}:{nameof(CopyAfterConversion)}={CopyAfterConversion}");

			builder.AppendLine("# Delete downoladed .zip files?");
			builder.AppendLine($"{typeof(bool).Name}:{nameof(DeleteZips)}={DeleteZips}");

			builder.AppendLine("# Delete temp files from original zips folder?");
			builder.AppendLine($"{typeof(bool).Name}:{nameof(DeleteExtractedAndRenamed)}={DeleteExtractedAndRenamed}");

			return builder.ToString();
		}
	}
}

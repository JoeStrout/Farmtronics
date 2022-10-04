
using System.IO;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics {
	static class SaveData {
		public static string SaveModPath => Path.Combine(Constants.CurrentSavePath, ModEntry.instance.ModManifest.UniqueID);
		public static string UsrDisksPath => Path.Combine(SaveModPath, "usrdisks");
		public static string NetDiskPath => Path.Combine(SaveModPath, "netdisk");
		
		public static void CreateSaveDataDirs() {
			if (!Directory.Exists(SaveModPath)) Directory.CreateDirectory(SaveModPath);
			if (!Directory.Exists(UsrDisksPath)) Directory.CreateDirectory(UsrDisksPath);
			if (!Directory.Exists(NetDiskPath)) Directory.CreateDirectory(NetDiskPath);
		}
		
		public static void CreateUsrDisk(long playerID) {
			string playerUsrDisk = Path.Combine(UsrDisksPath, playerID.ToString());
			if (!Directory.Exists(playerUsrDisk)) Directory.CreateDirectory(playerUsrDisk);
		}
		
		public static string GetUsrDiskPath(long playerID) {
			return Path.Combine(UsrDisksPath, playerID.ToString());
		}
		
		public static bool IsOldSaveDirPresent() {
			if (string.IsNullOrEmpty(Constants.CurrentSavePath)) return false;
			
			return Directory.Exists(Path.Combine(Constants.CurrentSavePath, "usrdisk"));
		}
		
		public static void MoveOldSaveDir() {
			Directory.Move(Path.Combine(Constants.CurrentSavePath, "usrdisk"), Path.Combine(UsrDisksPath, Game1.player.UniqueMultiplayerID.ToString()));
		}
	}
}
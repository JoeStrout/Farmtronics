
using System.IO;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics {
	static class SaveData {
		static string saveModPath = Path.Combine(Constants.CurrentSavePath, ModEntry.instance.ModManifest.UniqueID);
		static string usrDisksPath = Path.Combine(saveModPath, "usrdisks");	
		
		public static void CreateSaveDataDirs() {
			if (!Directory.Exists(saveModPath)) Directory.CreateDirectory(saveModPath);
			if (!Directory.Exists(usrDisksPath)) Directory.CreateDirectory(usrDisksPath);
		}
		
		public static void CreateUsrDisk(long playerID) {
			string playerUsrDisk = Path.Combine(usrDisksPath, playerID.ToString());
			if (!Directory.Exists(playerUsrDisk)) Directory.CreateDirectory(playerUsrDisk);
		}
		
		public static string GetUsrDiskPath(long playerID) {
			return Path.Combine(usrDisksPath, playerID.ToString());
		}
		
		public static bool IsOldSaveDirPresent() {
			if (string.IsNullOrEmpty(Constants.CurrentSavePath)) return false;
			
			return Directory.Exists(Path.Combine(Constants.CurrentSavePath, "usrdisk"));
		}
		
		public static void MoveOldSaveDir() {
			Directory.Move(Path.Combine(Constants.CurrentSavePath, "usrdisk"), Path.Combine(usrDisksPath, Game1.player.UniqueMultiplayerID.ToString()));
		}
	}
}
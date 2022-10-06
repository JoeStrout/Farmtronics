using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewValley;
using M1FileInfo = Farmtronics.M1.Filesystem.FileInfo;

namespace Farmtronics.M1.Filesystem {
	class DiskController {
		private static Dictionary<long, DiskController> playerDiskController = new();
		
		private long playerID;
		private Dictionary<string, Disk> disks = new();
		
		private static string GetDiskName(string path) {
			if (path.Length < 1 || path[0] != '/') return null;
			string diskName = path.Substring(1);
			int slashPos = diskName.IndexOf('/');
			if (slashPos >= 0) diskName = diskName.Substring(0, slashPos);
			// ModEntry.instance.Monitor.Log($"Returning diskName: {diskName}");
			return diskName;
		}

		public static void ClearInstances() {
			playerDiskController.Clear();
		}
		
		public static void RemovePlayer(long playerID) {
			playerDiskController.Remove(playerID);
		}
		
		public static bool ContainsPlayer(long playerID) {
			return playerDiskController.ContainsKey(playerID);
		}
		
		public static DiskController GetDiskController(long playerID) {
			if (!playerDiskController.ContainsKey(playerID)) new DiskController(playerID);
			return playerDiskController[playerID];
		}
		
		public static DiskController GetCurrentDiskController() {
			return GetDiskController(Game1.player.UniqueMultiplayerID);
		}
		
		public static string[] GetCurrentDiskNames() {
			return GetCurrentDiskController().GetDiskNames();
		}
		
		public static Disk GetCurrentDisk(ref string path) {
			return GetCurrentDiskController().GetDisk(ref path);
		}
		
		public static Disk GetDisk(ref string path, long playerID) {
			if (!playerDiskController.ContainsKey(playerID)) return null;
			
			return playerDiskController[playerID].GetDisk(ref path);
		}
		
		public DiskController(long playerID) {
			this.playerID = playerID;
			playerDiskController[playerID] = this;
		}
		
		/// <summary>
		/// Find the disk indicated by the given full path, and then strip that
		/// off, returning the rest of the path.  If the disk is not found,
		/// return null.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public Disk GetDisk(ref string path) {
			// ModEntry.instance.Monitor.Log($"Getting disk for: {path}");
			var diskName = GetDiskName(path);

			if (path.Length <= diskName.Length + 2) path = "";
			else path = path.Substring(diskName.Length + 2);
			
			if (diskName == "sys") return ModEntry.sysDisk;
			
			foreach (string volName in disks.Keys) {
				//ModEntry.instance.Monitor.Log("Checking " + volName + " -> " + disks[volName] + " against " + diskName);
				if (diskName == volName) {
					//ModEntry.instance.Monitor.Log("Matches " + disks[volName] + " with remainder " + path);
					return disks[volName];
				}
			}
			return null;
		}
		
		public string[] GetDiskNames() {
			var diskNames = disks.Keys.ToList();
			diskNames.Add("sys");
			return diskNames.ToArray();
		}
		
		public void AddDisk(string diskName, Disk disk) {
			disks[diskName] = disk;
		}

		public bool Exists(string path) {
			Disk disk = GetDisk(ref path);
			if (disk == null) return false;
			return disk.Exists(path);
		}

		public M1FileInfo GetInfo(string path) {
			Disk disk = GetDisk(ref path);
			if (disk == null) return null;
			// ModEntry.instance.Monitor.Log($"Getting info for: {path}");
			return disk.GetFileInfo(path);
		}

		/// <summary>
		/// Delete the given file.
		/// </summary>
		/// <param name="path">file to delete</param>
		/// <returns>null if successful, error message otherwise</returns>
		public string Delete(string path) {
			Disk disk = GetDisk(ref path);
			if (disk == null) return "Error: disk not found";
			if (!disk.IsWriteable()) return "Error: disk not writeable";
			string err;
			disk.Delete(path, out err);
			return err;
		}

		/// <summary>
		/// Move or copy a file from one place to another.
		/// </summary>
		/// <param name="oldPath">path of source file</param>
		/// <param name="newPath">destination path or directory</param>
		/// <param name="deleteSource">whether to delete source after copy (i.e. move) if possible</param>
		/// <param name="overwriteDest">whether to overwrite an existing file at the destination</param>
		/// <returns>null if successful, or error string if an error occurred</returns>
		public string MoveOrCopy(string oldPath, string newPath, bool deleteSource, bool overwriteDest) {
			if (newPath == oldPath) return null;    // nothing to do
			Disk oldDisk = GetDisk(ref oldPath);
			if (oldDisk == null) return "Error: source disk not found";
			Disk newDisk = GetDisk(ref newPath);
			if (newDisk == null) return "Error: target disk not found";
			if (!newDisk.IsWriteable()) return "Error: target disk is not writeable";
			if (newPath.ToLowerInvariant() == oldPath.ToLowerInvariant()) {
				// The file names (or paths) differ only in case.  This is tricky to handle
				// correctly.  On a case-insensitive file system (like most Mac and Windows
				// machines), we may be just changing the case of an existing file.  But
				// on a case-insensitive system, it may be making a new file.  Unfortunately
				// there is no easy way to tell what sort of file system we are even on.
			} else {
				bool isDir;
				if (newDisk.Exists(newPath, out isDir)) {
					if (isDir) newPath = Path.Combine(newPath, Path.GetFileName(oldPath));
					else if (!overwriteDest) return "Error: target file already exists";
				}
			}
			try {
				byte[] data = oldDisk.ReadBinary(oldPath);
				string err;
				if (deleteSource) oldDisk.Delete(oldPath, out err); // (it's actually OK if we can't delete the original)
				newDisk.WriteBinary(newPath, data);
				return null;    // Success!
			} catch (System.Exception e) {
				return e.Message;
			}
		}
	}
}
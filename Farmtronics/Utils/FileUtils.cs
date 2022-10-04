using System.Collections.Generic;
using Farmtronics.M1.Filesystem;
using M1FileInfo = Farmtronics.M1.Filesystem.FileInfo;

namespace Farmtronics.Utils {
	static class FileUtils {

		public static Dictionary<string, Disk> disks = new Dictionary<string, Disk>();

		/// <summary>
		/// Get just the last part of the given path (i.e. the file name).
		/// </summary>
		public static string GetFileName(string path) {
			int pos = path.LastIndexOf("/");
			if (pos < 0) return path;
			return path.Substring(pos + 1);
		}

		/// <summary>
		/// Get everything *except* the file name in the given path.
		/// </summary>
		public static string StripFileName(string path) {
			int pos = path.LastIndexOf("/");
			if (pos < 0) return path;
			return path.Substring(0, pos);
		}

		public static string PathCombine(string basePath, string partialPath) {
			if (!basePath.EndsWith("/")) basePath += "/";
			return basePath + partialPath;
		}

		public static bool Exists(string path) {
			Disk disk = GetDisk(ref path);
			if (disk == null) return false;
			return disk.Exists(path);
		}

		public static M1FileInfo GetInfo(string path) {
			Disk disk = GetDisk(ref path);
			if (disk == null) return null;
			return disk.GetFileInfo(path);
		}

		/// <summary>
		/// Delete the given file.
		/// </summary>
		/// <param name="path">file to delete</param>
		/// <returns>null if successful, error message otherwise</returns>
		public static string Delete(string path) {
			Disk disk = GetDisk(ref path);
			if (disk == null) return "Error: disk not found";
			if (!disk.IsWriteable()) return "Error: disk not writeable";
			string err;
			disk.Delete(path, out err);
			return err;
		}

		/// <summary>
		/// Given a (possibly partial) path, expand it to a full
		/// path from our current working directory, and resolve
		/// any . and .. entries in it to get a proper full path.
		/// If the path is invalid, return null and set error.
		/// </summary>
		public static string ResolvePath(string curdir, string path, out string error) {
			error = null;
			if (!path.StartsWith("/")) path = PathCombine(curdir, path);

			//ModEntry.instance.Monitor.Log("resolving path: " + path);
			// Simplify and then validate our full path.
			List<string> parts = new List<string>(path.Split(new char[] { '/' }));
			for (int i = 1; i < parts.Count; i++) {
				if (parts[i] == ".") {
					// indicates current directory -- skip this
					parts.RemoveAt(i);
					i--;
				} else if (parts[i] == "..") {
					// go up one level (error if we're at the root)
					if (i == 1) {
						ModEntry.instance.Monitor.Log("Wtf? " + parts[i]);
						error = "Invalid path";
						return null;
					}
					parts.RemoveAt(i);
					parts.RemoveAt(i - 1);
					i -= 2;
				}
			}
			path = string.Join("/", parts.ToArray());
			//ModEntry.instance.Monitor.Log($"resolved path to: {path}");
			return path;
		}

		/// <summary>
		/// Find the disk indicated by the given full path, and then strip that
		/// off, returning the rest of the path.  If the disk is not found,
		/// return null.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static Disk GetDisk(ref string path) {
			if (path.Length < 1 || path[0] != '/') return null;
			string diskName = path.Substring(1);
			int slashPos = diskName.IndexOf('/');
			if (slashPos >= 0) diskName = diskName.Substring(0, slashPos);

			foreach (string volName in disks.Keys) {
				//ModEntry.instance.Monitor.Log("Checking " + volName + " -> " + disks[volName] + " against " + diskName);
				if (diskName == volName) {
					if (path.Length <= volName.Length + 2) path = "";
					else path = path.Substring(volName.Length + 2);
					//ModEntry.instance.Monitor.Log("Matches " + disks[volName] + " with remainder " + path);
					return disks[volName];
				}
			}
			return null;
		}



		/// <summary>
		/// Move or copy a file from one place to another.
		/// </summary>
		/// <param name="oldPath">path of source file</param>
		/// <param name="newPath">destination path or directory</param>
		/// <param name="deleteSource">whether to delete source after copy (i.e. move) if possible</param>
		/// <param name="overwriteDest">whether to overwrite an existing file at the destination</param>
		/// <returns>null if successful, or error string if an error occurred</returns>
		public static string MoveOrCopy(string oldPath, string newPath, bool deleteSource, bool overwriteDest) {
			if (newPath == oldPath) return null;    // nothing to do
			Disk oldDisk = GetDisk(ref oldPath);
			if (oldDisk == null) return "Error: source disk not found";
			Disk newDisk = FileUtils.GetDisk(ref newPath);
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
					if (isDir) newPath = PathCombine(newPath, GetFileName(oldPath));
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

using System;
using System.Collections.Generic;
using System.IO;
using Farmtronics.M1.Filesystem;
using M1FileInfo = Farmtronics.M1.Filesystem.FileInfo;
using StardewModdingAPI;

namespace Farmtronics.Utils {
	static class DiskUtils {
		/// <summary>
		/// Given a (possibly partial) path, expand it to a full
		/// path from our current working directory, and resolve
		/// any . and .. entries in it to get a proper full path.
		/// If the path is invalid, return null and set error.
		/// </summary>
		public static string ResolvePath(string curdir, string path, out string error) {
			error = null;
			if (!path.StartsWith("/")) path = Path.Combine(curdir, path);

			ModEntry.instance.Monitor.Log("resolving path: " + path, LogLevel.Trace);
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
						ModEntry.instance.Monitor.Log("Path error: attempt to go up beyond root", LogLevel.Error);
						error = "Invalid path";
						return null;
					}
					parts.RemoveAt(i);
					parts.RemoveAt(i - 1);
					i -= 2;
				}
			}
			path = string.Join("/", parts.ToArray());
			if (path == "") path = "/";
			ModEntry.instance.Monitor.Log($"ResolvePath: resolved path to: {path}", LogLevel.Trace);
			return path;
		}
			
		private static MemoryDirectory BuildMemorySubDirectory(RealFileDisk disk, string dirPath, M1FileInfo dirInfo) {
			MemoryDirectory subDir = new() {
				DirInfo = dirInfo
			};
			foreach (var filename in disk.GetFileNames(dirPath)) {
				var filePath = Path.Combine(dirPath, filename);
				var fileInfo = disk.GetFileInfo(filePath);
				if (fileInfo.isDirectory) {
					subDir.Subdirectories.Add(filename, BuildMemorySubDirectory(disk, filePath, fileInfo));
				} else {
					subDir.Files.Add(filename, Tuple.Create(fileInfo, disk.ReadBinary(filePath)));
				}
			}
			
			return subDir;
		}
		
		public static MemoryDirectory BuildMemoryDirectory(this RealFileDisk disk) {
			MemoryDirectory memDir = new();
			// Get root directory entries
			foreach (var filename in disk.GetFileNames("")) {
				var fileInfo = disk.GetFileInfo(filename);
				if (fileInfo.isDirectory) {
					memDir.Subdirectories.Add(filename, BuildMemorySubDirectory(disk, filename, fileInfo));
				} else {
					memDir.Files.Add(filename, Tuple.Create(fileInfo, disk.ReadBinary(filename)));
				}
			}
			
			return memDir;
		}
	}
}
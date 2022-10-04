using System;
using System.Collections.Generic;
using System.Text;

namespace Farmtronics.M1.Filesystem {
	public class MemoryDirectory {
		public FileInfo DirInfo { get; set; } = new() {
			isDirectory = true	
		};
		
		public Dictionary<string, MemoryDirectory> Subdirectories {get; set;} = new();
		public Dictionary<string, Tuple<FileInfo, byte[]>> Files {get; set;} = new();
		
		public List<string> ListFiles(List<string> dirPath) {
			if (dirPath.Count == 0) {
				List<string> fileList = new();
				fileList.AddRange(Files.Keys);
				fileList.AddRange(Subdirectories.Keys);
				
				return fileList;
			} else {
				var dirKey = dirPath[0];
				dirPath.RemoveAt(0);
				
				return Subdirectories[dirKey].ListFiles(dirPath);
			}
		}
		
		public FileInfo GetFileInfo(List<string> filePath) {			
			if (filePath.Count == 0) {
				return DirInfo;
			}
			else if (filePath.Count == 1) {
				if (Files.ContainsKey(filePath[0])) {
					return Files[filePath[0]].Item1;
				}
				else if (Subdirectories.ContainsKey(filePath[0])) {
					return Subdirectories[filePath[0]].DirInfo;
				}
				
				return null;
			}
			else {
				var dirKey = filePath[0];
				filePath.RemoveAt(0);
				if (Subdirectories.ContainsKey(dirKey)) {
					return Subdirectories[dirKey].GetFileInfo(filePath);
				}
				
				return null;
			}
		}
		
		public byte[] ReadBinaryFile(List<string> filePath) {
			if (filePath.Count == 0) return null;
			else if (filePath.Count == 1) {
				if (Files.ContainsKey(filePath[0])) {
					return Files[filePath[0]].Item2;
				}
				
				return null;
			} else {
				var dirKey = filePath[0];
				filePath.RemoveAt(0);
				if (Subdirectories.ContainsKey(dirKey)) {
					return Subdirectories[dirKey].ReadBinaryFile(filePath);
				}
				
				return null;
			}
		}
		
		public string ReadTextFile(List<string> filePath) {
			if (filePath.Count == 0) return null;
			else if (filePath.Count == 1) {
				if (Files.ContainsKey(filePath[0])) {
					return Encoding.Default.GetString(Files[filePath[0]].Item2);
				}

				return null;
			} else {
				var dirKey = filePath[0];
				filePath.RemoveAt(0);
				if (Subdirectories.ContainsKey(dirKey)) {
					return Encoding.Default.GetString(Subdirectories[dirKey].ReadBinaryFile(filePath));
				}

				return null;
			}
		}

		public override string ToString() {
			var dirString = "MemoryDirectory: {";
			foreach (var dir in Subdirectories)
			{
				dirString += $"\n\tdir\t{dir.Key}";
			}
			foreach (var file in Files)
			{
				dirString += $"\n\tfile\t{file.Key}";
			}
			dirString += "\n}";
			
			return dirString;
		}
	}
}
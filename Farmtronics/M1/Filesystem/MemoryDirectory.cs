using System;
using System.Collections.Generic;
using System.Text;

namespace Farmtronics.M1.Filesystem {
	class MemoryDirectory {
		internal FileInfo dirInfo = new() {
			isDirectory = true	
		};
		
		internal Dictionary<string, MemoryDirectory> subdirectories = new();
		internal Dictionary<string, Tuple<FileInfo, byte[]>> files = new();
		
		public List<string> ListFiles(List<string> dirPath) {
			if (dirPath.Count == 0) {
				List<string> fileList = new();
				fileList.AddRange(files.Keys);
				fileList.AddRange(subdirectories.Keys);
				
				return fileList;
			} else {
				var dirKey = dirPath[0];
				dirPath.RemoveAt(0);
				
				return subdirectories[dirKey].ListFiles(dirPath);
			}
		}
		
		public FileInfo GetFileInfo(List<string> filePath) {
			if (filePath.Count == 0) {
				return dirInfo;
			}
			else if (filePath.Count == 1) {
				if (files.ContainsKey(filePath[0])) {
					return files[filePath[0]].Item1;
				}
				
				return null;
			}
			else {
				var dirKey = filePath[0];
				filePath.RemoveAt(0);
				if (subdirectories.ContainsKey(dirKey)) {
					return subdirectories[dirKey].GetFileInfo(filePath);
				}
				
				return null;
			}
		}
		
		public byte[] ReadBinaryFile(List<string> filePath) {
			if (filePath.Count == 0) return null;
			else if (filePath.Count == 1) {
				if (files.ContainsKey(filePath[0])) {
					return files[filePath[0]].Item2;
				}
				
				return null;
			} else {
				var dirKey = filePath[0];
				filePath.RemoveAt(0);
				if (subdirectories.ContainsKey(dirKey)) {
					return subdirectories[dirKey].ReadBinaryFile(filePath);
				}
				
				return null;
			}
		}
		
		public string ReadTextFile(List<string> filePath) {
			if (filePath.Count == 0) return null;
			else if (filePath.Count == 1) {
				if (files.ContainsKey(filePath[0])) {
					return Encoding.Default.GetString(files[filePath[0]].Item2);
				}

				return null;
			} else {
				var dirKey = filePath[0];
				filePath.RemoveAt(0);
				if (subdirectories.ContainsKey(dirKey)) {
					return Encoding.Default.GetString(subdirectories[dirKey].ReadBinaryFile(filePath));
				}

				return null;
			}
		}
	}
}
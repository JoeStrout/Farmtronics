using System.Collections.Generic;
using System.Linq;
using Farmtronics.Multiplayer.Messages;
using StardewModdingAPI.Utilities;

namespace Farmtronics.M1.Filesystem {
	class MemoryFileDisk : Disk {
		internal MemoryDirectory root;
		
		// Request initial data sync
		public MemoryFileDisk() {
			new SyncMemoryFileDisk().SendToHost();
		}
		
		public override bool IsWriteable() {
			return true;
		}
		
		public override FileInfo GetFileInfo(string filePath) {
			if (root == null) return null;

			return root.GetFileInfo(PathUtilities.GetSegments(filePath).ToList());
		}

		// Relative to disk root
		public override List<string> GetFileNames(string dirPath) {
			if (root == null) return null;
			
			return root.ListFiles(PathUtilities.GetSegments(dirPath).ToList());
		}

		public override byte[] ReadBinary(string filePath) {
			if (root == null) return null;
			
			return root.ReadBinaryFile(PathUtilities.GetSegments(filePath).ToList());
		}

		public override string ReadText(string filePath) {
			if (root == null) return null;
			
			return root.ReadTextFile(PathUtilities.GetSegments(filePath).ToList());
		}		

		public override void WriteText(string filePath, string text) {
			if (root == null) return;
			
			root.WriteTextFile(PathUtilities.GetSegments(filePath).ToList(), text);
		}

		public override void WriteBinary(string filePath, byte[] data) {
			if (root == null) return;
			
			root.WriteBinaryFile(PathUtilities.GetSegments(filePath).ToList(), data);
		}

		public override bool MakeDir(string dirPath, out string errMsg) {
			errMsg = "Root directory not found";
			if (root == null) return false;
			
			return root.MakeDir(PathUtilities.GetSegments(dirPath).ToList(), out errMsg);
		}

		public override bool Delete(string filePath, out string errMsg) {
			errMsg = "Root directory not found";
			if (root == null) return false;
			
			return root.Delete(PathUtilities.GetSegments(filePath).ToList(), out errMsg);
		}
	}
}
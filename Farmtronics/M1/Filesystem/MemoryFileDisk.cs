using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Utilities;

namespace Farmtronics.M1.Filesystem {
	class MemoryFileDisk : Disk {
		internal MemoryDirectory root;
		
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
			base.WriteText(filePath, text);
		}

		public override void WriteBinary(string filePath, byte[] data) {
			base.WriteBinary(filePath, data);
		}

		public override bool MakeDir(string dirPath, out string errMsg) {
			return base.MakeDir(dirPath, out errMsg);
		}

		public override bool Delete(string filePath, out string errMsg) {
			return base.Delete(filePath, out errMsg);
		}
	}
}
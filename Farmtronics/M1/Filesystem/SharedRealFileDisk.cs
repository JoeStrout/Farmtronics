using System.Text;
using Farmtronics.Multiplayer.Messages;

namespace Farmtronics.M1.Filesystem {
	class SharedRealFileDisk : RealFileDisk {
		internal string diskName;
		internal bool sendUpdate = true;
		
		public SharedRealFileDisk(string diskName, string basePath) : base(basePath) {
			this.diskName = diskName;
			this.readOnly = false;
		}

		public override void WriteText(string filePath, string text) {
			base.WriteText(filePath, text);
			
			if (sendUpdate) {
				new UpdateMemoryFileDisk() {
					DiskName = diskName,
					Action = MemoryFileDiskAction.Write,
					FilePath = filePath,
					Data = Encoding.Default.GetBytes(text)
				}.Send();	
			}
		}

		public override void WriteBinary(string filePath, byte[] data) {
			base.WriteBinary(filePath, data);

			if (sendUpdate) {
				new UpdateMemoryFileDisk() {
					DiskName = diskName,
					Action = MemoryFileDiskAction.Write,
					FilePath = filePath,
					Data = data
				}.Send();
			}
		}

		public override bool MakeDir(string dirPath, out string errMsg) {
			var result = base.MakeDir(dirPath, out errMsg);
			
			if (result && sendUpdate) {
				new UpdateMemoryFileDisk() {
					DiskName = diskName,
					Action = MemoryFileDiskAction.MakeDir,
					FilePath = dirPath
				}.Send();
			}
			
			return result;
		}

		public override bool Delete(string filePath, out string errMsg) {
			var result = base.Delete(filePath, out errMsg);
			
			if (result && sendUpdate) {
				new UpdateMemoryFileDisk() {
					DiskName = diskName,
					Action = MemoryFileDiskAction.Delete,
					FilePath = filePath
				}.Send();
			}
			
			return result;
		}
	}
}
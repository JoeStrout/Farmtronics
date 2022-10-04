using Farmtronics.M1.Filesystem;
using StardewModdingAPI;

namespace Farmtronics.Multiplayer.Messages {
	internal class UpdateMemoryFileDisk : BaseMessage<UpdateMemoryFileDisk> {
		internal Disk Disk { get; set; }
		public string DiskName { get; set; }
		public string FilePath { get; set; }
		public MemoryFileDiskAction Action { get; set; }
		public byte[] Data { get; set; }

		public override void Apply() {
			if (Disk == null) return;
			switch(Action) {
			case MemoryFileDiskAction.Write:
				Disk.WriteBinary(FilePath, Data);
				ModEntry.instance.Monitor.Log($"UpdateMemoryFileDisk Write: Finished");
				return;
			case MemoryFileDiskAction.MakeDir:
				var makeDirResult = Disk.MakeDir(FilePath, out string makeDirError);
				ModEntry.instance.Monitor.Log($"UpdateMemoryFileDisk MakeDir result: {makeDirResult} - {makeDirError}");
				return;
			case MemoryFileDiskAction.Delete:
				var deleteResult = Disk.Delete(FilePath, out string deleteError);
				ModEntry.instance.Monitor.Log($"UpdateMemoryFileDisk Delete result: {deleteResult} - {deleteError}");
				return;
			default:
				ModEntry.instance.Monitor.Log("Invalid MemoryFileDisk action provided.", LogLevel.Error);
				return;
			}
		}
	}
}
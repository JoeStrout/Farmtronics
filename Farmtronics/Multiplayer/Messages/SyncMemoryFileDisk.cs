using Farmtronics.M1.Filesystem;
using Farmtronics.Utils;

namespace Farmtronics.Multiplayer.Messages {
	internal class SyncMemoryFileDisk : BaseMessage<SyncMemoryFileDisk> {
		public string DiskName { get; set; }
		public MemoryDirectory RootDirectory { get; set; }
		
		// Host
		public void Apply(long playerID) {
			if (DiskName == "usr") {
				if (!MultiplayerManager.remoteDisks.ContainsKey(playerID)) MultiplayerManager.remoteDisks.Add(playerID, new RealFileDisk(SaveData.GetUsrDiskPath(playerID)));

				RootDirectory = MultiplayerManager.remoteDisks[playerID].BuildMemoryDirectory();	
			} else {
				var sharedDisk = FileUtils.disks[DiskName] as SharedRealFileDisk;
				if (sharedDisk == null) return;
				RootDirectory = sharedDisk.BuildMemoryDirectory();
			}
			Send(new[] { playerID });
		}

		// Client
		public override void Apply() {
			MemoryFileDisk disk = FileUtils.disks[DiskName] as MemoryFileDisk;
			if (disk == null) return;
			
			disk.root = RootDirectory;
			ModEntry.instance.Monitor.Log($"MemoryFileDisk data: {disk.root.ToString()}");
		}
	}
}
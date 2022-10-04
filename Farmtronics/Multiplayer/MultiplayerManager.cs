using System.Collections.Generic;
using System.Linq;
using Farmtronics.Bot;
using Farmtronics.M1;
using Farmtronics.M1.Filesystem;
using Farmtronics.Multiplayer.Messages;
using Farmtronics.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Farmtronics.Multiplayer {
	static class MultiplayerManager {
		internal static long hostID;
		internal static Dictionary<long, RealFileDisk> remoteDisks = new();
		internal static Dictionary<long, Shell> remoteComputer = new();
		
		public static void OnPeerContextReceived(object sender, PeerContextReceivedEventArgs e) {
			// This prevents a XML serialization error
			if (Context.IsMainPlayer) {
				BotManager.ConvertBotsToChests(false);
				remoteComputer.Remove(e.Peer.PlayerID);
			}
			BotManager.ClearAll();
			
			if (e.Peer.GetMod(ModEntry.instance.ModManifest.UniqueID) == null) {
				ModEntry.instance.Monitor.Log($"Couldn't find Farmtronics for player {e.Peer.PlayerID}. Make sure the mod is correctly installed.", LogLevel.Error);
			} else if (e.Peer.IsHost) {
				ModEntry.instance.Monitor.Log($"Found host player ID: {e.Peer.PlayerID}");
				hostID = e.Peer.PlayerID;
			}
		}
		
		public static void OnPeerConnected(object sender, PeerConnectedEventArgs e) {
			if (Context.IsMainPlayer) {
				SaveData.CreateUsrDisk(e.Peer.PlayerID);
				// At this point we can restore everything again
				BotManager.ConvertChestsToBots();
				var disk = new RealFileDisk(SaveData.GetUsrDiskPath(e.Peer.PlayerID));
				disk.readOnly = false;
				remoteDisks.Add(e.Peer.PlayerID, disk);
				InitRemoteComputer();
			}
			BotManager.InitShellAll();
		}
		
		public static void OnPeerDisconnected(object sender, PeerDisconnectedEventArgs e) {
			if (Context.IsMainPlayer) {
				remoteDisks.Remove(e.Peer.PlayerID);
				// Run home computer of disconnected player
				remoteComputer.Add(e.Peer.PlayerID, new Shell());
				remoteComputer[e.Peer.PlayerID].Init(e.Peer.PlayerID);
				// Run bots of disconnected player
				BotManager.remoteInstances.Add(e.Peer.PlayerID, BotManager.GetPlayerBotsInMap(e.Peer.PlayerID));
				foreach (BotObject bot in BotManager.remoteInstances[e.Peer.PlayerID])
				{
					bot.InitShell();
				}
			}
		}
		
		public static void InitRemoteComputer() {
			foreach (var playerID in SaveData.GetOfflinePlayersWithUsrDisk()) {
				if (!remoteComputer.ContainsKey(playerID)){
					remoteComputer.Add(playerID, new Shell());
					remoteComputer[playerID].Init(playerID);	
				}
			}
		}

		public static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e) {
			if (e.FromModID != ModEntry.instance.ModManifest.UniqueID) return;
			
			ModEntry.instance.Monitor.Log($"Receiving message from '{e.FromPlayerID}' of type: {e.Type}");

			switch (e.Type) {
			case nameof(AddBotInstance):
				AddBotInstance addBotInstance = e.ReadAs<AddBotInstance>();
				addBotInstance.Apply();
				return;
			case nameof(SyncMemoryFileDisk):
				SyncMemoryFileDisk syncMemDisk = e.ReadAs<SyncMemoryFileDisk>();
				if (Context.IsMainPlayer) syncMemDisk.Apply(e.FromPlayerID);
				else syncMemDisk.Apply();
				return;
			case nameof(UpdateMemoryFileDisk):
				UpdateMemoryFileDisk updateMemDisk = e.ReadAs<UpdateMemoryFileDisk>();
				if (Context.IsMainPlayer) {
					if (updateMemDisk.DiskName == "usr" && remoteDisks.ContainsKey(e.FromPlayerID)) {
						updateMemDisk.Disk = remoteDisks[e.FromPlayerID];
						updateMemDisk.Apply();
					} else if (updateMemDisk.DiskName != "usr") {
						var sharedDisk = FileUtils.disks[updateMemDisk.DiskName] as SharedRealFileDisk;
						if (sharedDisk != null) {
							sharedDisk.sendUpdate = false;
							updateMemDisk.Disk = sharedDisk;
							updateMemDisk.Apply();
							sharedDisk.sendUpdate = true;
						}
					}
				} else if (updateMemDisk.DiskName != "usr" && FileUtils.disks.ContainsKey(updateMemDisk.DiskName)) {
					updateMemDisk.Disk = FileUtils.disks[updateMemDisk.DiskName];
					updateMemDisk.Apply();
				}
				return;
			case nameof(AddBotChatMessage):
				AddBotChatMessage addBotChatMessage = e.ReadAs<AddBotChatMessage>();
				addBotChatMessage.Apply();
				return;
			default:
				ModEntry.instance.Monitor.Log($"Couldn't receive message of unknown type: {e.Type}", LogLevel.Error);
				return;
			}
		}

		public static void SendMessage<T>(T message, long[] playerIDs = null) where T : BaseMessage<T> {
			if (!Context.IsMultiplayer) return;
			
			if (playerIDs != null) ModEntry.instance.Monitor.Log($"Sending message: {message} to {string.Join(',', playerIDs)}");
			else ModEntry.instance.Monitor.Log($"Broadcasting message: {message}");
			ModEntry.instance.Helper.Multiplayer.SendMessage(message, typeof(T).Name, modIDs: new[] { ModEntry.instance.ModManifest.UniqueID }, playerIDs: playerIDs);
			ModEntry.instance.Monitor.Log("Message sent successfully!");
		}
		
		public static void SendMessageToHost<T>(T message) where T : BaseMessage<T> {
			SendMessage(message, new[] { hostID });
		}
	}
}
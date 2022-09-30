using Farmtronics.Multiplayer.Messages;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Farmtronics.Multiplayer {
	static class MultiplayerManager {
		public static void OnPeerContextReceived(object sender, PeerContextReceivedEventArgs e) {
			if (e.Peer.GetMod(ModEntry.instance.ModManifest.UniqueID) == null) {
				ModEntry.instance.Monitor.Log($"Couldn't find Farmtronics for player {e.Peer.PlayerID}. Make sure the mod is correctly installed.", LogLevel.Error);
			}
		}
		
		public static void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e) {
			if (e.FromModID != ModEntry.instance.ModManifest.UniqueID) return;
			
			switch (e.Type) {
			case nameof(BotScreenColorUpdate):
				BotScreenColorUpdate screenColorUpdate = e.ReadAs<BotScreenColorUpdate>();
				screenColorUpdate.Apply();
				return;
			case nameof(BotStatusColorUpdate):
				BotStatusColorUpdate statusColorUpdate = e.ReadAs<BotStatusColorUpdate>();
				statusColorUpdate.Apply();
				return;
			default:
				ModEntry.instance.Monitor.Log($"Couldn't receive message of type: {e.Type}", LogLevel.Error);
				return;
			}
		}
		
		public static void SendMessage(BaseMessage message) {
			if (!Context.IsMultiplayer) return;
			
			switch(message) {
			case BotScreenColorUpdate screenColorUpdate:
				ModEntry.instance.Helper.Multiplayer.SendMessage(screenColorUpdate, nameof(BotScreenColorUpdate), modIDs: new[] { ModEntry.instance.ModManifest.UniqueID });
				return;
			case BotStatusColorUpdate statusColorUpdate:
				ModEntry.instance.Helper.Multiplayer.SendMessage(statusColorUpdate, nameof(BotStatusColorUpdate), modIDs: new[] { ModEntry.instance.ModManifest.UniqueID });
				return;
			default:
				ModEntry.instance.Monitor.Log("Couldn't send message of unknown type.", LogLevel.Error);
				return;
			}
		}
	}
}
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
			
			ModEntry.instance.Monitor.Log($"Receiving message from '{e.FromPlayerID}' of type: {e.Type}");

			switch (e.Type) {
			case nameof(AddBotInstance):
				AddBotInstance addBotInstance = e.ReadAs<AddBotInstance>();
				addBotInstance.Apply();
				return;
			default:
				ModEntry.instance.Monitor.Log($"Couldn't receive message of type: {e.Type}", LogLevel.Error);
				return;
			}
		}

		public static void SendMessage(BaseMessage message, long[] playerIDs = null) {
			if (!Context.IsMultiplayer) return;
			
			ModEntry.instance.Monitor.Log($"Sending message: {message}");

			switch (message) {
			case AddBotInstance addBotInstance:
				ModEntry.instance.Helper.Multiplayer.SendMessage(addBotInstance, nameof(AddBotInstance), modIDs: new[] { ModEntry.instance.ModManifest.UniqueID }, playerIDs: playerIDs);
				return;
			default:
				ModEntry.instance.Monitor.Log("Couldn't send message of unknown type.", LogLevel.Error);
				return;
			}
		}
	}
}
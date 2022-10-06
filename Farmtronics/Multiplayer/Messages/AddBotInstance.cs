using System.Linq;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Multiplayer.Messages {
	class AddBotInstance : BaseMessage<AddBotInstance> {
		public string LocationName { get; set; }
		public Vector2 TileLocation { get; set; }

		public static void Send(BotObject bot) {
			var message = new AddBotInstance() {
				LocationName = bot.currentLocation.NameOrUniqueName,
				TileLocation = bot.TileLocation
			};
			message.Send(new[] {bot.owner.Value});
		}
		
		private BotObject GetBotFromLocation() {
			foreach (GameLocation location in ModEntry.instance.Helper.Multiplayer.GetActiveLocations().Where(location => location.NameOrUniqueName == LocationName)) {
				var bot = location.getObjectAtTile(TileLocation.GetIntX(), TileLocation.GetIntY()) as BotObject;
				if (bot != null) return bot;
			}
			
			return null;
		}

		public override void Apply() {
			ModEntry.instance.Monitor.Log($"Adding bot to instance list: {LocationName} - {TileLocation}");
			var bot = GetBotFromLocation();
			if (bot == null) {
				ModEntry.instance.Monitor.Log($"Could not add new bot instance. Trying again later.", LogLevel.Warn);
				if (!BotManager.lostInstances.Contains(this)) BotManager.lostInstances.Add(this);
				return;
			}
			ModEntry.instance.Monitor.Log($"Successfully added bot to instance list!", LogLevel.Info);
			BotManager.lostInstances.Remove(this);
			BotManager.instances.Add(bot);
			bot.InitShell();
		}
	}
}
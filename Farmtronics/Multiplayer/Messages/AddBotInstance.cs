using System.Linq;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Multiplayer.Messages {
	class AddBotInstance : BaseMessage {
		public string LocationName { get; set; }
		public Vector2 TileLocation { get; set; }

		public static void Send(BotObject bot) {
			var message = new AddBotInstance(bot);
			message.Send(new[] {bot.owner.Value});
		}

		public AddBotInstance() {

		}

		public AddBotInstance(BotObject bot) {
			LocationName = bot.currentLocation.NameOrUniqueName;
			TileLocation = bot.TileLocation;
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
			BotManager.lostInstances.Remove(this);
			BotManager.instances.Add(bot);
			bot.InitShell();
		}
	}
}
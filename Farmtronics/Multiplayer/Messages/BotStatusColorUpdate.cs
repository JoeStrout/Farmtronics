using System.Linq;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Farmtronics.Multiplayer.Messages {
	class BotStatusColorUpdate : BaseMessage {
		public string LocationName { get; set; }
		public Vector2 TileLocation { get; set; }
		public Color StatusColor { get; set; }

		public static void Send(BotObject bot) {
			var update = new BotStatusColorUpdate(bot);
			update.Send();
		}
		
		public BotStatusColorUpdate() {
			
		}
		
		public BotStatusColorUpdate(BotObject bot) {
			LocationName = bot.currentLocation.Name;
			TileLocation = bot.TileLocation;
			StatusColor = bot.StatusColor;
		}
		
		public override void Apply() {
			ModEntry.instance.Monitor.Log($"Applying status color update: {StatusColor}");
			GameLocation location = ModEntry.instance.Helper.Multiplayer.GetActiveLocations().Where(location => location.Name == LocationName).Single();
			var bot = location.getObjectAtTile(TileLocation.GetIntX(), TileLocation.GetIntY()) as BotObject;
			if (bot == null) return;
			bot.StatusColor = StatusColor;
		}
	}
}
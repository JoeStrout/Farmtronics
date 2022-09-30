using System.Linq;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Farmtronics.Multiplayer.Messages {
	class BotScreenColorUpdate : BaseMessage {
		public string LocationName { get; set; }
		public Vector2 TileLocation { get; set; }
		public Color ScreenColor { get; set; }

		public static void Send(BotObject bot) {
			var update = new BotScreenColorUpdate(bot);
			update.Send();
		}
		
		public BotScreenColorUpdate() {
			
		}
		
		public BotScreenColorUpdate(BotObject bot) {
			LocationName = bot.currentLocation.Name;
			TileLocation = bot.TileLocation;
			ScreenColor = bot.ScreenColor;
		}
		
		public override void Apply() {
			ModEntry.instance.Monitor.Log($"Applying screen color update: {ScreenColor}");
			GameLocation location = ModEntry.instance.Helper.Multiplayer.GetActiveLocations().Where(location => location.Name == LocationName).Single();
			var bot = location.getObjectAtTile(TileLocation.GetIntX(), TileLocation.GetIntY()) as BotObject;
			if (bot == null) return;
			bot.ScreenColor = ScreenColor;
		}
	}
}
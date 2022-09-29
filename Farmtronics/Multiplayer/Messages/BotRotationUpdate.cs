using System.Linq;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Farmtronics.Multiplayer.Messages {
	class BotRotationUpdate : BaseMessage {
		public string LocationName { get; set; }
		public Vector2 TileLocation { get; set; }
		public int FacingDirection { get; set; }

		public static void Send(BotObject bot) {
			var update = new BotRotationUpdate(bot);
			update.Send();
		}
		
		public BotRotationUpdate() {
			
		}
		
		public BotRotationUpdate(BotObject bot) {
			LocationName = bot.currentLocation.Name;
			TileLocation = bot.TileLocation;
			FacingDirection = bot.facingDirection;
		}
		
		public override void Apply() {			
			ModEntry.instance.Monitor.Log("Applying rotation update to bot");
			GameLocation location = ModEntry.instance.Helper.Multiplayer.GetActiveLocations().Where(location => location.Name == LocationName).Single();
			var bot = location.getObjectAtTile(TileLocation.GetIntX(), TileLocation.GetIntY()) as BotObject;
			if (bot == null) return;
			bot.facingDirection = FacingDirection;
		}
	}
}
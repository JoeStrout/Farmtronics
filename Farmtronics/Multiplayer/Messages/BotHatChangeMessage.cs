using System.Linq;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace Farmtronics.Multiplayer.Messages {
	class BotHatChangeMessage : BaseMessage<BotHatChangeMessage> {
		public string LocationName { get; set; }
		public Vector2 TileLocation { get; set; }
		public int HatID { get; set; }

		public static void Send(BotObject bot) {
			var hat = bot.inventory[bot.GetActualCapacity() - 1] as Hat;
			
			new BotHatChangeMessage() {
				HatID = hat != null ? hat.which.Value : -1,
				LocationName = bot.currentLocation.NameOrUniqueName,
				TileLocation = bot.TileLocation
			}.Send();
		}

		private GameLocation GetLocation() {
			return ModEntry.instance.Helper.Multiplayer.GetActiveLocations().Where(location => location.NameOrUniqueName == LocationName).Single();
		}

		private BotObject GetBotFromLocation(GameLocation location) {
			return location.getObjectAtTile(TileLocation.GetIntX(), TileLocation.GetIntY()) as BotObject;
		}
		
		private void SetHatItemSlot(BotObject bot, Item hat) {
			bot.inventory[bot.GetActualCapacity() - 1] = hat;
		}

		public override void Apply() {
			var location = GetLocation();
			var bot = GetBotFromLocation(location);
			
			if (bot == null) {
				ModEntry.instance.Monitor.Log($"Couldn't find bot at {location.NameOrUniqueName}, {TileLocation} to change hat to: {HatID}", LogLevel.Error);
				return;	
			}
			
			if (HatID >= 0) SetHatItemSlot(bot, new Hat(HatID));
			else SetHatItemSlot(bot, null);
			ModEntry.instance.Monitor.Log($"Successfully changed hat of {bot.Name} to {HatID}");
		}
	}
}
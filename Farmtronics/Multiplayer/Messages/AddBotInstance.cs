using System.Linq;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Multiplayer.Messages {
	class AddBotInstance : BaseMessage<AddBotInstance> {
		private const int maxAttempts = 3;
		private int attempt = 1;
		public string LocationName { get; set; }
		public Vector2 TileLocation { get; set; }

		public bool HasGivenUp => attempt >= maxAttempts;

		public static void Send(BotObject bot) {
			var message = new AddBotInstance() {
				LocationName = bot.currentLocation.NameOrUniqueName,
				TileLocation = bot.TileLocation
			};
			message.Send(new[] {bot.owner.Value});
		}
		
		private GameLocation GetLocation() {
			return ModEntry.instance.Helper.Multiplayer.GetActiveLocations().Single(location => location.NameOrUniqueName == LocationName);
		}
		
		private BotObject GetBotFromLocation(GameLocation location) {
			return location.getObjectAtTile(TileLocation.GetIntX(), TileLocation.GetIntY()) as BotObject;
		}

		public override void Apply() {
			var location = GetLocation();
			var bot = GetBotFromLocation(location);
			if (bot == null && attempt < maxAttempts) {
				Debug.Log($"Could not add new bot instance. Trying again later.", LogLevel.Warn);
				if (!BotManager.lostInstances.Contains(this)) BotManager.lostInstances.Add(this);
				attempt++;
				return;
			}
			else if (bot == null) {
				// Only log this message once.
				if (attempt == maxAttempts) {
					Debug.Log($"Could not add new bot instance. Aborting after {attempt} attempts.", LogLevel.Error);	
				}
				attempt++;
				return;
			}
			BotManager.lostInstances.Remove(this);
			BotManager.instances.Add(bot);
			bot.data.Load();
			bot.currentLocation = location;
			Debug.Log($"Successfully added bot to instance list: {LocationName} - {TileLocation}", LogLevel.Info);
			bot.InitShell();
		}
	}
}
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace Farmtronics.Bot {
	class BotManager {
		// Instances of bots which need updating, i.e., ones that actually exist in the world.
		internal static List<BotObject> instances = new List<BotObject>();

		/// <summary>
		/// Initializes each bot instance.
		/// Does nothing if the bot instance has already been initialized.
		/// Effectively starts up the bots.
		/// </summary>
		public static void InitShellAll() {
			foreach (var instance in instances) {
				instance.InitShell();
			}
		}

		public static void UpdateAll(GameTime gameTime) {
			for (int i = instances.Count - 1; i >= 0; i--) {
				instances[i].Update(gameTime);
			}
		}

		public static void ClearAll() {
			instances.Clear();
		}
		
		
		// Kept for compatibility
		
		//----------------------------------------------------------------------
		// Conversion of bots to chests (before saving)
		//----------------------------------------------------------------------

		// Convert all bots everywhere into vanilla chests, with appropriate metadata.
		public static void ConvertBotsToChests() {
			//ModEntry.instance.Monitor.Log("Bot.ConvertBotsToChests");
			//ModEntry.instance.Monitor.Log($"NOTE: Game1.player.recoveredItem = {Game1.player.recoveredItem}");
			int count = 0;

			// New approach: search all game locations.
			count += ConvertBotsInMapToChests();

			// Also convert the player's inventory.
			int playerBotCount = ConvertBotsInListToChests(Game1.player.Items);
			//ModEntry.instance.Monitor.Log($"Converted {playerBotCount} bots in player inventory");
			count += playerBotCount;

			// And watch out for a recoveredItem (mail attachment).
			if (Game1.player.recoveredItem is BotObject) Game1.player.recoveredItem = null;

			instances.Clear();
			//ModEntry.instance.Monitor.Log($"Total bots converted to chests: {count}");
		}

		static Chest ConvertBotToChest(BotObject bot) {
			var chest = new Chest();
			chest.Stack = bot.Stack;

			bot.SetModData(ref chest.modData);
			// Remove "energy" from the data, since this method happens at night, and
			// we actually want our bots to wake up refreshed.
			chest.modData.Remove(ModEntry.GetModDataKey(ModData.ENERGY));

			var inventory = bot.inventory;
			if (inventory != null) {
				if (chest.items.Count < inventory.Count) chest.items.Set(inventory);
				for (int i = 0; i < chest.items.Count && i < inventory.Count; i++) {
					//chest.items[i] = inventory[i];
					//ModEntry.instance.Monitor.Log($"Moved {chest.items[i]} in slot {i} from bot to chest");
				}
				int convertedItems = ConvertBotsInListToChests(chest.items);
				//if (convertedItems > 0) ModEntry.instance.Monitor.Log($"Converted {convertedItems} bots inside a bot");
				inventory.Clear();
			}
			return chest;
		}


		/// <summary>
		/// Convert all bots in the given item list into chests with the appropriate metadata.
		/// </summary>
		/// <param name="items">Item list to search in</param>
		static int ConvertBotsInListToChests(IList<Item> items) {
			int count = 0;
			for (int i = 0; i < items.Count; i++) {
				BotObject bot = items[i] as BotObject;
				if (bot == null) continue;
				items[i] = ConvertBotToChest(bot);
				//ModEntry.instance.Monitor.Log($"Converted list item {i} to {items[i]} of stack {items[i].Stack}");
				count++;
			}
			return count;
		}

		/// <summary>
		/// Convert all the bots in a map (or all maps) into chests with the appropriate metadata.
		/// </summary>
		/// <param name="inLocation">Location to search in, or if null, search all locations</param>
		public static int ConvertBotsInMapToChests(GameLocation inLocation = null) {
			if (inLocation == null) {
				int totalCount = 0;
				foreach (var loc in Game1.locations) {
					totalCount += ConvertBotsInMapToChests(loc);
				}
				return totalCount;
			}

			int countInLoc = 0;
			var targetTileLocs = new List<Vector2>();
			foreach (var kv in inLocation.objects.Pairs) {
				if (kv.Value is BotObject) targetTileLocs.Add(kv.Key);
				if (kv.Value is Chest chest) {
					//ModEntry.instance.Monitor.Log($"Found a chest in {inLocation.Name} at {kv.Key}");
					countInLoc += ConvertBotsInListToChests(chest.items);
				}
			}
			foreach (var tileLoc in targetTileLocs) {
				//ModEntry.instance.Monitor.Log($"Found bot in {inLocation.Name} at {tileLoc}; converting");
				var chest = ConvertBotToChest(inLocation.objects[tileLoc] as BotObject);
				inLocation.objects.Remove(tileLoc);
				inLocation.objects.Add(tileLoc, chest);
				countInLoc++;
			}
			//if (countInLoc > 0) ModEntry.instance.Monitor.Log($"Converted {countInLoc} bots in {inLocation.Name}");
			return countInLoc;
		}

		//----------------------------------------------------------------------
		// Conversion of chests to bots (after loading)
		//----------------------------------------------------------------------


		/// <summary>
		/// Convert all chests with appropriate metadata into bots, everywhere.
		/// </summary>
		public static void ConvertChestsToBots() {
			// Convert chests in the world.
			ConvertChestsInMapToBots();

			// Convert chests in the player's inventory.
			int count = ConvertChestsInListToBots(Game1.player.Items);
			//ModEntry.instance.Monitor.Log($"Converted {count} chests to bots in player inventory");
		}

		/// <summary>
		/// Convert all the chests with appropriate metadata into bots.
		/// </summary>
		/// <param name="inLocation">Location to search in, or if null, search all locations</param>
		static void ConvertChestsInMapToBots(GameLocation inLocation = null) {
			if (inLocation == null) {
				foreach (var loc in Game1.locations) {
					//ModEntry.instance.Monitor.Log($"Converting in location: {loc}");
					ConvertChestsInMapToBots(loc);
				}
				return;
			}
			int count = 0;
			var targetTileLocs = new List<Vector2>();
			foreach (var kv in inLocation.objects.Pairs) {
				var tileLoc = kv.Key;
				var chest = kv.Value as Chest;
				if (chest == null) continue;
				int inChestCount = ConvertChestsInListToBots(chest.items);
				//if (inChestCount > 0) ModEntry.instance.Monitor.Log($"Converted {inChestCount} chests stored in a chest into bots");

				if (!ModData.TryGetModData(chest.modData, out ModData modData) || !modData.IsBot) continue;
				targetTileLocs.Add(tileLoc);
			}
			foreach (Vector2 tileLoc in targetTileLocs) {
				var chest = inLocation.objects[tileLoc] as Chest;

				BotObject bot = new BotObject(tileLoc, inLocation);
				inLocation.objects.Remove(tileLoc);             // remove chest from "objects"
				inLocation.overlayObjects.Add(tileLoc, bot);    // add bot to "overlayObjects"

				// Apply mod data EXCEPT for energy; we want energy restored after a night
				if (!ModData.TryGetModData(chest.modData, out ModData botModData)) continue;
				bot.ApplyModData(botModData, includingEnergy: false);

				for (int i = 0; i < chest.items.Count && i < bot.inventory.Count; i++) {
					//ModEntry.instance.Monitor.Log($"Moving {chest.items[i]} from chest to bot in slot {i}");
					bot.inventory[i] = chest.items[i];
				}
				chest.items.Clear();

				count++;
				//ModEntry.instance.Monitor.Log($"Converted {chest} to {bot} at {tileLoc} of {inLocation}");
			}
			//if (count > 0) ModEntry.instance.Monitor.Log($"Converted {count} chests to bots in {inLocation}");
		}

		/// <summary>
		/// Convert all chests (with appropriate metadata) in the given item list into bots.
		/// </summary>
		/// <param name="items">Item list to search in</param>
		static int ConvertChestsInListToBots(IList<Item> items) {
			int count = 0;
			for (int i = 0; i < items.Count; i++) {
				var chest = items[i] as Chest;
				if (chest == null) continue;
				if (!ModData.TryGetModData(chest.modData, out ModData modData) || !modData.IsBot) continue;
				BotObject bot = new BotObject();
				bot.Stack = chest.Stack;
				items[i] = bot;
				// Note: we assume that chests in an item list are just items,
				// and can't themselves contain other stuff.
				count++;
			}
			return count;
		}


		//----------------------------------------------------------------------
	}
}
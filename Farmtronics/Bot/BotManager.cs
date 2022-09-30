using System.Collections.Generic;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace Farmtronics.Bot {
	class BotManager {
		// Instances of bots which need updating, i.e., ones that actually exist in the world.
		internal static List<BotObject> instances = new();

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
		
		//----------------------------------------------------------------------
		// Conversion of bots to chests (before saving)
		//----------------------------------------------------------------------

		// Convert all bots everywhere into vanilla chests, with appropriate metadata.
		public static void ConvertBotsToChests(bool saving) {
			//ModEntry.instance.Monitor.Log("Bot.ConvertBotsToChests");
			//ModEntry.instance.Monitor.Log($"NOTE: Game1.player.recoveredItem = {Game1.player.recoveredItem}");
			int count = 0;
			
			// Prevent issues with an open menu while converting bots to chests
			if (!saving && Game1.activeClickableMenu is UIMenu) Game1.exitActiveMenu();

			// New approach: search all game locations.
			count += ConvertBotsInMapToChests(saving: saving);

			// Also convert the player's inventory.
			int playerBotCount = ConvertBotsInListToChests(Game1.player.Items, saving);
			//ModEntry.instance.Monitor.Log($"Converted {playerBotCount} bots in player inventory");
			count += playerBotCount;

			// And watch out for a recoveredItem (mail attachment).
			if (Game1.player.recoveredItem is BotObject) Game1.player.recoveredItem = null;

			instances.Clear();
			//ModEntry.instance.Monitor.Log($"Total bots converted to chests: {count}");
		}

		static Chest ConvertBotToChest(BotObject bot, bool saving = true) {
			var chest = new Chest();
			ModEntry.instance.Monitor.Log($"Converting bot [owned by: {bot.owner.Value}] to chest.");
			chest.owner.Value = bot.owner.Value;
			chest.Stack = bot.Stack;

			bot.data.Save(ref chest.modData);
			// Remove "energy" from the data, since this method happens at night, and
			// we actually want our bots to wake up refreshed.
			if (saving) bot.data.RemoveEnergy(ref chest.modData);

			var inventory = bot.inventory;
			if (inventory != null) {
				if (chest.GetActualCapacity() >= bot.GetActualCapacity()) chest.items.CopyFrom(inventory);
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
		static int ConvertBotsInListToChests(IList<Item> items, bool saving = true) {
			int count = 0;
			for (int i = 0; i < items.Count; i++) {
				BotObject bot = items[i] as BotObject;
				if (bot == null) continue;
				items[i] = ConvertBotToChest(bot, saving);
				//ModEntry.instance.Monitor.Log($"Converted list item {i} to {items[i]} of stack {items[i].Stack}");
				count++;
			}
			return count;
		}

		/// <summary>
		/// Convert all the bots in a map (or all maps) into chests with the appropriate metadata.
		/// </summary>
		/// <param name="inLocation">Location to search in, or if null, search all locations</param>
		public static int ConvertBotsInMapToChests(GameLocation inLocation = null, bool saving = true) {
			if (inLocation == null) {
				int totalCount = 0;
				foreach (var loc in Game1.locations) {
					totalCount += ConvertBotsInMapToChests(loc, saving);
				}
				return totalCount;
			}

			int countInLoc = 0;
			var targetTileLocs = new List<Vector2>();
			foreach (var kv in inLocation.objects.Pairs) {
				if (kv.Value is BotObject) targetTileLocs.Add(kv.Key);
				if (kv.Value is Chest chest) {
					//ModEntry.instance.Monitor.Log($"Found a chest in {inLocation.Name} at {kv.Key}");
					countInLoc += ConvertBotsInListToChests(chest.items, saving);
				}
			}
			foreach (var tileLoc in targetTileLocs) {
				//ModEntry.instance.Monitor.Log($"Found bot in {inLocation.Name} at {tileLoc}; converting");
				var chest = ConvertBotToChest(inLocation.getObjectAtTile(tileLoc.GetIntX(), tileLoc.GetIntY()) as BotObject, saving);
				inLocation.removeObject(tileLoc, false);
				inLocation.setObject(tileLoc, chest);
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

		static BotObject ConvertChestToBot(Chest chest, Vector2 tileLocation = default, GameLocation location = null) {
			BotObject bot;
			if (tileLocation != Vector2.Zero && location != null) {
				bot = new BotObject(tileLocation, location);
			} else {
				bot = new BotObject();	
			}
			ModEntry.instance.Monitor.Log($"Converting chest [owned by: {chest.owner.Value}] to bot.");
			bot.owner.Value = chest.owner.Value;

			bot.modData.SetFromSerialization(chest.modData);
			bot.data.Load();

			if (chest.items.Count <= bot.GetActualCapacity()) {
				bot.inventory.Clear();
				for (int i = 0; i < chest.items.Count && i < bot.GetActualCapacity(); i++) {
					ModEntry.instance.Monitor.Log($"Moving {chest.items[i]} from chest to bot in slot {i}");
					bot.inventory.Add(chest.items[i]);
				}
			}
			
			chest.items.Clear();
			
			return bot;
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

				if (!ModData.IsBotData(chest.modData)) continue;
				targetTileLocs.Add(tileLoc);
			}
			foreach (Vector2 tileLoc in targetTileLocs) {
				var chest = inLocation.objects[tileLoc] as Chest;
				var bot = ConvertChestToBot(chest, tileLoc, inLocation);
				
				inLocation.removeObject(tileLoc, false);             // remove chest from "objects"
				inLocation.setObject(tileLoc, bot);    // add bot to "objects"

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
				if (!ModData.IsBotData(chest.modData)) continue;
				BotObject bot = ConvertChestToBot(chest);
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
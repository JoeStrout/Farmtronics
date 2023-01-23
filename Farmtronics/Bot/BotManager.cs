using System.Collections.Generic;
using System.Linq;
using Farmtronics.Multiplayer.Messages;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Farmtronics.Bot {
	static class BotManager {
		private static bool addedFindEvent = false;
		internal static int botCount = 0;
		// Instances of bots which need updating, i.e., ones that actually exist in the world.
		internal static List<BotObject> instances = new();
		// Host needs to run instances of other players while they aren't connected
		internal static Dictionary<long, List<BotObject>> remoteInstances = new();
		// Instances of bots which couldn't be located before (happens when a new player connects to the world)
		// NOTE: 'OnDayStarted' triggers on connect for the new player, but it's still to early to find the bots on the map.
		internal static List<AddBotInstance> lostInstances = new();
		
		public static void FindLostInstances(object sender, OneSecondUpdateTickingEventArgs args) {
			if (lostInstances.Count == 0) return;
			
			var findInstances = new List<AddBotInstance>(lostInstances);
			
			foreach (var addInstance in findInstances)
			{
				// If maxAttempts were reached it's unlikely another attempt here could find this instance.
				if (addInstance.HasGivenUp) continue;
				
				addInstance.Apply();
			}
			
			if (lostInstances.All(instance => instance.HasGivenUp)) {
				ModEntry.instance.Helper.Events.GameLoop.OneSecondUpdateTicking -= FindLostInstances;
				addedFindEvent = false;
			}
		}

		public static void FindLostInstancesOnWarp(object sender, WarpedEventArgs args) {
			if (lostInstances.Count == 0) return;
			
			var findInstances = new List<AddBotInstance>(lostInstances);
			
			// Also retry all lost instances which reached maxAttempts
			foreach (var addInstance in findInstances)
			{
				addInstance.Apply();
			}
		}
		
		public static void AddFindEvent() {
			if (!addedFindEvent && lostInstances.Count > 0) {
				addedFindEvent = true;
				ModEntry.instance.Helper.Events.GameLoop.OneSecondUpdateTicking += FindLostInstances;
			}
		}

		/// <summary>
		/// Initializes each bot instance.
		/// Does nothing if the bot instance has already been initialized.
		/// Effectively starts up the bots.
		/// </summary>
		public static void InitShellAll() {
			AddFindEvent();
						
			Debug.Log($"Initializing {instances.Count} bots!");
			foreach (var instance in instances) {
				instance.InitShell();
			}
			
			if (remoteInstances.Count > 0) {
				Debug.Log($"Initializing remote instances for {remoteInstances.Count} players!");
				foreach (var playerBots in remoteInstances.Values) {
					foreach (var bot in playerBots) {
						bot.InitShell();
					}
				}	
			}
		}

		public static void UpdateAll(GameTime gameTime) {
			for (int i = instances.Count - 1; i >= 0; i--) {
				instances[i].Update(gameTime);
			}
			foreach (var remoteBots in remoteInstances.Values) {
				foreach (var bot in remoteBots) {
					bot.Update(gameTime);
				}
			}
		}

		public static void ClearAll() {
			instances.Clear();
			remoteInstances.Clear();
		}

		public static List<BotObject> GetPlayerBotsInMap(long playerID, GameLocation inLocation = null) {
			List<BotObject> playerBots = new();
			if (inLocation == null) {
				foreach (var loc in Game1.locations) {
					playerBots.AddRange(GetPlayerBotsInMap(playerID, loc));
				}
				return playerBots;
			}

			foreach (var kv in inLocation.objects.Pairs) {
				if (kv.Value is BotObject && kv.Value.owner.Value == playerID) {
					playerBots.Add(kv.Value as BotObject);
				}
				
			}
			
			return playerBots;
		}
		
		//----------------------------------------------------------------------
		// Conversion of bots to chests (before saving)
		//----------------------------------------------------------------------

		// Convert all bots everywhere into vanilla chests, with appropriate metadata.
		public static void ConvertBotsToChests(bool saving) {
			//Debug.Log("Bot.ConvertBotsToChests");
			//Debug.Log($"NOTE: Game1.player.recoveredItem = {Game1.player.recoveredItem}");
			int count = 0;
			
			// Prevent issues with an open menu while converting bots to chests
			if (!saving && Game1.activeClickableMenu is UIMenu) Game1.exitActiveMenu();

			// New approach: search all game locations.
			if (Context.IsMainPlayer) count += ConvertBotsInMapToChests(saving: saving);

			// Also convert the player's inventory.
			int playerBotCount = ConvertBotsInListToChests(Game1.player.Items, saving);
			//Debug.Log($"Converted {playerBotCount} bots in player inventory");
			count += playerBotCount;

			// And watch out for a recoveredItem (mail attachment).
			if (Game1.player.recoveredItem is BotObject) Game1.player.recoveredItem = null;

			//Debug.Log($"Total bots converted to chests: {count}");
		}

		static Chest ConvertBotToChest(BotObject bot, bool saving = true) {
			var chest = new Chest();
			// Debug.Log($"Converting bot [owned by: {bot.owner.Value}] to chest.");
			chest.owner.Value = bot.owner.Value;
			chest.Stack = bot.Stack;

			bot.data.Update();
			bot.data.Save(ref chest.modData, saving);
			// Remove "energy" from the data, since this method happens at night, and
			// we actually want our bots to wake up refreshed.
			if (saving) bot.data.RemoveEnergy(ref chest.modData);

			var inventory = bot.inventory;
			if (inventory != null) {
				if (chest.GetActualCapacity() >= bot.GetActualCapacity()) chest.items.CopyFrom(inventory);
				int convertedItems = ConvertBotsInListToChests(chest.items);
				//if (convertedItems > 0) Debug.Log($"Converted {convertedItems} bots inside a bot");
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
				//Debug.Log($"Converted list item {i} to {items[i]} of stack {items[i].Stack}");
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
					//Debug.Log($"Found a chest in {inLocation.Name} at {kv.Key}");
					countInLoc += ConvertBotsInListToChests(chest.items, saving);
				}
			}
			foreach (var tileLoc in targetTileLocs) {
				//Debug.Log($"Found bot in {inLocation.Name} at {tileLoc}; converting");
				var chest = ConvertBotToChest(inLocation.getObjectAtTile(tileLoc.GetIntX(), tileLoc.GetIntY()) as BotObject, saving);
				inLocation.removeObject(tileLoc, false);
				inLocation.setObject(tileLoc, chest);
				countInLoc++;
			}
			//if (countInLoc > 0) Debug.Log($"Converted {countInLoc} bots in {inLocation.Name}");
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
			if (Context.IsMainPlayer) ConvertChestsInMapToBots();

			// Convert chests in the player's inventory.
			int count = ConvertChestsInListToBots(Game1.player.Items);
			//Debug.Log($"Converted {count} chests to bots in player inventory");
		}

		static BotObject ConvertChestToBot(Chest chest, Vector2 tileLocation = default, GameLocation location = null) {
			BotObject bot;
			if (tileLocation != Vector2.Zero && location != null) {
				bot = new BotObject(tileLocation, location);
			} else {
				bot = new BotObject();	
			}
			// Debug.Log($"Converting chest [owned by: {chest.owner.Value}] to bot.");
			bot.owner.Value = chest.owner.Value;
			
			// Backwards compatibility
			if (bot.owner.Value == 0) bot.owner.Value = Game1.player.UniqueMultiplayerID;

			bot.modData.SetFromSerialization(chest.modData);
			bot.data.Load();

			bot.inventory.Clear();
			for (int i = 0; i < chest.items.Count && i < bot.GetActualCapacity(); i++) {
				// Debug.Log($"Moving {chest.items[i]?.Name} from chest to bot in slot {i}");
				bot.inventory.Add(chest.items[i]);
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
					//Debug.Log($"Converting in location: {loc}");
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
				//if (inChestCount > 0) Debug.Log($"Converted {inChestCount} chests stored in a chest into bots");

				if (!ModData.IsBotData(chest.modData)) continue;
				targetTileLocs.Add(tileLoc);
			}
			foreach (Vector2 tileLoc in targetTileLocs) {
				var chest = inLocation.getObjectAtTile(tileLoc.GetIntX(), tileLoc.GetIntY()) as Chest;
				var bot = ConvertChestToBot(chest, tileLoc, inLocation);
				
				inLocation.removeObject(tileLoc, false);             // remove chest from "objects"
				inLocation.setObject(tileLoc, bot);    // add bot to "objects"

				if (bot.owner.Value == Game1.player.UniqueMultiplayerID) BotManager.instances.Add(bot);
				else if (ModEntry.instance.Helper.Multiplayer.GetConnectedPlayer(bot.owner.Value) != null) AddBotInstance.Send(bot);
				else {
					Debug.Log($"Adding bot to remote instances for playerID: {bot.owner.Value}");
					if (!BotManager.remoteInstances.ContainsKey(bot.owner.Value)) BotManager.remoteInstances.Add(bot.owner.Value, new());
					BotManager.remoteInstances[bot.owner.Value].Add(bot);
				}

				count++;
				//Debug.Log($"Converted {chest} to {bot} at {tileLoc} of {inLocation}");
			}
			//if (count > 0) Debug.Log($"Converted {count} chests to bots in {inLocation}");
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
/*
This class is a stardew valley Object subclass that represents a Bot.

*/

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;

namespace Farmtronics {
	public class Bot : StardewValley.Object {
		public IList<Item> inventory { get { return farmer == null ? null : farmer.Items; } }
		public Color screenColor = Color.Transparent;
		public Color statusColor = Color.Yellow;
		public Shell shell { get; private set; }
		public bool isUsingTool { get { return toolUseFrame > 0; } }

		public GameLocation currentLocation {
			get { return farmer.currentLocation; }
			set { farmer.currentLocation = value; }
		}
		public float energy {
			get { return farmer == null ? 0 : farmer.Stamina; }
			set { farmer.Stamina = value; }
		}
		public int facingDirection {
			get { return farmer == null ? 2 : farmer.FacingDirection; }
		}
		public int currentToolIndex {
			get { return farmer.CurrentToolIndex; }
			set {
				if (farmer != null && value >= 0 && value < inventory.Count) {
					farmer.CurrentToolIndex = value;
				}
			}
		}

		[XmlIgnore]
		public readonly NetMutex mutex = new NetMutex();

		const int vanillaObjectTypeId = 130; // "Chest"

		// mod data keys, used for saving/loading extra data with the game save:
		static class dataKey {
			public static string _prefix = $"{ModEntry.instance.ModManifest.UniqueID}/";
			public static string isBot = _prefix + "isBot";
			public static string facing = _prefix + "facing";
			public static string energy = _prefix + "energy";
			public static string name = _prefix + "name";
		}

		// We need a Farmer to be able to use tools.  So, we're going to
		// create our own invisible Farmer instance and store it here:
		Farmer farmer;

		Vector2 position; // our current position, in pixels
		Vector2 targetPos;  // position we're moving to, in pixels

		static List<Bot> instances = new List<Bot>();

		const string NAME_PREFIX = "Bot ";
		public int maxEnergy {
			get { return 1000; }
		}
		public int Speed { // pixels/sec
			get { return farmer.Speed; }
			set { farmer.Speed = value; }
		}

		int toolUseFrame = 0;		// > 0 when using a tool

		static Texture2D botSprites;
		
		public static List<Item> defaultInventories() {
			return new List<Item> {
				new Hoe(),
				new Axe(),
				new Pickaxe(),
				new MeleeWeapon(47),  // (scythe)
				new WateringCan()
			};
		}

		public static string defaultName() {
			return "Farmtronics Bot";
		}

		public static string getUniqueName() {
			HashSet<string> names = new HashSet<string>();
			foreach (var bot in instances) {
				names.Add(bot.Name);
			}
			for (int botIndex = 1; ; botIndex++) {
				string name = NAME_PREFIX + botIndex;
				if (!names.Contains(name)) return name;
			}
		}

		public static bool isNameValid(string name) {
			if (string.IsNullOrEmpty(name)) return false;
			// Avoid duplicate names so we can reference a bot with its name
			foreach (Bot bot in instances) {
				if (bot.Name == name) return false;
            }
			return true;
        }

		public Bot(
				Chest chest = null,
				GameLocation location = null,
				Vector2? tileLocation = null,
				int facing = 2,
				string name = null,
				float energy = float.PositiveInfinity,
				IList<Item> inventory = null
		): base(tileLocation ?? Vector2.Zero, vanillaObjectTypeId) {
			Debug.Log($"Creating Bot: {Environment.StackTrace}");

			if (botSprites == null) {
				botSprites = ModEntry.helper.Content.Load<Texture2D>("assets/BotSprites.png");
			}

			type.Value = "Crafting";
			bigCraftable.Value = true;
			canBeSetDown.Value = true;

			if (chest != null) inventory = chest.items;
			else if (inventory == null) inventory = defaultInventories();
            farmer = new Farmer(new FarmerSprite("Characters\\Farmer\\farmer_base"),
				position, 64,
				Name, new List<Item>(inventory), isMale: true);
			currentLocation = location;
			farmer.MaxStamina = maxEnergy;

			if (chest != null) {
				ApplyModData(chest.modData);
			} else {
				farmer.faceDirection(facing);
				Name = name ?? defaultName();
				farmer.stamina = MathF.Min(energy, maxEnergy);
            }

			TileLocation = tileLocation ?? Vector2.Zero;
			position = targetPos = TileLocation * 64f;

			instances.Add(this);
		}

		//----------------------------------------------------------------------
		// Storage/retrieval of bot data in a modData dictionary
		//----------------------------------------------------------------------

		/// <summary>
		/// Fill in the given ModDataDictionary with values from this bot,
		/// so they can be saved and restored later.
		/// </summary>
		void SetModData(ModDataDictionary d) {
			d[dataKey.isBot] = "1";
			d[dataKey.name] = name;
			d[dataKey.energy] = energy.ToString();
			d[dataKey.facing] = facingDirection.ToString();
		}

		/// <summary>
		/// Apply the values in the given ModDataDictionary to this bot,
		/// configuring name, energy, etc.
		/// </summary>
		void ApplyModData(ModDataDictionary d) {
			if (!d.GetBool(dataKey.isBot)) {
				Debug.Log("ERROR: ApplyModData called with modData where isBot is not true!");
			}
			string modDataName = d.GetString(dataKey.name, null);
			if (!string.IsNullOrEmpty(modDataName)) Name = modDataName;
			else Name = getUniqueName();
			if (float.TryParse(d.GetString(dataKey.energy, null), out float parsedEnergy)) energy = parsedEnergy;
            else energy = maxEnergy;
			farmer.faceDirection(d.GetInt(dataKey.facing, facingDirection));
			Debug.Log($"after ApplyModData, name=[{name}]");
		}

		//----------------------------------------------------------------------
		// Conversion of bots to chests (before saving)
		//----------------------------------------------------------------------

		public static bool IsBotChest(Chest chest) {
			string s = null;
			chest.modData.TryGetValue(dataKey.isBot, out s);
			return !string.IsNullOrEmpty(s);
		}

		// Convert all bots everywhere into vanilla chests, with appropriate metadata.
		public static void ConvertBotsToChests() {
			Debug.Log("Bot.ConvertBotsToChests");
			Debug.Log($"NOTE: Game1.player.recoveredItem = {Game1.player.recoveredItem}");
			int count = 0;

			// New approach: search all game locations.
			count += ConvertBotsInMapToChests();

			// Also convert the player's inventory.
			int playerBotCount = ConvertBotsInListToChests(Game1.player.Items);
			Debug.Log($"Converted {playerBotCount} bots in player inventory");
			count += playerBotCount;

			// And watch out for a recoveredItem (mail attachment).
			if (Game1.player.recoveredItem is Bot) Game1.player.recoveredItem = null;

			instances.Clear();
			Debug.Log($"Total bots converted to chests: {count}");
		}

		static int ConvertBotToChest(Bot bot, out Chest chest) {
			chest = new Chest();
			chest.Stack = bot.Stack;
			bot.SetModData(chest.modData);
			chest.items.CopyFrom(bot.inventory);
            return ConvertBotsInListToChests(chest.items) + 1;
		}

		static int ConvertChestToBot(Chest chest, GameLocation loc, Vector2 tileLoc, out Bot bot) {
            int nested = ConvertChestsInListToBots(chest.items);
            bot = new Bot(
				chest: chest,
				tileLocation: tileLoc,
				location: loc
			);
			bot.Stack = chest.Stack;

			Debug.Log($"Converted {chest} to {bot} at {bot.TileLocation} of {loc?.Name}");
			return nested + 1;
		}


		/// <summary>
		/// Convert all bots in the given item list into chests with the appropriate metadata.
		/// </summary>
		/// <param name="items">Item list to search in</param>
		static int ConvertBotsInListToChests(IList<Item> items) {
			int count = 0;
			for (int i=0; i<items.Count; i++) {
				Bot bot = items[i] as Bot;
				if (bot == null) continue;
				Chest chest;
				count += ConvertBotToChest(bot, out chest);
				items[i] = chest;
				Debug.Log($"Converted list item {i} to {items[i]} of stack {items[i].Stack}");
			}
			return count;
		}

		/// <summary>
		/// Convert all the bots in a map (or all maps) into chests with the appropriate metadata.
		/// </summary>
		/// <param name="inLocation">Location to search in, or if null, search all locations</param>
		public static int ConvertBotsInMapToChests(GameLocation inLocation=null) {
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
				if (kv.Value is Bot bot) {
					targetTileLocs.Add(kv.Key);
					ConvertBotsInListToChests(bot.inventory);
				} else if (kv.Value is Chest chest) {
					Debug.Log($"Found a chest in {inLocation.Name} at {kv.Key}");
					countInLoc += ConvertBotsInListToChests(chest.items);
				}
			}
			foreach (var tileLoc in targetTileLocs) {
				Debug.Log($"Found bot in {inLocation.Name} at {tileLoc}; converting");
				Chest chest;
				ConvertBotToChest(inLocation.objects[tileLoc] as Bot, out chest);
				inLocation.objects.Remove(tileLoc);
				inLocation.objects.Add(tileLoc, chest);
				countInLoc++;
			}
			if (countInLoc > 0) Debug.Log($"Converted {countInLoc} bots in {inLocation.Name}");
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
			Debug.Log($"Converted {count} chests to bots in player inventory");
		}

		/// <summary>
		/// Convert all the chests with appropriate metadata into bots.
		/// </summary>
		/// <param name="inLocation">Location to search in, or if null, search all locations</param>
		static int ConvertChestsInMapToBots(GameLocation inLocation=null) {
			int count = 0;
			if (inLocation == null) {
				foreach (var loc in Game1.locations) {
					//Debug.Log($"Converting in location: {loc}");
					count += ConvertChestsInMapToBots(loc);
				}
				return count;
			}
			var targetTileLocs = new List<Vector2>();
			foreach (var kv in inLocation.objects.Pairs) {
				var tileLoc = kv.Key;
				var chest = kv.Value as Chest;
				if (chest == null) continue;
				if (IsBotChest(chest)) targetTileLocs.Add(tileLoc);
				else count += ConvertChestsInListToBots(chest.items);
			}
			foreach (Vector2 tileLoc in targetTileLocs) {
				var chest = inLocation.objects[tileLoc] as Chest;
				Bot bot;
				count += ConvertChestToBot(chest, inLocation, tileLoc, out bot);
				inLocation.objects.Remove(tileLoc); // remove chest from "objects"
				inLocation.overlayObjects.Add(bot.TileLocation, bot); // add bot to "overlayObjects"
			}
			if (count > 0) Debug.Log($"Converted {count} chests to bots in {inLocation}");
			return count;
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
				if (IsBotChest(chest)) {
					Bot bot;
					count += ConvertChestToBot(chest, null, Vector2.Zero, out bot);
					items[i] = bot;
				}
			}
			return count;
		}


		//----------------------------------------------------------------------

		public static void UpdateAll(GameTime gameTime) {
			for (int i=instances.Count - 1; i >= 0; i--) {
				instances[i].Update(gameTime);
			}
		}

		public static void ClearAll() {
			instances.Clear();
		}

		public override void dropItem(GameLocation location, Vector2 origin, Vector2 destination) {
			Debug.Log($"Bot.dropItem({location}, {origin}, {destination}");
			base.dropItem(location, origin, destination);
		}

		public override bool performDropDownAction(Farmer who) {
			Debug.Log($"Bot.performDropDownAction({who.Name})");
			base.performDropDownAction(who);

			// Keep our farmer positioned wherever this object is
			farmer.currentLocation = Game1.player.currentLocation;
			farmer.setTileLocation(TileLocation);
			return false;	// OK to set down (add to Objects list in the tile)
		}

		/// <summary>
		/// placementAction is called when the player, who is carring a Bot item, indicates
		/// that they want to place it down.  The item is going to be destroyed; we have
		/// to create a new Bot instance that matches its data.
		/// </summary>
		public override bool placementAction(GameLocation location, int x, int y, Farmer who = null) {
			Debug.Log($"Bot.placementAction({location}, {x}, {y}, {who?.Name})");
			var bot = new Bot(
				name: name == defaultName() ? getUniqueName() : name,
				tileLocation: new Vector2((int)(x / 64), (int)(y / 64)),
				facing: who.facingDirection,
				location: location,
				energy: energy,
				inventory: inventory
			);
			bot.currentLocation.overlayObjects.Add(bot.TileLocation, bot);
			bot.shakeTimer = 50;

			instances.Remove(this);
			location.playSound("hammer");
			return true;
		}

		// Apply the currently-selected item as a tool (or weapon) on
		// the square in front of the bot.
		public void UseTool() {
			if (farmer == null || inventory == null) return;
			Tool tool = inventory[currentToolIndex] as Tool;
			if (tool == null) return;
			int useX = (int)position.X + 64 * DxForDirection(farmer.FacingDirection);
			int useY = (int)position.Y + 64 * DyForDirection(farmer.FacingDirection);
			tool.beginUsing(currentLocation, useX, useY, farmer);

			farmer.setTileLocation(TileLocation);
			Farmer.showToolSwipeEffect(farmer);

			// Count how many frames into the swipe effect we are.
			// We'll actually apply the tool effect later, in Update.
			toolUseFrame = 1;
		}

		// Attempt to harvest the crop in front of the bot.
		public bool Harvest() {
			if (farmer == null) return false;
			farmer.setTileLocation(TileLocation);

			GameLocation loc = this.currentLocation;
			int aheadX = (int)position.X + 64 * DxForDirection(farmer.FacingDirection);
			int aheadY = (int)position.Y + 64 * DyForDirection(farmer.FacingDirection);
			Vector2 tileLocation = new Vector2(aheadX/64, aheadY/64);
			TerrainFeature feature = null;
			if (!loc.terrainFeatures.TryGetValue(tileLocation, out feature)) return false;

			var origPlayer = Game1.player;
			Game1.player = farmer;
			bool result = feature.performUseAction(tileLocation, loc);
			Game1.player = origPlayer;

			return result;
		}

		// Place the currently selected item (e.g., seed) in/on the ground
		// ahead of the robot.
		public bool PlaceItem() {
			var item = inventory[currentToolIndex] as StardewValley.Object;
			if (item == null) {
				Debug.Log($"No item equipped in slot {currentToolIndex}");
				return false;
			}
			int placeX = (int)position.X + 64 * DxForDirection(farmer.FacingDirection);
			int placeY = (int)position.Y + 64 * DyForDirection(farmer.FacingDirection);
			Vector2 tileLocation = new Vector2(placeX/64, placeY/64);

			// make sure we can place the item here
			if (!Utility.playerCanPlaceItemHere(farmer.currentLocation, item, placeX, placeY, farmer)) {
				Debug.Log($"Can't place {item} (stack size {item.Stack}) at {placeX},{placeY}");
				return false;
			}

			// place it
			bool result = item.placementAction(currentLocation, placeX, placeY, farmer);
			//Debug.Log($"Placed {item} (from stack of {item.Stack}) at {placeX},{placeY}: {result}");

			// reduce inventory by one
			item.Stack--;
			if (item.Stack <= 0) inventory[currentToolIndex] = null;

			return true;
		}

		public void Move(int dColumn, int dRow) {
			// Face in the specified direction
			if (dRow < 0) farmer.faceDirection(0);
			else if (dRow > 0) farmer.faceDirection(2);
			else if (dColumn < 0) farmer.faceDirection(3);
			else if (dColumn > 0) farmer.faceDirection(1);

			// make sure the terrain in that direction isn't blocked
			Vector2 newTile = farmer.getTileLocation() + new Vector2(dColumn, dRow);
			var location = currentLocation;
			{
				// How to detect walkability in pretty much the same way as other characters:
				var newBounds = farmer.GetBoundingBox();
				newBounds.X += dColumn * 64;
				newBounds.Y += dRow * 64;
				bool coll = location.isCollidingPosition(newBounds, Game1.viewport, isFarmer:false, 0, glider:false, farmer);
				if (coll) {
					Debug.Log("Colliding position: " + newBounds);
					return;
				}
			}

			// start moving
			targetPos = newTile * 64;

			// Do collision actions (shake the grass, etc.)
			if (location.terrainFeatures.ContainsKey(newTile)) {
				ModEntry.instance.print($"Shaking terrain feature at {newTile}");
				//Rectangle posRect = new Rectangle((int)position.X-16, (int)position.Y-24, 32, 48);
				var feature = location.terrainFeatures[newTile];
				var posRect = feature.getBoundingBox(newTile);
				feature.doCollisionAction(posRect, 4, newTile, null, location);
			}
		}

		public static int DxForDirection(int direction) {
			if (direction == 1) return 1;
			if (direction == 3) return -1;
			return 0;
		}

		public static int DyForDirection(int direction) {
			if (direction == 2) return 1;
			if (direction == 0) return -1;
			return 0;
		}

		public void MoveForward() {
			Move(DxForDirection(farmer.FacingDirection), DyForDirection(farmer.FacingDirection));
			Debug.Log($"{Name} MoveForward() went position to {position}, tileLocation to {TileLocation}; facing {farmer.FacingDirection}");
		}

		public bool IsMoving() {
			return (position != targetPos);
		}

		public void Rotate(int stepsClockwise) {
			farmer.faceDirection((farmer.FacingDirection + 4 + stepsClockwise) % 4);
			Debug.Log($"{Name} Rotate({stepsClockwise}): now facing {farmer.FacingDirection}");
		}

		void ApplyToolToTile() {
			// Actually apply the tool to the tile in front of the bot.
			// This is a big pain in the neck that is duplicated in many of the Tool subclasses.
			// Here's how we do it:
			// First, get the tool to apply, and the tile location to apply it.
			if (farmer == null || inventory == null) return;
			Tool tool = inventory[currentToolIndex] as Tool;
			int tileX = (int)position.X / 64 + DxForDirection(farmer.FacingDirection);
			int tileY = (int)position.Y / 64 + DyForDirection(farmer.FacingDirection);
			Vector2 tile = new Vector2(tileX, tileY);
			var location = currentLocation;

			// If it's not a MeleeWeapon, call the easy method and let SDV handle it.
			if (tool is not MeleeWeapon) {
				Game1.toolAnimationDone(farmer);
				return;
			}

			// Otherwise, big pain in the neck time.

			// Apply it to the location itself.
			Debug.Log($"{name} Performing {tool} action at {tileX},{tileY}");
			location.performToolAction(tool, tileX, tileY);

			// Then, apply it to any terrain feature (grass, weeds, etc.) at this location.
			if (location.terrainFeatures.ContainsKey(tile) && location.terrainFeatures[tile].performToolAction(tool, 0, tile, location)) {
				Debug.Log($"Performed tool action on the terrain feature {location.terrainFeatures[tile]}; removing it");
				location.terrainFeatures.Remove(tile);
			}
			if (location.largeTerrainFeatures is not null) {
				var tileRect = new Rectangle(tileX*64, tileY*64, 64, 64);
				for (int i = location.largeTerrainFeatures.Count - 1; i >= 0; i--) {
					if (location.largeTerrainFeatures[i].getBoundingBox().Intersects(tileRect) && location.largeTerrainFeatures[i].performToolAction(tool, 0, tile, location)) {
						Debug.Log($"Performed tool action on the LARGE terrain feature {location.terrainFeatures[tile]}; removing it");
						location.largeTerrainFeatures.RemoveAt(i);
					}
				}
			}

			// Finally, apply to any object sitting on this tile.
			if (location.Objects.ContainsKey(tile)) {
				var obj = location.Objects[tile];
				if (obj != null && obj.Type != null && obj.performToolAction(tool, location)) {
					if (obj.Type.Equals("Crafting") && (int)obj.Fragility != 2) {
						var center = farmer.GetBoundingBox().Center;
						Debug.Log($"Performed tool action on the object {obj}; adding debris");
						location.debris.Add(new Debris(obj.bigCraftable.Value ? (-obj.ParentSheetIndex) : obj.ParentSheetIndex,
							farmer.GetToolLocation(), new Vector2(center.X, center.Y)));
					}
					Debug.Log($"Performing {obj} remove action, then removing it from {tile}");
					obj.performRemoveAction(tile, location);
					location.Objects.Remove(tile);
				}
			}

		}

		public void Update(GameTime gameTime) {
			// Weird things happen if we try to update bots in locations other than
			// the current location.  We should try harder to get that to work sometime,
			// but for now, let's just detect that case and bail out.
			if (farmer.currentLocation != Game1.currentLocation) return;

			if (shell != null) {
				shell.console.update(gameTime);
			}
			if (toolUseFrame > 0) {
				toolUseFrame++;
				if (toolUseFrame == 6) ApplyToolToTile();
				else if (toolUseFrame == 12) toolUseFrame = 0;	// all done!
			}

			if (position != targetPos) {
				// ToDo: make a utility module with MoveTowards in it
				position.X += MathF.Sign(targetPos.X - position.X);
				position.Y += MathF.Sign(targetPos.Y - position.Y);
				Vector2 newTile = new Vector2((int)position.X / 64, (int)position.Y / 64);
				if (newTile != TileLocation) {
					// Remove this object from the Objects list at its old position
					var location = currentLocation;
					location.overlayObjects.Remove(TileLocation);
					// Update our tile pos, and add this object to the Objects list at the new position
					TileLocation = newTile;
					location.overlayObjects.Add(newTile, this);
					// Update the invisible farmer
					farmer.setTileLocation(newTile);
				}
				//Debug.Log($"Updated position to {position}, tileLocation to {TileLocation}; facing {farmer.FacingDirection}");
			}
		}

		public override string getDescription() {
			return "A programmable mechanical wonder.";
		}

		protected override string loadDisplayName() {
			return name;
		}

		public override bool checkForAction(Farmer who, bool justCheckingForActivity = false) {
			//Debug.Log($"Bot.checkForAction({who.Name}, {justCheckingForActivity}), tool {who.CurrentTool}");
			if (justCheckingForActivity) return true;
			// all this overriding... just to change the open sound.
			if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true)) {
				Debug.Log($"Bailing because didPlayerJustRightClick is false");
				return false;
			}

			// ToDo: use mutex to ensure only one player can open a bot at a time.
			// (Tried, but couldn't get to work:
			//Debug.Log($"Requesting mutex lock: {mutex}, IsLocked={mutex.IsLocked()}, IsLockHeld={mutex.IsLockHeld()}");
			//mutex.RequestLock(delegate {
			//	Game1.playSound("bigSelect");
			//	Game1.player.Halt();
			//	Game1.player.freezePause = 1000;
			//	ShowMenu();
			//}, delegate {
			//	Debug.Log("Failed to get mutex lock :(");
			//});

			// For now, just dewit:
			Game1.playSound("bigSelect");
			Game1.player.Halt();
			Game1.player.freezePause = 1000;
			ShowMenu();
		
			return true;
		}

		public override bool performToolAction(Tool t, GameLocation location) {
			Debug.Log($"{name} Bot.performToolAction({t}, {location})");

		   if (t is Pickaxe or Axe or Hoe) {
				Debug.Log("{name} Bot.performToolAction: creating custom debris");
				var who = t.getLastFarmerToUse();
				this.performRemoveAction(this.TileLocation, location);
				Debris deb = new Debris(this.getOne(), who.GetToolLocation(), new Vector2(who.GetBoundingBox().Center.X, who.GetBoundingBox().Center.Y));
				Game1.currentLocation.debris.Add(deb);
				Debug.Log($"{name} Created debris with item {deb.item}");
				// Remove, stop, and destroy this bot
				Game1.currentLocation.overlayObjects.Remove(this.TileLocation);
				if (shell != null) shell.interpreter.Stop();
				instances.Remove(this);
				return false;
			}

			// previous code, that called the base... this sometimes resulted
			// in picking up a chest, while leaving a ghost bot behind:
			//bool result = base.performToolAction(t, location);
			//Debug.Log($"{name} Bot.performToolAction: My TileLocation is now {this.TileLocation}");
			//return result;
			// I'm not aware of any use case for doing the default tool action on a bot.
			// So now we're going to avoid that whole issue by always doing:
			return false;
		}

		/// <summary>
		/// I think this is called when the player tries to drop something into the bot
		/// (or might be considering it, e.g., is hovering over the bot with an item).
		/// But it's still not entirely clear; it also seems to get called when I'm holding
		/// a bot and hovering over various tiles on the ground.
		/// </summary>
		/// <param name="dropIn"></param>
		/// <param name="probe"></param>
		/// <param name="who"></param>
		/// <returns></returns>
		public override bool performObjectDropInAction(Item dropIn, bool probe, Farmer who) {
			Debug.Log($"{name} Bot.performObjectDropInAction({dropIn}, {probe}, {who.Name}");
			return base.performObjectDropInAction(dropIn, probe, who);
		}

		public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1) {
			//ModEntry.instance.print($"draw 1 at {x},{y}, {alpha}");

			if (alpha < 0.9f) {
				// Drawing with alpha=0.5 is done when the player is placing the bot down
				// in the world.  In this case, our internal position doesn't matter;
				// we want to update that to match the given tile position.
				position.X = x * 64;
				position.Y = y * 64;
				targetPos = position;
			}

			// draw shadow
			spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport,
				new Vector2(position.X + 32, position.Y + 51 + 4)),
				Game1.shadowTexture.Bounds, Color.White * alpha, 0f,
				new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f,
				SpriteEffects.None, (float)getBoundingBox(new Vector2(x, y)).Bottom / 15000f);

			// draw sprite
			if (botSprites == null) {
				Debug.Log("Bot.draw: botSprites is null; bailing out");
				return;
			}

			Vector2 position3 = Game1.GlobalToLocal(Game1.viewport, new Vector2(
				position.X + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
				position.Y + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)));
			// Note: FacingDirection 0-3 is Up, Right, Down, Left
			int facing = 2;
			if (farmer != null) facing = farmer.FacingDirection;
			Rectangle srcRect = new Rectangle(16 * facing, 0, 16, 24);
			Vector2 origin2 = new Vector2(8f, 8f);
			float scale = (this.scale.Y > 1f) ? getScale().Y : 4f;
			float z = (float)(getBoundingBox(new Vector2(x, y)).Bottom) / 10000f;
			// base sprite
			spriteBatch.Draw(botSprites, position3, srcRect, Color.White * alpha, 0f,
				origin2, scale, SpriteEffects.None, z);
			// screen color (if not black or clear)
			if (screenColor.A > 0 && (screenColor.R > 0 || screenColor.G > 0 || screenColor.B > 0)) {
				srcRect.Y = 24;
				spriteBatch.Draw(botSprites, position3, srcRect, screenColor * alpha, 0f,
					origin2, scale, SpriteEffects.None, z + 0.001f);
			}
			// screen shine overlay
			srcRect.Y = 48;
			spriteBatch.Draw(botSprites, position3, srcRect, Color.White * alpha, 0f,
				origin2, scale, SpriteEffects.None, z + 0.002f);
			// status light color (if not black or clear)
			if (statusColor.A > 0 && (statusColor.R > 0 || statusColor.G > 0 || statusColor.B > 0)) {
				srcRect.Y = 72;
				spriteBatch.Draw(botSprites, position3, srcRect, statusColor * alpha, 0f,
					origin2, scale, SpriteEffects.None, z + 0.002f);
			}

		}

		public override void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1) {
			//ModEntry.instance.print($"draw 2 at {xNonTile},{yNonTile}, {layerDepth}, {alpha}");
			base.draw(spriteBatch, xNonTile, yNonTile, layerDepth, alpha);
		}

		/// <summary>
		/// Draw the bot as it should appear above the player's head when held.
		/// </summary>
		public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
			//Debug.Log($"Bot.drawWhenHeld");
			if (botSprites == null) {
				Debug.Log("Bot.drawWhenHeld: botSprites is null; bailing out");
				return;
			}
			Rectangle srcRect = new Rectangle(16 * f.facingDirection, 0, 16, 24);
			spriteBatch.Draw(botSprites, objectPosition, srcRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, Math.Max(0f, (float)(f.getStandingY() + 3) / 10000f));
		}

		public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
			if (botSprites == null) {
				Debug.Log("Bot.drawInMenu: botSprites is null; bailing out");
				return;
			}
			//Debug.Log($"Bot.drawInMenu with scaleSize {scaleSize}");
			if ((bool)this.IsRecipe) {
				transparency = 0.5f;
				scaleSize *= 0.75f;
			}
			bool shouldDrawStackNumber = ((drawStackNumber == StackDrawType.Draw && this.maximumStackSize() > 1 && this.Stack > 1)
				|| drawStackNumber == StackDrawType.Draw_OneInclusive) && (double)scaleSize > 0.3 && this.Stack != int.MaxValue;

			Rectangle srcRect = new Rectangle(0, 112, 16, 16);
			spriteBatch.Draw(botSprites, location + new Vector2((int)(32f * scaleSize), (int)(32f * scaleSize)), srcRect, color * transparency, 0f,
				new Vector2(8f, 8f) * scaleSize, 4f * scaleSize, SpriteEffects.None, layerDepth);

			if (shouldDrawStackNumber) {
				var loc = location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(this.Stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 2f);
				Utility.drawTinyDigits(this.Stack, spriteBatch, loc, 3f * scaleSize, 1f, color);
			}
		}

		public override void drawAsProp(SpriteBatch b) {
			//Debug.Log($"Bot.drawAsProp");
			if (botSprites == null) {
				Debug.Log("Bot.drawAsProp: botSprites is null; bailing out");
				return;
			}
			if (this.isTemporarilyInvisible) return;
			int x = (int)this.TileLocation.X;
			int y = (int)this.TileLocation.Y;

			Vector2 scaleFactor = Vector2.One; // this.PulseIfWorking ? this.getScale() : Vector2.One;
			scaleFactor *= 4f;
			Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 48));
			Rectangle srcRect = new Rectangle(16 * 2, 0, 16, 24);
			b.Draw(destinationRectangle: new Rectangle((int)(position.X - scaleFactor.X / 2f), (int)(position.Y - scaleFactor.Y / 2f),
				(int)(64f + scaleFactor.X), (int)(128f + scaleFactor.Y / 2f)),
				texture: botSprites,
				sourceRectangle: srcRect,
				color: Color.White,
				rotation: 0f,
				origin: Vector2.Zero,
				effects: SpriteEffects.None,
				layerDepth: Math.Max(0f, (float)((y + 1) * 64 - 1) / 10000f));
		}

		/// <summary>
		/// This method is called to get an "Item" (something that can be carried) from this Bot.
		/// Since Bot is an Object and Objects are Items, we can just return another Bot, but
		/// for some reason we can't just return *this* bot.
		/// </summary>
		/// <returns></returns>
		public override Item getOne() {
			Bot bot = new Bot(
				name: name,
				tileLocation: tileLocation,
				location: null,
				energy: energy,
				inventory: inventory
			);
			bot._GetOneFrom(this);
			return bot;
		}

		// we should not stack bots, as otherwise inventories will not be kept
		public override int maximumStackSize() {
			return 1;
		}

		public int GetActualCapacity() {
			return 12;
		}

		public void ShowMenu() {
			ModEntry.instance.print($"{Name} ShowMenu()");

			if (shell == null) {
				shell = new Shell();
				shell.Init(this);
			}
			Game1.activeClickableMenu = new BotUIMenu(this, shell);
		}
	}
}

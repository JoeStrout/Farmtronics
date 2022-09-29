/*
This class is a stardew valley Object subclass that represents a Bot.
*/

using System;
using System.Collections.Generic;
using System.IO;
using Farmtronics.M1;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Farmtronics.Bot {
	class BotObject : StardewValley.Object {
		// Stardew Valley craftables store 576 parentSheetIndices, so we choose a number after that to avoid conflicts.
		// The wiki recommends not to do that: https://stardewvalleywiki.com/Modding:Items#Define_a_custom_item
		// LookupAnything suggests that everything is fine in game.
		const int ItemID = 0xB07;

		// We need a Farmer to be able to use tools.  So, we're going to
		// create our own invisible Farmer instance and store it here:
		BotFarmer farmer;
		public string BotName { get; internal set; }

		public IList<Item> inventory { get { return farmer.Items; } }
		public Color screenColor { get => shell != null ? shell.console.backColor : Color.Black; }
		
		public Color statusColor = Color.Yellow;
		public Shell shell { get; private set; }
		public bool isUsingTool { get { return farmer.UsingTool? farmer.UsingTool : scytheUseFrame > 0; } }
		public int energy { get { return (int)farmer.Stamina; } }
		public GameLocation currentLocation { get => farmer.currentLocation; set => farmer.currentLocation = value; }
		public int facingDirection { get => farmer.FacingDirection; }
		public int currentToolIndex {
			get => farmer.CurrentToolIndex;
			set {
				if (value >= 0 && value < inventory.Count) {
					farmer.CurrentToolIndex = value;
				}
			}
		}

		// [XmlIgnore]
		// public readonly NetMutex mutex = new NetMutex();

		private int scytheUseFrame = 0;       // > 0 when using the scythe
		private float scytheOldStamina = -1;

		// Assign common values
		private void Initialize() {
			Name = I18n.Bot_Name();
			DisplayName = I18n.Bot_Name();
			BotName = I18n.Bot_Name();
			Type = "Crafting";
			Category = StardewValley.Object.BigCraftableCategory;
			ParentSheetIndex = ItemID;
			bigCraftable.Value = true;
			CanBeSetDown = true;
		}

		private void CreateFarmer(Vector2 tileLocation, GameLocation location) {
			if (location == null) location = Game1.player.currentLocation;
			farmer = new BotFarmer() {
				UniqueMultiplayerID = ModEntry.instance.Helper.Multiplayer.GetNewID(),
				Name = Name,
				displayName = DisplayName,
				Speed = 2,
				MaxStamina = Farmer.startingStamina,
				Stamina = Farmer.startingStamina,
				Position = tileLocation.GetAbsolutePosition(),
				currentLocation = location,
				Items = Farmer.initialTools(),
				MaxItems = 12
			};
			
			// Inventory indices have to exist, since InventoryMenu exclusively uses them and can't assign items otherwise.
			for (int i = farmer.Items.Count; i < GetActualCapacity(); i++) {
				farmer.Items.Add(null);
			}
			
			// NOTE: Make sure to not use farmer.Items.Count to get the actual number of items in the inventory
			ModEntry.instance.Monitor.Log($"TileLocation: {tileLocation} Position: {farmer.Position} Location: {farmer.currentLocation}");
			ModEntry.instance.Monitor.Log($"Items: {farmer.numberOfItemsInInventory()}/{farmer.MaxItems}");
		}

		// This constructor is used for a Bot that is an Item, e.g., in inventory or as a mail attachment.
		public BotObject() {
			//ModEntry.instance.Monitor.Log($"Creating Bot({farmer?.Name}):\n{Environment.StackTrace}");
			Initialize();

			CreateFarmer(tileLocation, null);

			// NOTE: this constructor is used for bots that are not in the world
			// (but are in inventory, mail attachment, etc.).  So we do not add
			// to the instances list.
		}

		public BotObject(Vector2 tileLocation, GameLocation location = null) : base(tileLocation, ItemID) {
			//ModEntry.instance.Monitor.Log($"Creating Bot({tileLocation}, {location?.Name}, {farmer?.Name}):\n{Environment.StackTrace}");
			Initialize();

			CreateFarmer(tileLocation, location);

			BotManager.instances.Add(this);
			// Game1.otherFarmers.Add(farmer.UniqueMultiplayerID, farmer);
		}

		//----------------------------------------------------------------------
		// Storage/retrieval of bot data in a modData dictionary, and
		// inventory transfer from bot to bot (object to item, etc.).
		//----------------------------------------------------------------------

		/// <summary>
		/// Fill the ModDataDictionary with values from this bot,
		/// so they can be saved and restored later.
		/// </summary>
		internal void SetModData(ref ModDataDictionary data, bool includingEnergy = true) {
			ModData botData = new() {
				IsBot = true,
				ModVersion = ModEntry.instance.ModManifest.Version,
				Name = BotName,
				Energy = energy,
				Facing = facingDirection
			};
			botData.Save(ref data);
			
			if (!includingEnergy) botData.RemoveEnergy(ref data);
		}

		/// <summary>
		/// Apply the values in the given ModDataDictionary to this bot,
		/// configuring name, energy, etc.
		/// </summary>
		internal void ApplyModData(ModData d, bool includingEnergy = true) {
			if (!string.IsNullOrEmpty(d.Name)) BotName = d.Name;
			if (includingEnergy) farmer.Stamina = d.Energy;
			farmer.faceDirection(d.Facing);
			//ModEntry.instance.Monitor.Log($"after ApplyModData, name=[{name}]");
		}
		
		internal void ApplyModData(bool includingEnergy = true) {
			if (!ModData.TryGetModData(modData, out ModData botModData)) return;
			BotName = botModData.Name;
			if (includingEnergy) farmer.Stamina = botModData.Energy;
			farmer.FacingDirection = botModData.Facing;
		}

		private bool IsEmptyWithoutInitialTools() {
			ModEntry.instance.Monitor.Log($"isEmptyWithoutInitialTools: items: {farmer.numberOfItemsInInventory()}");
			if (farmer.numberOfItemsInInventory() > Farmer.initialTools().Count) return false;
			var copyItems = new List<Item>(farmer.Items);
			while (copyItems.Contains(null)) { copyItems.Remove(null); }
			Farmer.removeInitialTools(copyItems);
			ModEntry.instance.Monitor.Log($"isEmptyWithoutInitialTools: without initial tools: {copyItems.Count}");
			return copyItems.Count == 0;
		}
		
		public override bool performDropDownAction(Farmer who) {
			ModEntry.instance.Monitor.Log($"Bot.performDropDownAction({who.Name})");
			base.performDropDownAction(who);
			
			return false;   // OK to set down (add to Objects list in the tile)
		}

		/// <summary>
		/// placementAction is called when the player, who is carring a Bot item, indicates
		/// that they want to place it down.  The item is going to be destroyed; we have
		/// to create a new Bot instance that matches its data.
		/// </summary>
		public override bool placementAction(GameLocation location, int x, int y, Farmer who = null) {
			//ModEntry.instance.Monitor.Log($"Bot.placementAction({location}, {x}, {y}, {who.Name})");
			Vector2 placementTile = new Vector2(x, y).GetTilePosition();
			// Create a new bot.
			var bot = new BotObject(placementTile, location);
			Game1.player.currentLocation.setObject(placementTile, bot);
			bot.shakeTimer = 50;

			// Copy other data from this item to bot.
			SetModData(ref bot.modData);
			bot.ApplyModData();

			// But have the placed bot face the same direction as the farmer placing it.
			bot.farmer.FacingDirection = who.facingDirection;
			// Make sure the bot is owned by the farmer placing it.
			bot.owner.Value = who.UniqueMultiplayerID;

			// Add the new bot (which is in the world) to our instances list.
			// Remove the old item, if it happens to be in there (though it probably isn't).
			// Game1.otherFarmers.Remove(farmer.UniqueMultiplayerID);
			BotManager.instances.Remove(this);
			if (!BotManager.instances.Contains(bot)) BotManager.instances.Add(bot);
			//ModEntry.instance.Monitor.Log($"Added {bot.Name} to instances; now have {instances.Count}");

			location.playSound("hammer");
			return true;
		}

		// Apply the currently-selected item as a tool (or weapon) on
		// the square in front of the bot.
		public void UseTool() {
			if (farmer == null || inventory == null || farmer.CurrentTool == null) return;
			Vector2 toolLocation = farmer.GetToolLocation(true);
			ModEntry.instance.Monitor.Log($"UseTool called: {farmer.CurrentTool.Name}[{farmer.CurrentToolIndex}] {toolLocation}");
			float oldStamina = farmer.stamina;
			if (farmer.CurrentTool is not MeleeWeapon)
			{
				farmer.CurrentTool.DoFunction(farmer.currentLocation, toolLocation.GetIntX(), toolLocation.GetIntY(), 1, farmer);
				farmer.checkForExhaustion(oldStamina);
			} else {
				// Special case for using the Scythe
				farmer.CurrentTool.beginUsing(currentLocation, toolLocation.GetIntX(), toolLocation.GetIntY(), farmer);
				Farmer.showToolSwipeEffect(farmer);
				scytheOldStamina = oldStamina;
				// Count how many frames into the swipe effect we are.
				// We'll actually apply the tool effect later, in Update.
				scytheUseFrame = 1;
			}
		}

		// Attempt to harvest the crop in front of the bot.
		public bool Harvest() {
			if (farmer == null) return false;

			GameLocation loc = this.currentLocation;
			Vector2 absoluteLocation = farmer.GetToolLocation(true);
			Vector2 tileLocation = absoluteLocation.GetTilePosition();
			
			ModEntry.instance.Monitor.Log($"Harvest start: {tileLocation}");

			if (loc.isTerrainFeatureAt(absoluteLocation.GetIntX(), absoluteLocation.GetIntY())) {
				// If we can get a terrain feature, then have it do the "use" action,
				// by temporarily setting the bot farmer to be the Game1 player.
				ModEntry.instance.Monitor.Log("Harvesting: TerrainFeature");
				
				var origPlayer = Game1.player;
				Game1.player = farmer;
				bool result = loc.terrainFeatures[tileLocation].performUseAction(tileLocation, loc);
				Game1.player = origPlayer;
				return result;
			} else if (loc.isObjectAtTile(tileLocation.GetIntX(), tileLocation.GetIntY())) {
				ModEntry.instance.Monitor.Log("Harvesting: Tile");
				// If we have an object in that location, harvest from it
				// via a helper method.
				return doBotHarvestFromObject(loc.getObjectAtTile(tileLocation.GetIntX(), tileLocation.GetIntY()));
			} else if (loc.isCropAtTile(tileLocation.GetIntX(), tileLocation.GetIntY())) {
				var dirtObj = loc.terrainFeatures[tileLocation] as HoeDirt;
				ModEntry.instance.Monitor.Log($"Harvesting: Crop [ready = {dirtObj.readyForHarvest()}]");
				
				if (dirtObj.readyForHarvest()) {
					// this starts the animation and sound
					farmer.CurrentTool.beginUsing(farmer.currentLocation, tileLocation.GetIntX(), tileLocation.GetIntY(), farmer);
					// See StardewValley.TerrainFeatures.HoeDirt.cs performToolAction()
					if (farmer.CurrentTool is MeleeWeapon && (farmer.CurrentTool as MeleeWeapon).isScythe() && dirtObj.crop.harvestMethod.Value == 1) {
						if (dirtObj.crop.harvest(tileLocation.GetIntX(), tileLocation.GetIntY(), dirtObj)) {
							dirtObj.destroyCrop(tileLocation, true, farmer.currentLocation);
							return true;
						}	
					}
				}
			}

			return false;
		}

		public bool AddItemToInventory(Item item) {
			// Returns false if the whole item stack can't be added:
			if (Utility.canItemBeAddedToThisInventoryList(item, farmer.Items, farmer.MaxItems)) {
				if (item is Tool) {
					// Without this special case, taking a tool will fill
                    // the bot's inventory with it for some reason
					for (int j = inventory.Count - 1; j >= 0; j--) {
						if (inventory[j] == null) {
							inventory[j] = item;
							return true;
						}
					}
				}
				ModEntry.instance.Monitor.Log("Adding item");
				Utility.addItemToThisInventoryList(item, farmer.Items, farmer.MaxItems);
				return true;
			} else {
				ModEntry.instance.Monitor.Log("Can't add item");
				return false;
			}
		}

		public bool doBotHarvestFromObject(StardewValley.Object what) {
			// See "checkForAction" in StardewValley.Object.
			// This is effectively a snippet from it that deals with harvesting from machines.
			// We probably don't want to call checkForAction, as that could cause weird behaviour like opening menus.
			// Unfortunately, if other mods are patching checkForAction to alter their harvest results this won't work well with those.
			// However, this doesn't seem to be common practice. This fix works with PFM (Producer Framework)
			Farmer who = farmer;

			StardewValley.Object objectThatWasHeld = what.heldObject.Value;
			if (what != null && what.readyForHarvest.Value) {
				if (who.isMoving()) {
					Game1.haltAfterCheck = false;
				}
				bool check_for_reload = false;
				if (what.name.Equals("Bee House")) {
					int honey_type = -1;
					string honeyName = "Wild";
					int honeyPriceAddition = 0;
					Crop c = Utility.findCloseFlower(who.currentLocation, what.TileLocation, 5, (Crop crop) => (!crop.forageCrop.Value) ? true : false);
					if (c != null) {
						honeyName = Game1.objectInformation[c.indexOfHarvest.Value].Split('/')[0];
						honey_type = c.indexOfHarvest.Value;
						honeyPriceAddition = Convert.ToInt32(Game1.objectInformation[c.indexOfHarvest.Value].Split('/')[1]) * 2;
					}
					if (what.heldObject.Value != null) {
						what.heldObject.Value.name = honeyName + " Honey";
						//what.heldObject.Value.displayName = what.loadDisplayName();
						what.heldObject.Value.Price = Convert.ToInt32(Game1.objectInformation[340].Split('/')[1]) + honeyPriceAddition;
						what.heldObject.Value.preservedParentSheetIndex.Value = honey_type;
						if (Game1.GetSeasonForLocation(Game1.currentLocation).Equals("winter")) {
							what.heldObject.Value = null;
							what.readyForHarvest.Value = false;
							what.showNextIndex.Value = false;
							return false;
						}

						StardewValley.Object item = what.heldObject.Value;
						what.heldObject.Value = null;
						if (!AddItemToInventory(item)) {
							what.heldObject.Value = item;
							Game1.showRedMessage(Game1.content.LoadString(Path.Combine("Strings", "StringsFromCSFiles:Crop.cs.588")));
							return false;
						}

						//Game1.playSound("coin");
						check_for_reload = true;
					}
				} else {
					// This is the real meat and potatoes.
					// We remove the heldObject from whatever harvestable we are interacting with.
					// If we do not successfully add the item to the bot, we then reassign our held item back to heldObject
					what.heldObject.Value = null;
					if (!AddItemToInventory(objectThatWasHeld)) {
						what.heldObject.Value = objectThatWasHeld;
						Game1.showRedMessage("Cannot add item to Bot");
						return false;
					}
					//Game1.playSound("coin");
					check_for_reload = true;
					switch (what.name) {
					case "Keg":
						Game1.stats.BeveragesMade++;
						break;
					case "Preserves Jar":
						Game1.stats.PreservesMade++;
						break;
					case "Cheese Press":
						if (objectThatWasHeld.ParentSheetIndex == 426) {
							Game1.stats.GoatCheeseMade++;
						} else {
							Game1.stats.CheeseMade++;
						}
						break;
					}
				}
				if (what.name.Equals("Crystalarium")) {
					int mins = ModEntry.instance.Helper.Reflection.GetMethod(objectThatWasHeld, "getMinutesForCrystalarium").Invoke<int>(objectThatWasHeld.ParentSheetIndex);
					what.MinutesUntilReady = mins;
					what.heldObject.Value = (StardewValley.Object)objectThatWasHeld.getOne();
				} else if (what.name.Contains("Tapper")) {
					if (who.currentLocation.terrainFeatures.ContainsKey(what.TileLocation) && who.currentLocation.terrainFeatures[what.TileLocation] is Tree) {
						(who.currentLocation.terrainFeatures[what.TileLocation] as Tree).UpdateTapperProduct(what, objectThatWasHeld);
					}
				} else {
					what.heldObject.Value = null;
				}
				what.readyForHarvest.Value = false;
				what.showNextIndex.Value = false;
				if (what.name.Equals("Bee House") && !Game1.GetSeasonForLocation(who.currentLocation).Equals("winter")) {
					what.heldObject.Value = new StardewValley.Object(Vector2.Zero, 340, null, canBeSetDown: false, canBeGrabbed: true, isHoedirt: false, isSpawnedObject: false);
					what.MinutesUntilReady = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, 4);
				} else if (what.name.Equals("Worm Bin")) {
					what.heldObject.Value = new StardewValley.Object(685, Game1.random.Next(2, 6));
					what.MinutesUntilReady = Utility.CalculateMinutesUntilMorning(Game1.timeOfDay, 1);
				}
				if (check_for_reload) {
					what.AttemptAutoLoad(who);
				}
				return true;

			} else {
				return false;
			}
		}

		public bool TakeItem(int slotNumber, int amount = -1) {
			if (farmer == null) return false;
			Vector2 tileLocation = farmer.GetToolLocation(true);

			if (farmer.currentLocation.isObjectAt(tileLocation.GetIntX(), tileLocation.GetIntY())) {
				StardewValley.Object obj = farmer.currentLocation.getObjectAt(tileLocation.GetIntX(), tileLocation.GetIntY());
				ModEntry.instance.Monitor.Log($"Taking item from slot {slotNumber} of {obj.Name}");
				IList<Item> sourceItems = null;
				if (obj is Chest chest) sourceItems = chest.items;
				else if (obj is BotObject bot) sourceItems = bot.inventory;
				else ModEntry.instance.Monitor.Log($"Couldn't take any items from this object.");
				if (sourceItems != null && slotNumber < sourceItems.Count && sourceItems[slotNumber] != null && AddItemToInventory(sourceItems[slotNumber])) {
					ModEntry.instance.Monitor.Log($"Taking {sourceItems[slotNumber].DisplayName} from container");
					Utility.removeItemFromInventory(slotNumber, sourceItems);
					return true;
				}
			} else {
				ModEntry.instance.Monitor.Log("Not facing anything");
			}

			return false;
		}
		
		// Place the currently selected item (e.g., seed) in/on the ground
		// or machine/container ahead of the robot.  Return the number
		// successfully placed.
		public int PlaceItem() {
			var item = farmer.CurrentItem;
			if (item == null) {
				ModEntry.instance.Monitor.Log($"No item equipped in slot {currentToolIndex}");
				return 0;
			}
			ModEntry.instance.Monitor.Log($"Placing {item.DisplayName} from slot {currentToolIndex}");
			Location tileLocation = farmer.GetToolLocation(true).ToLocation();

			// if we can place the item via standard Utility/item methods, do so
			var itemAsObj = item as StardewValley.Object;
			if (itemAsObj != null && Utility.playerCanPlaceItemHere(farmer.currentLocation, item, tileLocation.X, tileLocation.Y, farmer)) {
				//Place it
				bool result = itemAsObj.placementAction(currentLocation, tileLocation.X, tileLocation.Y, farmer);
				ModEntry.instance.Monitor.Log($"placementAction result: {result}");
				// reduce inventory by one, and clear the inventory if the stack is empty
				item.Stack--;
				if (item.Stack <= 0) inventory[currentToolIndex] = null;
				return 1;
			}

			// check the Object layer for machines etc
			if (farmer.currentLocation.isObjectAt(tileLocation.X, tileLocation.Y)) {
				StardewValley.Object obj = farmer.currentLocation.getObjectAt(tileLocation.X, tileLocation.Y);
				// Perform the object drop in
				// This method is patched by mods like PFM to get custom machines working,
                // so we get compatibility with that by default.
				bool result = obj.performObjectDropInAction(item, false, farmer);
				ModEntry.instance.Monitor.Log($"performObjectDropInAction({item.DisplayName}) result: {result}");
				if (result) {
					// reduce inventory by one, and clear the inventory if the stack is empty
					item.Stack--;
					if (item.Stack <= 0) inventory[currentToolIndex] = null;
					return 1;
				}
				// If that doesn't work, then check various special cases
				// (including placing in bots and chests).
				if (obj is Sign sign) {
					var oneItem = inventory[currentToolIndex].getOne();
					sign.displayItem.Value = oneItem;
					sign.displayType.Value = 1;
					if (sign.displayItem.Value is Hat) {
						sign.displayType.Value = 2;
					} else if (sign.displayItem.Value is Ring) {
						sign.displayType.Value = 4;
					} else if (sign.displayItem.Value is Furniture) {
						sign.displayType.Value = 5;
					} else if (sign.displayItem.Value is StardewValley.Object) {
						sign.displayType.Value = ((!(oneItem as StardewValley.Object).bigCraftable.Value) ? 1 : 3);
					}
					return 1;
				}
				if (obj is Chest chest) {
					ModEntry.instance.Monitor.Log($"Adding {item.DisplayName} to chest.");
					int beforeCount = item.Stack;
					inventory[currentToolIndex] = chest.addItem(item);
					int afterCount = (inventory[currentToolIndex] == null ? 0 : inventory[currentToolIndex].Stack);
					return beforeCount - afterCount;
				} else if (obj is BotObject bot) {
					ModEntry.instance.Monitor.Log($"Adding {item.DisplayName} to bot.");
					int beforeCount = item.Stack;
					if (!bot.AddItemToInventory(item)) return 0;
					inventory[currentToolIndex] = null;
					return beforeCount;
                } else {
					ModEntry.instance.Monitor.Log($"Object ahead of bot is neither Chest nor Bot");
					return 0;
				}
			}
			else ModEntry.instance.Monitor.Log($"No object found at {tileLocation}");
			return 0;
		}

		public void MoveForward() {
			// make sure the terrain in that direction isn't blocked
			Vector2 newTile = farmer.nextPositionTile().ToVector2();
			Vector2 moveVector = newTile - farmer.getTileLocation();
			ModEntry.instance.Monitor.Log($"Moving from {TileLocation} to {newTile} with speed: {farmer.getMovementSpeed()}");
			var location = farmer.currentLocation;
			{
				// How to detect walkability in pretty much the same way as other characters:
				var newBounds = farmer.GetBoundingBox();
				newBounds.X += moveVector.GetIntX() * Game1.tileSize;
				newBounds.Y += moveVector.GetIntY() * Game1.tileSize;
				bool coll = location.isCollidingPosition(newBounds, Game1.viewport, isFarmer: false, 0, glider: false, farmer);
				if (coll) {
					ModEntry.instance.Monitor.Log("Colliding position: " + newBounds);
					return;
				}
			}

			// Do collision actions (shake the grass, etc.)
			if (location.terrainFeatures.ContainsKey(newTile)) {
				var feature = location.terrainFeatures[newTile];
				var posRect = feature.getBoundingBox(newTile);
				feature.doCollisionAction(posRect, 4, newTile, null, location);
			}
			
			// Remove this object from the Objects list at its old position
			location.removeObject(TileLocation, false);
			// Update our tile pos, and add this object to the Objects list at the new position
			TileLocation = newTile;
			location.setObject(newTile, this);
			// Update the invisible farmer
			farmer.setTileLocation(newTile);
		}

		public void Rotate(int stepsClockwise) {
			farmer.faceDirection((farmer.FacingDirection + 4 + stepsClockwise) % 4);
			//ModEntry.instance.Monitor.Log($"{Name} Rotate({stepsClockwise}): now facing {farmer.FacingDirection}");
		}

		void ApplyScytheToTile() {
			// Actually apply the tool to the tile in front of the bot.
			// This is a big pain in the neck that is duplicated in many of the Tool subclasses.
			// Here's how we do it:
			// First, get the tool to apply, and the tile location to apply it.
			if (farmer == null || inventory == null) return;
			Tool tool = farmer.CurrentTool;
			Vector2 tile = farmer.GetToolLocation(true).GetTilePosition();
			var location = farmer.currentLocation;

			// Big pain in the neck time.

			// Apply it to the location itself.
			ModEntry.instance.Monitor.Log($"{name} Performing {tool.Name} action at {tile}");
			location.performToolAction(tool, tile.GetIntX(), tile.GetIntY());

			// Then, apply it to any terrain feature (grass, weeds, etc.) at this location.
			if (location.terrainFeatures.ContainsKey(tile) && location.terrainFeatures[tile].performToolAction(tool, 1, tile, location)) {
				ModEntry.instance.Monitor.Log($"Performed tool action on the terrain feature {location.terrainFeatures[tile]}; removing it");
				location.terrainFeatures.Remove(tile);
			}
			if (location.largeTerrainFeatures is not null) {
				var absoluteTile = tile.GetAbsolutePosition();
				var tileRect = new Rectangle(absoluteTile.GetIntX(), absoluteTile.GetIntY(), Game1.tileSize, Game1.tileSize);
				for (int i = location.largeTerrainFeatures.Count - 1; i >= 0; i--) {
					if (location.largeTerrainFeatures[i].getBoundingBox().Intersects(tileRect) && location.largeTerrainFeatures[i].performToolAction(tool, 1, tile, location)) {
						//ModEntry.instance.Monitor.Log($"Performed tool action on the LARGE terrain feature {location.terrainFeatures[tile]}; removing it");
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
						//ModEntry.instance.Monitor.Log($"Performed tool action on the object {obj}; adding debris");
						location.debris.Add(new Debris(obj.bigCraftable.Value ? (-obj.ParentSheetIndex) : obj.ParentSheetIndex,
							farmer.GetToolLocation(true), new Vector2(center.X, center.Y)));
					}
					//ModEntry.instance.Monitor.Log($"Performing {obj} remove action, then removing it from {tile}");
					obj.performRemoveAction(tile, location);
					location.Objects.Remove(tile);
				}
			}
			
			farmer.checkForExhaustion(scytheOldStamina);
			scytheOldStamina = -1;
		}

		public void Update(GameTime gameTime) {
			// bool debug = false;//ModEntry.instance.Helper.Input.IsDown(SButton.RightShift);
			// if (debug) ModEntry.instance.Monitor.Log($"{Name} updating with farmer in {farmer.currentLocation?.Name}, here is {Game1.currentLocation.Name}, shell is {shell}");


			// Weird things happen if we try to update bots in locations other than
			// the current location.  We should try harder to get that to work sometime,
			// but for now, let's just detect that case and bail out.
			if (farmer.currentLocation != Game1.currentLocation) return;

			if (shell != null) {
				shell.console.update(gameTime);
				if (farmer.Name != Name) farmer.Name = Name;
			}
			
			if (scytheUseFrame > 0) {
				scytheUseFrame++;
				if (scytheUseFrame == 6) ApplyScytheToTile();
				else if (scytheUseFrame == 12) scytheUseFrame = 0;  // all done!
			}

			farmer.Update(gameTime, farmer.currentLocation);
		}

		public override bool checkForAction(Farmer who, bool justCheckingForActivity = false) {
			//ModEntry.instance.Monitor.Log($"Bot.checkForAction({who.Name}, {justCheckingForActivity}), tool {who.CurrentTool}");
			if (justCheckingForActivity) return true;
			// all this overriding... just to change the open sound.
			if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true)) {
				//ModEntry.instance.Monitor.Log($"Bailing because didPlayerJustRightClick is false");
				return false;
			}

			// ToDo: use mutex to ensure only one player can open a bot at a time.
			// (Tried, but couldn't get to work:
			//ModEntry.instance.Monitor.Log($"Requesting mutex lock: {mutex}, IsLocked={mutex.IsLocked()}, IsLockHeld={mutex.IsLockHeld()}");
			//mutex.RequestLock(delegate {
			//	Game1.playSound("bigSelect");
			//	Game1.player.Halt();
			//	Game1.player.freezePause = 1000;
			//	ShowMenu();
			//}, delegate {
			//	ModEntry.instance.Monitor.Log("Failed to get mutex lock :(");
			//});

			// For now, just dewit:
			Game1.playSound("bigSelect");
			Game1.player.Halt();
			Game1.player.freezePause = 1000;
			ShowMenu();

			return true;
		}

		public override bool performToolAction(Tool t, GameLocation location) {
			ModEntry.instance.Monitor.Log($"{name} Bot.performToolAction({t}, {location})");
			
			// NOTE: If a player holds left click it will eventually trigger a toolAction with a pickaxe
			// 		 This could be checked like this: t != t.getLastFarmerToUse().CurrentTool
			if (t is Pickaxe or Axe or Hoe) {
				if (!IsEmptyWithoutInitialTools()) {
					// Maybe create a small shake animation? (see Chest.cs performToolAction() -> shakeTimer)
					location.playSound("hammer");
					return false;
				}
				
				//ModEntry.instance.Monitor.Log("{name} Bot.performToolAction: creating custom debris");
				var who = t.getLastFarmerToUse();
				this.performRemoveAction(farmer.getTileLocation(), location);
				Debris deb = new Debris(this.getOne(), who.GetToolLocation(true), new Vector2(who.GetBoundingBox().Center.X, who.GetBoundingBox().Center.Y));
				SetModData(ref deb.item.modData);
				location.debris.Add(deb);
				//ModEntry.instance.Monitor.Log($"{name} Created debris with item {deb.item} and energy {energy}");
				// Remove, stop, and destroy this bot
				location.removeObject(farmer.getTileLocation(), true);
				if (shell != null) shell.interpreter.Stop();
				// Game1.otherFarmers.Remove(farmer.UniqueMultiplayerID);
				BotManager.instances.Remove(this);
				return false;
			}

			// previous code, that called the base... this sometimes resulted
			// in picking up a chest, while leaving a ghost bot behind:
			//bool result = base.performToolAction(t, location);
			//ModEntry.instance.Monitor.Log($"{name} Bot.performToolAction: My TileLocation is now {this.TileLocation}");
			//return result;
			// I'm not aware of any use case for doing the default tool action on a bot.
			// So now we're going to avoid that whole issue by always doing:
			return false;
		}

		public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1) {
			//ModEntry.instance.print($"draw 1 at {x},{y}, {alpha}");
			var absoluteLocation = new Location(x, y).GetAbsoluteLocation();

			// draw shadow
			spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport,
				new Vector2(absoluteLocation.X + 32, absoluteLocation.Y + 51 + 4)),
				Game1.shadowTexture.Bounds, Color.White * alpha, 0f,
				new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f,
				SpriteEffects.None, (float)getBoundingBox(new Vector2(x, y)).Bottom / 15000f);

			// draw sprite
			Vector2 position3 = Game1.GlobalToLocal(Game1.viewport, new Vector2(
				absoluteLocation.X + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
				absoluteLocation.Y + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)));
			// Note: FacingDirection 0-3 is Up, Right, Down, Left
			int facing = 2;
			if (farmer != null) facing = farmer.FacingDirection;
			Rectangle srcRect = new Rectangle(16 * facing, 0, 16, 24);
			Vector2 origin2 = new Vector2(8f, 8f);
			float scale = (this.scale.Y > 1f) ? getScale().Y : 4f;
			float z = (float)(getBoundingBox(new Vector2(x, y)).Bottom) / 10000f;
			// base sprite
			spriteBatch.Draw(Assets.BotSprites, position3, srcRect, Color.White * alpha, 0f,
				origin2, scale, SpriteEffects.None, z);
			// screen color (if not black or clear)
			if (screenColor.A > 0 && (screenColor.R > 0 || screenColor.G > 0 || screenColor.B > 0)) {
				srcRect.Y = 24;
				spriteBatch.Draw(Assets.BotSprites, position3, srcRect, screenColor * alpha, 0f,
					origin2, scale, SpriteEffects.None, z + 0.0001f);
			}
			// screen shine overlay
			srcRect.Y = 48;
			spriteBatch.Draw(Assets.BotSprites, position3, srcRect, Color.White * alpha, 0f,
				origin2, scale, SpriteEffects.None, z + 0.0002f);
			// status light color (if not black or clear)
			if (statusColor.A > 0 && (statusColor.R > 0 || statusColor.G > 0 || statusColor.B > 0)) {
				srcRect.Y = 72;
				spriteBatch.Draw(Assets.BotSprites, position3, srcRect, statusColor * alpha, 0f,
					origin2, scale, SpriteEffects.None, z + 0.0002f);
			}

			// draw hat, if one is found in the last slot
			if (farmer != null && farmer.MaxItems - 1 < farmer.Items.Count && farmer.Items[farmer.MaxItems -1] is Hat) {
				drawHat(spriteBatch, farmer.Items[farmer.MaxItems - 1] as Hat, position3, z + 0.0002f, alpha);
			}
		}

		/// <summary>
		/// Draw the bot as it should appear above the player's head when held.
		/// </summary>
		public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f) {
			//ModEntry.instance.Monitor.Log($"Bot.drawWhenHeld");
			Rectangle srcRect = new Rectangle(16 * f.facingDirection, 0, 16, 24);
			spriteBatch.Draw(Assets.BotSprites, objectPosition, srcRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, Math.Max(0f, (float)(f.getStandingY() + 3) / 10000f));
		}

		public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
			//ModEntry.instance.Monitor.Log($"Bot.drawInMenu with scaleSize {scaleSize}");
			if ((bool)this.IsRecipe) {
				transparency = 0.5f;
				scaleSize *= 0.75f;
			}
			bool shouldDrawStackNumber = ((drawStackNumber == StackDrawType.Draw && this.maximumStackSize() > 1 && this.Stack > 1)
				|| drawStackNumber == StackDrawType.Draw_OneInclusive) && (double)scaleSize > 0.3 && this.Stack != int.MaxValue;

			Rectangle srcRect = new Rectangle(0, 112, 16, 16);
			spriteBatch.Draw(Assets.BotSprites, location + new Vector2((int)(32f * scaleSize), (int)(32f * scaleSize)), srcRect, color * transparency, 0f,
				new Vector2(8f, 8f) * scaleSize, 4f * scaleSize, SpriteEffects.None, layerDepth);

			if (shouldDrawStackNumber) {
				var loc = location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(this.Stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 2f);
				Utility.drawTinyDigits(this.Stack, spriteBatch, loc, 3f * scaleSize, 1f, color);
			}
		}

		public override void drawAsProp(SpriteBatch b) {
			//ModEntry.instance.Monitor.Log($"Bot.drawAsProp");
			if (this.isTemporarilyInvisible) return;
			Vector2 tileLocation = farmer.getTileLocation();

			Vector2 scaleFactor = Vector2.One; // this.PulseIfWorking ? this.getScale() : Vector2.One;
			scaleFactor *= 4f;
			Vector2 position = Game1.GlobalToLocal(Game1.viewport, tileLocation.GetAbsolutePosition() - new Vector2(0, - Game1.tileSize));
			Rectangle srcRect = new Rectangle(16 * 2, 0, 16, 24);
			b.Draw(destinationRectangle: new Rectangle((int)(position.X - scaleFactor.X / 2f), (int)(position.Y - scaleFactor.Y / 2f),
				(int)(Game1.tileSize + scaleFactor.X), (int)(Game1.tileSize * 2 + scaleFactor.Y / 2f)),
				texture: Assets.BotSprites,
				sourceRectangle: srcRect,
				color: Color.White,
				rotation: 0f,
				origin: Vector2.Zero,
				effects: SpriteEffects.None,
				layerDepth: Math.Max(0f, ((tileLocation.X + 1) * Game1.tileSize - 1) / 10000f));
		}

		public void drawHat(SpriteBatch spriteBatch, Hat hat, Vector2 position, float layerDepth, float alpha = 1) {
			layerDepth += 1E-07f;
			var hatOffset = new Vector2();
			hatOffset.X = -42f;
			hatOffset.Y = -38f;
			hat.draw(spriteBatch, position + hatOffset, 1.5f, alpha, layerDepth, facingDirection);
		}

		/// <summary>
		/// This method is called to get an "Item" (something that can be carried) from this Bot.
		/// Since Bot is an Object and Objects are Items, we can just return another Bot, but
		/// for some reason we can't just return *this* bot.  Probably because this one is
		/// about to be destroyed.
		/// </summary>
		/// <returns></returns>
		public override Item getOne() {
			// Create a new Bot from this one, copying the modData and owner
			var ret = new BotObject();
			SetModData(ref ret.modData);
			ret._GetOneFrom(this);
			return ret;
		}


		public int GetActualCapacity() {
			return farmer.MaxItems;
		}

		/// <summary>
		/// Initializes this bot instance.
		/// Does nothing if the bot instance has already been initialized.
		/// Effectively starts up the bot.
		/// </summary>
		public void InitShell() {
			if (shell == null) {
				shell = new Shell();
				shell.Init(this);
			}
		}

		public void ShowMenu() {
			ModEntry.instance.Monitor.Log($"{Name} ShowMenu()");

			// Make sure the bot is booted up when showing the menu.
			InitShell();
			Game1.activeClickableMenu = new UIMenu(this);
		}

		#region ShopEntry

		public override bool actionWhenPurchased() {
			return false;
		}

		public override bool CanBuyItem(Farmer farmer) {
			// Pointless right now but could be useful for multiplayer:
			// return farmer.mailRecieved.Contains("FarmtronicsFirstBotMail"); //Should work, haven't tested.
			return Game1.player.mailReceived.Contains("FarmtronicsFirstBotMail");
		}

		public override bool canStackWith(ISalable other) {
			// Bots don't allow stacking.  Too hard to deal with individual bot
			// names, energy, inventory, etc.
			return false;
		}
		
		public override string getDescription() {
			return I18n.Bot_Description();
		}

		public override int maximumStackSize() {
			return 1;
		}

		public override int salePrice() {
			return 50;
		}
		
		#endregion
	}
}

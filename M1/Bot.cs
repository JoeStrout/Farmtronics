/*
This class is a stardew valley Object subclass that represents a Bot.

*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace M1 {

	public class Bot : StardewValley.Objects.Chest {
		public IList<Item> inventory {  get {  return farmer.Items; } }
		public Color screenColor = Color.Transparent;
		public Color statusColor = Color.Yellow;
		public Shell shell { get; private set; }
		public bool isUsingTool {  get {  return toolUseFrame > 0; } }

		public GameLocation currentLocation {
			get { return farmer.currentLocation; }
		}
		public int facingDirection {  get {  return farmer.FacingDirection; } }
		public int currentToolIndex {
			get { return farmer.CurrentToolIndex; }
			set {  farmer.CurrentToolIndex = value; }
		}

		const int vanillaObjectTypeId = 130;	// "Chest"
		//const int vanillaObjectTypeId = 125;	// "Golden Relic"

		// We need a Farmer to be able to use tools.  So, we're going to
		// create our own invisible Farmer instance and store it here:
		Farmer farmer;

		Vector2 position;	// our current position, in pixels
		Vector2 targetPos;	// position we're moving to, in pixels

		static List<Bot> instances = new List<Bot>();

		static int uniqueFarmerID = 1;
		const float speed = 64;		// pixels/sec

		int toolUseFrame = 0;		// > 0 when using a tool

		static Texture2D botSprites;

    public enum Seeds
		{
			// spring seeds
			Parsnip = 472, BeanStarter = 473, Cauliflower = 474, Potato = 475, Tulip = 427, Kale = 477, Jazz = 429,
			Garlic = 476, RiceShoot = 273,
			// summer
			Melon = 479, Tomato = 480, Blueberry = 481, Pepper = 482, Wheat = 483, Radish = 484,
			Poppy = 453, Spangle = 455, Hops = 302, Corn = 487, Sunflower = 431, RedCabbage = 485,
			// fall
			Pumpkin = 490, Eggplant = 488, BokChoy = 491, Yam = 492, Cranberry = 493, Fairy = 425,
            Amaranth = 299, Grape = 301, Artichoke = 489
		}

		public Bot(Vector2 tileLocation) :base(true, tileLocation) {
			if (botSprites == null) {
				botSprites = ModEntry.helper.Content.Load<Texture2D>("assets/BotSprites.png");
			}

      var initialTools = new List<Item> {
        new Hoe(),
        new Axe(),
      	new Pickaxe(),
        new WateringCan(),
				new MeleeWeapon(47),  // (scythe)
				new StardewValley.Object(Vector2.Zero, (int)Seeds.Parsnip, int.MaxValue),
				//new StardewValley.Object(Vector2.Zero, (int)Seeds.BeanStarter, int.MaxValue),		// trellis
				new StardewValley.Object(Vector2.Zero, (int)Seeds.Cauliflower, int.MaxValue),
				new StardewValley.Object(Vector2.Zero, (int)Seeds.Potato, int.MaxValue),
				new StardewValley.Object(Vector2.Zero, (int)Seeds.Tulip, int.MaxValue),
				//new StardewValley.Object(Vector2.Zero, (int)Seeds.Kale, int.MaxValue),					// harvested with scythe
				new StardewValley.Object(Vector2.Zero, (int)Seeds.Jazz, int.MaxValue),
				new StardewValley.Object(Vector2.Zero, (int)Seeds.Garlic, int.MaxValue),
				//new StardewValley.Object(Vector2.Zero, (int)Seeds.RiceShoot, int.MaxValue)			// harvested with scythe
      };

      foreach (Item i in initialTools) addItem(i);

			Name = "Bot " + uniqueFarmerID;
			farmer = new Farmer(new FarmerSprite("Characters\\Farmer\\farmer_base"),
				tileLocation * 64, 2,
				Name, initialTools, isMale: true);
			farmer.currentLocation = Game1.player.currentLocation;
			uniqueFarmerID++;
			//this.Type = "Crafting";	// (necessary for performDropDownAction to be called)
			ModEntry.instance.print($"Type: {this.Type}  bigCraftable:{bigCraftable}");

			instances.Add(this);
		}

		public static void UpdateAll(GameTime gameTime) {
			foreach (Bot bot in instances) bot.Update(gameTime);
		}

		public override bool performDropDownAction(Farmer who) {
			base.performDropDownAction(who);

			// Keep our farmer positioned wherever this object is
			farmer.currentLocation = Game1.player.currentLocation;
			farmer.setTileLocation(TileLocation);
			return false;	// OK to set down (add to Objects list in the tile)
		}

		public void NotePosition() {
			position = targetPos = TileLocation * 64f;
			farmer.setTileLocation(TileLocation);
		}

		public void UseTool() {
			Tool tool = inventory[currentToolIndex] as Tool;
			if (tool == null) return;

			int useX = (int)position.X + 32 * DxForDirection(farmer.FacingDirection);
			int useY = (int)position.Y + 32 * DyForDirection(farmer.FacingDirection);

			tool.beginUsing(currentLocation, useX, useY, farmer);

			farmer.setTileLocation(TileLocation);
            // Farmer.showToolSwipeEffect(farmer);

            // Count how many frames into the swipe effect we are.
            // We'll actually apply the tool effect later, in Update.
            toolUseFrame = 1;
		}

        public void EndUseTool()
		{
			Tool tool = inventory[currentToolIndex] as Tool;
			if (tool == null) return;

			tool.endUsing(currentLocation, farmer);

			toolUseFrame = 0;
		}

		public bool PlantSeeds() {

            if (inventory[currentToolIndex] is not StardewValley.Object)
            {
				ModEntry.instance.print($"currentToolIndex {currentToolIndex} is not an Object");
                return false;
            }

			StardewValley.Object seedPacket = inventory[currentToolIndex] as StardewValley.Object;
			farmer.makeThisTheActiveObject(seedPacket);

			seedPacket.placementAction(currentLocation, (int)position.X, (int)position.Y, farmer);
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
			var location = Game1.currentLocation;	// ToDo: find correct location!
			if (!location.isTileLocationTotallyClearAndPlaceableIgnoreFloors(newTile)) {
				ModEntry.instance.print($"No can do (path blocked)");
				return;
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

			if (!(inventory[currentToolIndex] is Tool))
			{
				return;
			}
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
			Debug.Log($"Performing {tool} action at {tileX},{tileY}");
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
			if (shell != null) {
				shell.console.update(gameTime);
			}
			if (toolUseFrame > 0) {
				toolUseFrame++;
				if (toolUseFrame == 6) ApplyToolToTile();
				else if (toolUseFrame == 12) EndUseTool();	// all done!

			}

			if (position != targetPos) {
				// ToDo: make a utility module with MoveTowards in it
				position.X += MathF.Sign(targetPos.X - position.X);
				position.Y += MathF.Sign(targetPos.Y - position.Y);
				Vector2 newTile = new Vector2((int)position.X / 64, (int)position.Y / 64);
				if (newTile != TileLocation) {
					// Remove this object from the Objects list at its old position
					var location = Game1.currentLocation;	// ToDo: find correct location!
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
			if (justCheckingForActivity) return true;
			// all this overriding... just to change the open sound.
			if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true)) return false;
			GetMutex().RequestLock(delegate {
				frameCounter.Value = 5;
				Game1.playSound("bigSelect");
				Game1.player.Halt();
				Game1.player.freezePause = 1000;
				});
			return true;
		}

		// Note: to change the close sound, we would need to override updateWhenCurrentLocation.
		// But that's fairly complex code and relies on currentLidFrame, which is private to Chest.
		// So we can't easily do that.  To make it work at all we'd need to either do away with
		// or replace most of that functionality with our own, or use reflection to get/set the
		// private variable, or do some other kind of hackery (patching localSound etc.).
		// Best is to probably throw out (override) all the standard chest animation stuff.
		// But that's a big job for another day.


		public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1) {
			// draw shadow
			spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(position.X + 32, position.Y + 51 + 4)),
				Game1.shadowTexture.Bounds, Color.White * alpha, 0f,
				new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f,
				SpriteEffects.None, (float)getBoundingBox(new Vector2(x, y)).Bottom / 15000f);

			// draw sprite
			Vector2 position3 = Game1.GlobalToLocal(Game1.viewport, new Vector2(
				position.X + 32 + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
				position.Y + ((shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)));
			// Note: FacingDirection 0-3 is Up, Right, Down, Left
			Rectangle srcRect = new Rectangle(16 * farmer.FacingDirection, 0, 16, 24);
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
			//ModEntry.instance.print($"draw 2 at {xNonTile},{yNonTile}");
			base.draw(spriteBatch, xNonTile, yNonTile, layerDepth, alpha);
		}

		public override int GetActualCapacity() {
			return 12;
		}

		public override void ShowMenu() {
			ModEntry.instance.print($"{Name} ShowMenu()");

			if (shell == null) {
				shell = new Shell();
				shell.Init(this);
			}
			Game1.activeClickableMenu = new BotUIMenu(this, shell);

			/*
			// So this is what a normal chest does:
			Game1.activeClickableMenu = new StardewValley.Menus.ItemGrabMenu(
				GetItemsForPlayer(Game1.player.UniqueMultiplayerID),
				reverseGrab: false,
				showReceivingMenu: false,
				StardewValley.Menus.InventoryMenu.highlightAllItems,
				grabItemFromInventory,
				Name,
				grabItemFromChest,
				snapToBottom: true,
				canBeExitedWithKey: false,
				playRightClickSound: true,
				allowRightClick: true,
				showOrganizeButton: true,
				1,		// int source
				this,	// sourceItem
				-1,		// whichSpecialButton
				this);	// context
			*/

			// ...but we're going to need to replace ItemGrabMenu with our own custom
			// menu.  Fortunately it should be able to leverage the ItemsToGrabMenu
			// (which is the container inventory space), as ItemGrabMenu does.
		}
	}
}

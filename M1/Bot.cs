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

namespace M1 {
	public class Bot : StardewValley.Objects.Chest {
		public IList<Item> inventory {  get {  return farmer.Items; } }
		public Color screenColor = Color.Transparent;
		public Color statusColor = Color.Yellow;

		public GameLocation currentLocation {
			get { return farmer.currentLocation; }
		}
		public int facingDirection {  get {  return farmer.FacingDirection; } }

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

		public Shell shell { get; private set; }

		static Texture2D botSprites;

		public Bot(Vector2 tileLocation) :base(true, tileLocation) {
			if (botSprites == null) {
				botSprites = ModEntry.helper.Content.Load<Texture2D>("assets/BotSprites.png");
			}

			var initialTools = new List<Item>();
			initialTools.Add(new StardewValley.Tools.Hoe());
			initialTools.Add(new StardewValley.Tools.Axe());
			initialTools.Add(new StardewValley.Tools.Pickaxe());

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
			farmer.setTileLocation(TileLocation);
			return false;	// OK to set down (add to Objects list in the tile)
		}

		public void NotePosition() {
			position = targetPos = TileLocation * 64f;
			farmer.setTileLocation(TileLocation);
		}

		public void UseTool() {
			farmer.setTileLocation(TileLocation);
			farmer.CurrentToolIndex = 0;			
			Game1.toolAnimationDone(farmer);
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

		public void MoveForward() {
			switch (farmer.FacingDirection) {
			case 0:		Move(0, -1);		break;
			case 1:		Move(1, 0);			break;
			case 2:		Move(0, 1);			break;
			case 3:		Move(-1, 0);		break;
			}
			Debug.Log($"{Name} MoveForward() when position to {position}, tileLocation to {TileLocation}; facing {farmer.FacingDirection}");
		}

		public bool IsMoving() {
			return (position != targetPos);
		}

		public void Rotate(int stepsClockwise) {
			farmer.faceDirection((farmer.FacingDirection + 4 + stepsClockwise) % 4);
			Debug.Log($"{Name} Rotate({stepsClockwise}): now facing {farmer.FacingDirection}");
		}

		public void Update(GameTime gameTime) {
			if (shell != null) {
				shell.console.update(gameTime);
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

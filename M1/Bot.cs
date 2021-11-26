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

		const int vanillaObjectTypeId = 130;	// "Chest"
		//const int vanillaObjectTypeId = 125;	// "Golden Relic"

		// We need a Farmer to be able to use tools.  So, we're going to
		// create our own invisible Farmer instance and store it here:
		Farmer farmer;

		Vector2 position;	// our current position, in pixels
		Vector2 targetPos;	// position we're moving to, in pixels
		

		static int uniqueFarmerID = 1;
		const float speed = 64;		// pixels/sec

		static Texture2D botSprites;

		public Bot(Vector2 tileLocation) :base(true, tileLocation) {
			if (botSprites == null) {
				botSprites = ModEntry.helper.Content.Load<Texture2D>("assets/BotSprites.png");
			}

			var initialTools = new List<Item>();
			initialTools.Add(new StardewValley.Tools.Hoe());

			foreach (Item i in initialTools) addItem(i);

			Name = "Bot " + uniqueFarmerID;
			farmer = new Farmer(new FarmerSprite("Characters\\Farmer\\farmer_base"),
				new Vector2(100,100), 2,
				Name, initialTools, isMale: true);
			uniqueFarmerID++;
			//this.Type = "Crafting";	// (necessary for performDropDownAction to be called)
			ModEntry.instance.print($"Type: {this.Type}  bigCraftable:{bigCraftable}");
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

		public void Update() {
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
				ModEntry.instance.print($"Updated position to {position}, tileLocation to {TileLocation}; facing {farmer.FacingDirection}");
			}
		}

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
			spriteBatch.Draw(botSprites, position3, srcRect, Color.White * alpha, 0f,
				origin2,
				(scale.Y > 1f) ? getScale().Y : 4f, SpriteEffects.None, (float)(getBoundingBox(new Vector2(x, y)).Bottom) / 10000f);
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

			Game1.activeClickableMenu = new BotUIMenu(this);

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

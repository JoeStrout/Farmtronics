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
	public class Bot : StardewValley.Object {
		const int vanillaObjectTypeId = 125;	// "Golden Relic"

		// We need a Farmer to be able to use tools.  So, we're going to
		// create our own invisible Farmer instance and store it here:
		Farmer farmer;

		Vector2 position;	// our current position, in pixels
		Vector2 targetPos;	// position we're moving to, in pixels
		

		static int uniqueFarmerID = 1;
		const float speed = 64;		// pixels/sec


		public Bot()
		:base(vanillaObjectTypeId, 1, false, -1, 0) {
			var initialTools = new List<Item>();
			initialTools.Add(new StardewValley.Tools.Hoe());

			farmer = new Farmer(new FarmerSprite("Characters\\Farmer\\farmer_base"),
				new Vector2(100,100), 2,
				"Bot " + uniqueFarmerID, initialTools, isMale: true);
			uniqueFarmerID++;
			this.Type = "Crafting";	// (necessary for performDropDownAction to be called)
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

		public void MoveLeft() {
			targetPos.X -= 64f;
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
					location.Objects.Remove(TileLocation);
					// Update our tile pos, and add this object to the Objects list at the new position
					TileLocation = newTile;
					location.Objects.Add(newTile, this);
					// Update the invisible farmer
					farmer.setTileLocation(newTile);
				}
				ModEntry.instance.print($"Updated position to {position}, tileLocation to {TileLocation}");
			}
		}

		public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1) {
			//ModEntry.instance.print($"draw 1 at {x},{y}");
			float base_sort = (float)((y + 1) * 64) / 10000f + tileLocation.X / 50000f;
			draw(spriteBatch, (int)position.X, (int)position.Y, base_sort, alpha);
		}

		public override void draw(SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha = 1) {
			//ModEntry.instance.print($"draw 2 at {xNonTile},{yNonTile}");
			base.draw(spriteBatch, xNonTile, yNonTile, layerDepth, alpha);
		}
	}
}

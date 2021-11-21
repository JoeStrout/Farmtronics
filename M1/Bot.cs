/*
This class is a stardew valley Object subclass that represents a Bot.
 
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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

		static int uniqueFarmerID = 1;

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

		public void UseTool() {
			farmer.setTileLocation(TileLocation);
			farmer.CurrentToolIndex = 0;			
			Game1.toolAnimationDone(farmer);
		}
	}
}

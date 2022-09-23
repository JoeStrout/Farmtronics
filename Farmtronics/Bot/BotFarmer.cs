using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Dimensions;

namespace Farmtronics.Bot
{
	class BotFarmer : Farmer {
		internal Vector2 Destination = Vector2.Zero;
		
		// TODO: Figure out if this is needed
		// 		 The base function calls Game1.player which we need to avoid
		//		 but for some reason it works just fine at the moment, need to do more testing
		// public override Vector2 GetToolLocation(bool ignoreClick = false) {
		// 	Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		// 	switch (FacingDirection) {
		// 	case 0:
		// 		return new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y - 64);
		// 	case 1:
		// 		return new Vector2(boundingBox.X + boundingBox.Width + 64, boundingBox.Y + boundingBox.Height / 2);
		// 	case 2:
		// 		return new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height + 64);
		// 	case 3:
		// 		return new Vector2(boundingBox.X - 64, boundingBox.Y + boundingBox.Height / 2);
		// 	}
		// 	return default;
		// }
		
		public override void SetMovingUp(bool b) {
			if (!b) Halt();
			else moveUp = true;
		}

		public override void SetMovingRight(bool b) {
			if (!b) Halt();
			else moveRight = true;
		}

		public override void SetMovingDown(bool b) {
			if (!b) Halt();
			else moveDown = true;
		}

		public override void SetMovingLeft(bool b) {
			if (!b) Halt();
			else moveLeft = true;
		}
		
		public new Location nextPositionTile() {
			setMovingInFacingDirection();
			var newTile = base.nextPositionTile();
			Halt();
			return newTile;
		}
	}
}
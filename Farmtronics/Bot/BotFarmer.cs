using StardewValley;
using xTile.Dimensions;

namespace Farmtronics.Bot
{
	class BotFarmer : Farmer {
		// TODO: Figure out if this is needed
		// 		 The base function calls Game1.player which we need to avoid
		//		 but for some reason it works just fine at the moment, need to do more testing
		// public override Vector2 GetToolLocation(bool ignoreClick = false) {
		// 	Microsoft.Xna.Framework.Rectangle boundingBox = GetBoundingBox();
		// 	switch (FacingDirection) {
		// 	case 0:
		// 		return new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y - Game1.tileSize);
		// 	case 1:
		// 		return new Vector2(boundingBox.X + boundingBox.Width + Game1.tileSize, boundingBox.Y + boundingBox.Height / 2);
		// 	case 2:
		// 		return new Vector2(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height + Game1.tileSize);
		// 	case 3:
		// 		return new Vector2(boundingBox.X - Game1.tileSize, boundingBox.Y + boundingBox.Height / 2);
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

		public new void tryToMoveInDirection(int direction, bool isFarmer, int damagesFarmer, bool glider) {
			// For some reason the normal isCollidingPosition() check used in the base method doesn't work
			bool canPass = currentLocation.isTilePassable(nextPosition(direction), Game1.viewport);
			// ModEntry.instance.Monitor.Log($"tryToMoveInDirection: canPass: {canPass}");
			if (canPass) {
				switch (direction) {
				case 0:
					position.Y -= speed + addedSpeed;
					break;
				case 1:
					position.X += speed + addedSpeed;
					break;
				case 2:
					position.Y += speed + addedSpeed;
					break;
				case 3:
					position.X -= speed + addedSpeed;
					break;
				}
			}
		}
	}
}
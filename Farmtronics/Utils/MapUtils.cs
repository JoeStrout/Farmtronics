using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Farmtronics.Utils {
	static class MapUtils {
		public static Vector2 GetAbsolutePosition(this Vector2 tilePosition) {
			return tilePosition * Game1.tileSize;
		}
		
		public static Vector2 GetTilePosition(this Vector2 absolutePosition) {
			var tilePosition = absolutePosition / Game1.tileSize;
			tilePosition.Floor();
			return tilePosition;
		}
		
		public static int GetIntX(this Vector2 vector) {
			return (int)vector.X;
		}
		
		public static int GetIntY(this Vector2 vector) {
			return (int)vector.Y;
		}
		
		public static void MoveTowards(this Vector2 start, Vector2 dest) {
			start.X += MathF.Sign(dest.X - start.X);
			start.Y += MathF.Sign(dest.Y - start.Y);
		}
		
		public static Vector2 ToVector2(this Location location) {
			return new Vector2(location.X, location.Y);
		}

		public static Location ToLocation(this Vector2 vector) {
			return new Location(vector.GetIntX(), vector.GetIntY());
		}
		
		public static Location GetAbsoluteLocation(this Location location) {
			return location * Game1.tileSize;
		}
		
		public static ResourceClump GetCollidingResourceClump(this GameLocation gameLocation, Vector2 absolutePosition) {
			var bbox = new Rectangle(absolutePosition.GetIntX(), absolutePosition.GetIntY(), Game1.tileSize, Game1.tileSize);
			foreach (var clump in gameLocation.resourceClumps) {
				if (clump.getBoundingBox(clump.tile.Value).Intersects(bbox)) return clump;
			}
			return null;
		}
		
		public static string GetName(this ResourceClump clump) {			
			switch (clump.parentSheetIndex.Value) {
			case ResourceClump.boulderIndex: return "Boulder";
			case ResourceClump.hollowLogIndex: return "Hollow Log";
			case ResourceClump.meteoriteIndex: return "Meteorite";
			case ResourceClump.mineRock1Index:
			case ResourceClump.mineRock2Index:
			case ResourceClump.mineRock3Index:
			case ResourceClump.mineRock4Index: return "Mine Rock";
			case ResourceClump.stumpIndex: return "Stump";
			default: return "#" + clump.parentSheetIndex.Value;
			}
		}
	}
}
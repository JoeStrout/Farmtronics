// This module provides info about a tile in a location.
// Gathering that is a real PITA that has to draw from lots of different sources,
// so it gets its own file.
//
// It also contains some related methods to get info about objects and items,
// which you may well find on a tile.

using System.Collections.Generic;
using Farmtronics.Bot;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using Miniscript;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace Farmtronics.M1 {
	static class TileInfo {

		static ValString _name = new ValString("name");
		static ValString _type = new ValString("type");
		static ValString _unknown = new ValString("unknown");
		static ValString _passable = new ValString("passable");
		static ValString _treeType = new ValString("treeType");
		static ValString _growthStage = new ValString("growthStage");
		static ValString _health = new ValString("health");
		static ValString _stump = new ValString("stump");
		static ValString _tapped = new ValString("tapped");
		static ValString _hasSeed = new ValString("hasSeed");
		static ValString _crop = new ValString("crop");
		static ValString _mature = new ValString("mature");
		static ValString _phase = new ValString("phase");
		static ValString _dry = new ValString("dry");
		static ValString _dead = new ValString("dead");
		static ValString _maxPhase = new ValString("maxPhase");
		static ValString _harvestable = new ValString("harvestable");
		static ValString _harvestMethod = new ValString("harvestMethod");

		public static ValMap ToMap(StardewValley.Object obj, ValMap result, bool passableOnly) {
			string type = obj.Type;
			if (type == "asdf") type = obj.Name;	// because, c'mon.
			result.map[_type] = new ValString(type);
			result.map[_passable] = ValNumber.zero;
			if (passableOnly) return result;

			// ToDo: limit the following to ones that really apply for this type.
			result.map[_name] = new ValString(obj.Name);
			result["displayName"] = new ValString(obj.DisplayName);
			result["health"] = new ValNumber(obj.getHealth());
			if (obj.isLamp.Get()) result["isOn"] = ValNumber.Truth(obj.IsOn);
			result["quality"] = new ValNumber(obj.Quality);
			result.map[_harvestable] = ValNumber.Truth(obj.readyForHarvest.Get());
			result["minutesTillReady"] = new ValNumber(obj.MinutesUntilReady);
			result["value"] = new ValNumber(obj.sellToStorePrice());
			result["description"] = new ValString(obj.getDescription());

			IList<Item> inventory = null;
			if (obj is Chest chest) inventory = chest.items;
			else if(obj is BotObject bot) inventory = bot.inventory;
			if (inventory != null) {
				var list = new ValList();
				result["inventory"] = list;
				foreach (var item in inventory) {
					list.values.Add(TileInfo.ToMap(item, new ValMap()));
				}
			}
			return result;
		}

		public static ValMap ToMap(StardewValley.Item item, ValMap result) {
			if (item == null) return result;
			result.map[_type] = new ValString(item.GetType().Name);
			// ToDo: limit the following to ones that really apply for this type.
			result.map[_name] = new ValString(item.Name);
			result["displayName"] = new ValString(item.DisplayName);
			result["stack"] = new ValNumber(item.Stack);
			result["maxStack"] = new ValNumber(item.maximumStackSize());
			result["category"] = new ValString(item.getCategoryName());
			result["value"] = new ValNumber(item.salePrice());
			result["description"] = new ValString(item.getDescription().Trim());
			if (item is StardewValley.Tools.WateringCan can) {
				result["waterLeft"] = new ValNumber(can.WaterLeft);
				result["waterMax"] = new ValNumber(can.waterCanMax);
			}
			return result;
		}

		static ValMap ToMap(TerrainFeature feature, ValMap result, bool passableOnly) {
			if (feature == null) return result;
			result.map[_type] = result["name"] = new ValString(feature.GetType().Name);
			if (feature is Tree tree) {
				result.map[_passable] = ValNumber.zero;
				if (passableOnly) return result;
				result.map[_treeType] = new ValNumber(tree.treeType.Value);
				result.map[_growthStage] = new ValNumber(tree.growthStage.Value);
				result.map[_health] = new ValNumber(tree.health.Value);
				result.map[_stump] = ValNumber.Truth(tree.stump.Value);
				result.map[_tapped] = ValNumber.Truth(tree.tapped.Value);
				result.map[_hasSeed] = ValNumber.Truth(tree.hasSeed.Value);
			} else if (feature is HoeDirt hoeDirt) {
				if (passableOnly) return result;
				result.map[_dry] = ValNumber.Truth(hoeDirt.state.Value != 1);
				var crop = hoeDirt.crop;
				if (crop == null) result.map[_crop] = null;
				else {
					ValMap cropInfo = new ValMap();
					cropInfo.map[_phase] = new ValNumber(crop.currentPhase.Value);
					cropInfo.map[_maxPhase] = new ValNumber(crop.phaseDays.Count - 1);
					cropInfo.map[_mature] = ValNumber.Truth(crop.fullyGrown.Value);
					cropInfo.map[_dead] = ValNumber.Truth(crop.dead.Value);
					cropInfo.map[_harvestMethod] = ValNumber.Truth(crop.harvestMethod.Value);
					bool harvestable = (int)crop.currentPhase.Value >= crop.phaseDays.Count - 1
						&& (!crop.fullyGrown.Value || (int)crop.dayOfCurrentPhase.Value <= 0);
					cropInfo.map[_harvestable] = ValNumber.Truth(harvestable);

					//Note: we might be able to get the name of the crop
					// using crop.indexOfHarvest or crop.netSeedIndex
					var product = new StardewValley.Object(crop.indexOfHarvest.Value, 0);
					cropInfo.map[_name] = new ValString(product.DisplayName);

					result.map[_crop] = cropInfo;
				}
			}
			return result;
		}

		static ValMap ToMap(ResourceClump clump, ValMap result, bool passableOnly) {
			if (clump == null) return result;
			result.map[_type] = new ValString("Clump");
			result.map[_passable] = ValNumber.zero;
			if (passableOnly) return result;
			string name = clump.GetName();
			result.map[_name] = new ValString(name);
			result.map[_health] = new ValNumber(clump.health.Value);
			return result;
		}

		static ValMap ToMap(Character character, ValMap result, bool passableOnly) {
			if (character == null) return result;
			result.map[_passable] = ValNumber.zero;
			if (passableOnly) return result;
			result.map[_type] = new ValString("Character");
			result.map[_name] = new ValString(character.Name);
			result["displayName"] = new ValString(character.displayName);
			result["facing"] = new ValNumber(character.FacingDirection);
			result["isEmoting"] = ValNumber.Truth(character.isEmoting);
			result["emote"] = new ValNumber(character.CurrentEmote);
			result["isMonster"] = ValNumber.Truth(character.IsMonster);

			return result;
		}

		/// <summary>
		/// Report whether the given location is something a bot can pass through.
		/// </summary>
		public static bool IsPassable(GameLocation loc, Vector2 xy) {
			// Because there are so many different cases to handle, and we're already handling
			// all those cases in GetInfo, we just call through to that -- but with passableOnly
			// set to true, so it can bail out early once it's figured that out.
			ValMap info = GetInfo(loc, xy, true);
			return info == null || info.map[_passable].BoolValue();
		}

		public static ValMap GetInfo(GameLocation loc, Vector2 xy, bool passableOnly=false) {

			var result = new ValMap();
			result.map[_passable] = ValNumber.one;
			result.map[_type] = _unknown;

			// check farmers
			if (Game1.player.currentLocation == loc && Game1.player.getTileLocation() == xy) return ToMap(Game1.player, result, passableOnly);
			foreach (var farmer in Game1.otherFarmers.Values) {
				if (farmer.currentLocation == loc && farmer.getTileLocation() == xy) return ToMap(farmer, result, passableOnly);
			}

			// check NPCs
			foreach (var character in loc.characters) {
				if (character.getTileLocation() == xy) {
					return ToMap(character, result, passableOnly);
				}
			}

			// check objects
			StardewValley.Object obj = null;
			loc.objects.TryGetValue(xy, out obj);
			if (obj != null) {
				result.map[_passable] = ValNumber.zero;
				if (passableOnly) return result;
				return ToMap(obj, result);
			}

			// check for buildings in the buildings list (which are not always in the buildings layer!)
			if (loc is BuildableGameLocation) {
				var bl = loc as BuildableGameLocation;
				foreach (var b in bl.buildings) {
					if (xy.X >= b.tileX.Value && xy.X < b.tileX.Value + b.tilesWide.Value
							&& xy.Y >= b.tileY.Value && xy.Y < b.tileY.Value + b.tilesHigh.Value) {
						result.map[_type] = new ValString("Building");
						result.map[_name] = new ValString(b.buildingType.ToString());
						result.map[_passable] = ValNumber.zero;
						return result;
                    }
                }
            }

			// check terrain features
			TerrainFeature feature = null;
			loc.terrainFeatures.TryGetValue(xy, out feature);
			if (feature != null) return ToMap(feature, result, passableOnly);

			// check LARGE terrain features
			// (not 100% certain we need to check these separately, but maybe)
			var absoluteXy = xy.GetAbsolutePosition();
			var xyBounds = new Rectangle(absoluteXy.GetIntX(), absoluteXy.GetIntY(), Game1.tileSize, Game1.tileSize);
			foreach (LargeTerrainFeature ltf in loc.largeTerrainFeatures) {
				if (ltf.getBoundingBox().Intersects(xyBounds)) return ToMap(ltf, result, passableOnly);
			}

			// check resource clumps (these span multiple tiles)
			var clump = loc.GetCollidingResourceClump(absoluteXy);
			if (clump != null) return ToMap(clump, result, passableOnly);

			// check water and other such terrain properties
			int x = (int)xy.X;
			int y = (int)xy.Y;
			string hasProp = null;
			string[] propNames = {"Water", "Trough", "Bed"};
			foreach (string prop in propNames) { 
				if (loc.doesTileHaveProperty(x, y, prop, "Back") != null) hasProp = prop;
			}
			if (!string.IsNullOrEmpty(hasProp)) { 
				result.map[_passable] = ValNumber.zero;
				result.map[_type] = new ValString("Property");
				result.map[_name] = new ValString(hasProp);
				return result;
			}

			// check buildings (any not covered above -- such as the cabin)
			var tileLocation = new xTile.Dimensions.Location(x*Game1.tileSize, y*Game1.tileSize);
			var buildings_layer = loc.map.GetLayer("Buildings");
			var tmp = buildings_layer.PickTile(tileLocation, Game1.viewport.Size);
			if (tmp != null) {
				result.map[_type] = new ValString("Building");
				result.map[_name] = result.map[_type];
				var p = loc.doesTileHaveProperty(x, y, "Passable", "Buildings");
				if (!string.IsNullOrEmpty(p)) result.map[_passable] = ValNumber.one;
				if (!passableOnly) {
					p = loc.doesTileHaveProperty(x, y, "Action", "Buildings");
					if (!string.IsNullOrEmpty(p)) result.map[new ValString("action")] = new ValString(p);
				}
				return result;
			}

#if DEBUG
			// for debugging: check properties in various layers
			string[] layers = {"Front", "Back", "Buildings", "Paths", "AlwaysFront"};
			foreach (string layer in layers) {
				var mapLayer = loc.map.GetLayer(layer);
				if (mapLayer == null) continue;
				var tile = mapLayer.PickTile(tileLocation, Game1.viewport.Size);
				if (tile == null) continue;
				foreach (var kv in tile.TileIndexProperties) {
					Debug.Log($"layer {layer}, {kv.Key} = {kv.Value}");
				}
			}
#endif

			// If there is nothing at all of interest, return null rather
			// than a map that contains only passable:true (and a type).
			if (result.map[_passable].BoolValue()) return null;
			return result;
		}

	}
}
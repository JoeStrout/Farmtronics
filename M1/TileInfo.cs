// This module provides info about a tile in a location.
// Gathering that is a real PITA that has to draw from lots of different sources,
// so it gets its own file.
//
// It also contains some related methods to get info about objects and items,
// which you may well find on a tile.

using System;
using Miniscript;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;
using StardewValley.TerrainFeatures;


namespace Farmtronics {
	public static class TileInfo {

		static ValString _name = new ValString("name");
		static ValString _type = new ValString("type");
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

		public static ValMap ToMap(StardewValley.Object obj) {
			var result = new ValMap();
			string type = obj.Type;
			if (type == "asdf") type = obj.Name;	// because, c'mon.
			result.map[_type] = new ValString(type);
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
			return result;
		}

		public static ValMap ToMap(StardewValley.Item item) {
			if (item == null) return null;
			var result = new ValMap();
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

		static ValMap ToMap(TerrainFeature feature) {
			if (feature == null) return null;
			var result = new ValMap();
			result.map[_type] = result["name"] = new ValString(feature.GetType().Name);
			if (feature is Tree tree) {
				result.map[_treeType] = new ValNumber(tree.treeType.Value);
				result.map[_growthStage] = new ValNumber(tree.growthStage.Value);
				result.map[_health] = new ValNumber(tree.health.Value);
				result.map[_stump] = ValNumber.Truth(tree.stump.Value);
				result.map[_tapped] = ValNumber.Truth(tree.tapped.Value);
				result.map[_hasSeed] = ValNumber.Truth(tree.hasSeed.Value);
			} else if (feature is HoeDirt hoeDirt) {
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

		static ValMap ToMap(ResourceClump clump) {
			if (clump == null) return null;
			var result = new ValMap();
			result.map[_type] = new ValString("Clump");
			string name;
			switch (clump.parentSheetIndex.Value) {
			case ResourceClump.boulderIndex: name = "Boulder"; break;
			case ResourceClump.hollowLogIndex: name = "Hollow Log"; break;
			case ResourceClump.meteoriteIndex: name = "Meteorite"; break;
			case ResourceClump.mineRock1Index:
			case ResourceClump.mineRock2Index:
			case ResourceClump.mineRock3Index:
			case ResourceClump.mineRock4Index: name = "Mine Rock"; break;
			case ResourceClump.stumpIndex: name = "Stump"; break;
			default: name = "#" + clump.parentSheetIndex.Value; break;
			}
			result.map[_name] = new ValString(name);
			result.map[_health] = new ValNumber(clump.health.Value);
			return result;
		}

		public static ValMap GetInfo(GameLocation loc, Vector2 xy) {
			// check objects
			StardewValley.Object obj = null;
			loc.objects.TryGetValue(xy, out obj);
			if (obj != null) return ToMap(obj);

			// check terrain features
			TerrainFeature feature = null;
			loc.terrainFeatures.TryGetValue(xy, out feature);
			if (feature != null) return ToMap(feature);

			// check resource clumps (these span multiple tiles)
			var bbox = new Rectangle((int)xy.X * 64, (int)xy.Y * 64, 64, 64);
			foreach (var clump in loc.resourceClumps) {
				if (clump.getBoundingBox(clump.tile.Value).Intersects(bbox)) return ToMap(clump);
			}

			// check water and other such terrain properties
			int x = (int)xy.X;
			int y = (int)xy.Y;
			string hasProp = null;
			if (loc.doesTileHaveProperty(x, y, "Water", "Back") != null) hasProp = "Water";
			if (loc.doesTileHaveProperty(x, y, "Trough", "Back") != null) hasProp = "Trough";
			if (loc.doesTileHaveProperty(x, y, "Bed", "Back") != null) hasProp = "Bed";
			if (!string.IsNullOrEmpty(hasProp)) { 
				var result = new ValMap();
				result.map[_type] = new ValString("Property");
				result.map[_name] = new ValString(hasProp);
				return result;
			}

			return null;
		}

	}
}

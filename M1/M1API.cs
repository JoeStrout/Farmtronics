/*
This static class implements the APIs that extend MiniScript with
custom intrinsic functions/classes for use on the M-1.
*/

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



namespace M1 {
	public static class M1API  {

		static bool initialized;
	
		public static Shell shell;		// these should be assigned whenever a shell is accessed
		public static Console console;	// (usually by calling Init)

		static ValString _size = new ValString("size");
		static ValString _name = new ValString("name");
		static ValString _type = new ValString("type");
		static ValString _treeType = new ValString("treeType");
		static ValString _growthStage = new ValString("growthStage");
		static ValString _health = new ValString("health");
		static ValString _stump = new ValString("stump");
		static ValString _tapped = new ValString("tapped");
		static ValString _hasSeed = new ValString("hasSeed");


		public static void Init(Shell shell) {
			M1API.shell = shell;
			console = shell.console;

			if (initialized) return;
			initialized = true;

			HostInfo.name = "Mini Micro";
			HostInfo.version = 1.0;
			HostInfo.info = "http://miniscript.org/MiniMicro";
		
			Intrinsic f;
		
			f = Intrinsic.Create("farm");
			f.code = (context, partialResult) => {
				var loc = (Farm)Game1.getLocationFromName("Farm");
				var layer = loc.map.Layers[0];
				var result = new ValMap();
				result.map[ValString.magicIsA] = LocationClass();
				result.map[_name] = new ValString("Farm");
				
				result.map[_size] = ToList(layer.LayerWidth, layer.LayerHeight);
				return new Intrinsic.Result(result);
			};
		}


		static ValMap locationClass;
		public static ValMap LocationClass() {
			if (locationClass != null) return locationClass;

			locationClass = new ValMap();
			locationClass.map[_name] = null;
		
			Intrinsic f;
		
			// Location.tile
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.code = (context, partialResult) => {
				ValMap self = context.GetVar("self") as ValMap;
				if (self == null) throw new RuntimeException("Map required for Location.tile parameter");
				int x = context.variables.GetInt("x", 0);
				int y = context.variables.GetInt("y", 0);
				string name = self.Lookup(_name).ToString();
				var loc = Game1.getLocationFromName(name);
				if (loc == null) return Intrinsic.Result.Null;
				TerrainFeature feature = null;
				if (!loc.terrainFeatures.TryGetValue(new Vector2(x,y), out feature)) {
					return Intrinsic.Result.Null;
				}
				var result = new ValMap();
				result.map[_type] = new ValString(feature.GetType().Name);
				if (feature is Tree tree) {
					result.map[_treeType] = new ValNumber(tree.treeType.Value);
					result.map[_growthStage] = new ValNumber(tree.growthStage.Value);
					result.map[_health] = new ValNumber(tree.health.Value);
					result.map[_stump] = ValNumber.Truth(tree.stump.Value);
					result.map[_tapped] = ValNumber.Truth(tree.tapped.Value);
					result.map[_hasSeed] = ValNumber.Truth(tree.hasSeed.Value);
				}
				return new Intrinsic.Result(result);
			};
			locationClass["tile"] = f.GetFunc();

			return locationClass;
		}

		static ValList ToList(double a, double b) {
			var result = new ValList();
			result.values.Add(new ValNumber(a));
			result.values.Add(new ValNumber(b));
			return result;
		}
	}
}

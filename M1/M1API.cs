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

		public static ValString _stackAtBreak = new ValString("_stackAtBreak");
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

			// language host info

			HostInfo.name = "Mini Micro";
			HostInfo.version = 1.0;
			HostInfo.info = "http://miniscript.org/MiniMicro";
		
			Intrinsic f;

			// global intrinsics
			// ...let's try to keep these alphabetical, shall we?

			f = Intrinsic.Create("_debugLog");
			f.AddParam("s");
			f.code = (context, partialResult) => {
				string s = context.variables.GetString("s");
				Debug.Log(s);
				return Intrinsic.Result.Null;
			};

			f = Intrinsic.Create("facing");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(new ValNumber(sh.bot.facingDirection));
			};

			f = Intrinsic.Create("currentToolIndex");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(new ValNumber(sh.bot.currentToolIndex));
			};

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

			f = Intrinsic.Create("forward");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				if (partialResult == null) {
					// Just starting our move; tell the bot and return partial result
					shell.bot.MoveForward();
					return new Intrinsic.Result(null, false);
				} else {
					// Continue until bot stops moving
					if (shell.bot.IsMoving()) return partialResult;
					return Intrinsic.Result.Null;
				}

			};

			f = Intrinsic.Create("inventory");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				ValList result = new ValList();
				foreach (var item in sh.bot.inventory) {
					result.values.Add(ToMap(item));
				}
				return new Intrinsic.Result(result);
			};

			f = Intrinsic.Create("left");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				shell.bot.Rotate(-1);
				return Intrinsic.Result.Null;
			};

			f = Intrinsic.Create("position");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				var pos = sh.bot.TileLocation;
				var loc = sh.bot.currentLocation;
				Debug.Log($"Got location {loc} ({loc.Name}, {loc.uniqueName}), pos {pos}");
				var result = new ValMap();
				result["x"] = new ValNumber(pos.X);
				result["y"] = new ValNumber(pos.Y);
				var area = new ValMap();
				area.map[ValString.magicIsA] = LocationClass();
				area.map[_name] = new ValString(loc.NameOrUniqueName);
				result["area"] = area;
				return new Intrinsic.Result(result);
			};

			f = Intrinsic.Create("right");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				shell.bot.Rotate(1);
				return Intrinsic.Result.Null;
			};

			f = Intrinsic.Create("useTool");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				
				if (partialResult == null) {
					// Just starting our tool use; tell the bot and return partial result
					shell.bot.UseTool();
					return new Intrinsic.Result(null, false);
				} else {
					// Continue until bot is done using the tool
					if (shell.bot.isUsingTool) return partialResult;
					return Intrinsic.Result.Null;
				}
			};

		}


		static ValMap locationClass;
		public static ValMap LocationClass() {
			if (locationClass != null) return locationClass;

			locationClass = new ValMap();
			locationClass.map[_name] = null;
		
			Intrinsic f;

			// Location.height
			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				ValMap self = context.GetVar("self") as ValMap;
				string name = self.Lookup(_name).ToString();
				var loc = Game1.getLocationFromName(name);
				if (loc == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(new ValNumber(loc.map.Layers[0].LayerHeight));
			};
			locationClass["height"] = f.GetFunc();

			// Location.tile
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.code = (context, partialResult) => {
				ValMap self = context.GetVar("self") as ValMap;
				if (self == null) throw new RuntimeException("Map required for Location.tile parameter");
				int x = context.GetLocalInt("x", 0);
				int y = context.GetLocalInt("y", 0);
				Vector2 xy = new Vector2(x,y);
				string name = self.Lookup(_name).ToString();
				var loc = Game1.getLocationFromName(name);
				if (loc == null) return Intrinsic.Result.Null;

				ValMap result = null;

				// check objects
				StardewValley.Object obj = null;
				loc.objects.TryGetValue(xy, out obj);
				Debug.Log($"Object at {xy}: {obj}");
				if (obj != null) {
					result = ToMap(obj);
				} else {
					// check terrain features
					TerrainFeature feature = null;
					if (!loc.terrainFeatures.TryGetValue(new Vector2(x,y), out feature)) {
						Debug.Log($"no terrain features at {xy}");
						return Intrinsic.Result.Null;
					}
					Debug.Log($"terrain features at {xy}: {feature}");
					if (result == null) result = new ValMap();
					result.map[_type] = result["name"] = new ValString(feature.GetType().Name);
					if (feature is Tree tree) {
						result.map[_treeType] = new ValNumber(tree.treeType.Value);
						result.map[_growthStage] = new ValNumber(tree.growthStage.Value);
						result.map[_health] = new ValNumber(tree.health.Value);
						result.map[_stump] = ValNumber.Truth(tree.stump.Value);
						result.map[_tapped] = ValNumber.Truth(tree.tapped.Value);
						result.map[_hasSeed] = ValNumber.Truth(tree.hasSeed.Value);
					}
				}
				if (result == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(result);
			};
			locationClass["tile"] = f.GetFunc();

			// Location.width
			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				ValMap self = context.GetVar("self") as ValMap;
				string name = self.Lookup(_name).ToString();
				var loc = Game1.getLocationFromName(name);
				if (loc == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(new ValNumber(loc.map.Layers[0].LayerWidth));
			};
			locationClass["width"] = f.GetFunc();


			return locationClass;
		}

		static ValList ToList(double a, double b) {
			var result = new ValList();
			result.values.Add(new ValNumber(a));
			result.values.Add(new ValNumber(b));
			return result;
		}

		static ValMap ToMap(StardewValley.Object obj) {
			var result = new ValMap();
			string type = obj.Type;
			if (type == "asdf") type = obj.Name;
			result.map[_type] = new ValString(type);
			// ToDo: limit the following to ones that really apply for this type.
			result.map[_name] = new ValString(obj.Name);
			result["displayName"] = new ValString(obj.DisplayName);
			result["health"] = new ValNumber(obj.getHealth());
			if (obj.isLamp.Get()) result["isOn"] = ValNumber.Truth(obj.IsOn);
			result["quality"] = new ValNumber(obj.Quality);
			result["readyForHarvest"] = ValNumber.Truth(obj.readyForHarvest.Get());
			result["minutesTillReady"] = new ValNumber(obj.MinutesUntilReady);
			result["value"] = new ValNumber(obj.sellToStorePrice());
			result["description"] = new ValString(obj.getDescription());
			return result;
		}

		static ValMap ToMap(StardewValley.Item item) {
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
			result["description"] = new ValString(item.getDescription());
			return result;
		}

		public static ValList StackList(TAC.Machine vm) {
			ValList result = new ValList();
			foreach (SourceLoc loc in vm.GetStack()) {
				if (loc == null) continue;
				string s = loc.context;
				if (string.IsNullOrEmpty(s)) s = "(current program)";
				s += " line " + loc.lineNum;
				result.values.Add(new ValString(s));
			}
			return result;
		}

	}
}

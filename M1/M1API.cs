/*
This static class implements the APIs that extend MiniScript with
custom intrinsic functions/classes for use on the M-1.
*/

using System;
using System.Collections.Generic;
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

			f = Intrinsic.Create("bot");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				if (sh.bot == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(BotModule());
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

			f = Intrinsic.Create("Location");
			f.code = (context, partialResult) => {
				return new Intrinsic.Result(LocationClass());
			};

			f = Intrinsic.Create("text");
			f.code = (context, partialResult) => {
				return new Intrinsic.Result(TextModule());
			};


		}

		static ValMap botModule;
		public static ValMap BotModule() {
			if (botModule != null) return botModule;

			botModule = new ValMap();

			Intrinsic f;

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(new ValNumber(sh.bot.facingDirection));
			};
			botModule["facing"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(new ValNumber(sh.bot.currentToolIndex));
			};
			botModule["currentToolIndex"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(new ValString(sh.bot.statusColor.ToHexString()));
			};
			botModule["statusColor"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				if (partialResult == null) {
					// Just starting our move; tell the bot and return partial result
					sh.bot.MoveForward();
					return new Intrinsic.Result(null, false);
				} else {
					// Continue until bot stops moving
					if (sh.bot.IsMoving()) return partialResult;
					return Intrinsic.Result.Null;
				}
			};
			botModule["forward"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				ValList result = new ValList();
				foreach (var item in sh.bot.inventory) {
					result.values.Add(ToMap(item));
				}
				return new Intrinsic.Result(result);
			};
			botModule["inventory"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				sh.bot.Rotate(-1);
				return Intrinsic.Result.Null;
			};
			botModule["left"] = f.GetFunc();

			f = Intrinsic.Create("");
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
			botModule["position"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				sh.bot.Rotate(1);
				return Intrinsic.Result.Null;
			};
			botModule["right"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				
				if (partialResult == null) {
					// Just starting our tool use; tell the bot and return partial result
					sh.bot.UseTool();
					return new Intrinsic.Result(null, false);
				} else {
					// Continue until bot is done using the tool
					if (shell.bot.isUsingTool) return partialResult;
					return Intrinsic.Result.Null;
				}
			};
			botModule["useTool"] = f.GetFunc();


			botModule.assignOverride = (key,value) => {
				string keyStr = key.ToString();
				if (keyStr == "_") return false;
				Debug.Log($"global {key} = {value}");
				if (keyStr == "statusColor") {
					Shell.runningInstance.bot.statusColor = value.ToString().ToColor();
					return true;
				} else if (keyStr == "currentToolIndex") {
					Shell.runningInstance.bot.currentToolIndex = value.IntValue();
					return true;
				}
				return false;	// allow the assignment
			};

			return botModule;
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


		static ValMap textModule;
		public static ValMap TextModule() {
			if (textModule != null) return textModule;

			textModule = new ValMap();
			
			Intrinsic f;

			// TextDisplay.clear
			//	Clear the text display, setting all cells to " " (space), with
			//	inverse turned off and all cell colors set to the match the display
			//	properties.  Note that this method does **not** change the cursor
			//	position, nor does it reset TextDisplay.delimiter.
			// Example:
			//	text.clear   // clears the default text display
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				disp.Clear();
				return Intrinsic.Result.Null;
			};
			textModule["clear"] = f.GetFunc();
		
			// TextDisplay.color
			//	Get or set the foreground color used on any future printing to this
			//	text display.  This is the text color for normal text, or the surrounding
			//	color for inverse-mode text.
			// Example:
			//	text.color = color.aqua
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				return new Intrinsic.Result(new ValString(disp.textColor.ToHexString()));
			};
			textModule["color"] = f.GetFunc();
		
			// TextDisplay.backColor
			//	Get or set the background color used on any future printing to this
			//	text display.  This is the surrounding color for normal text, or the
			//	text color for inverse-mode text.
			// Example:
			//	text.backColor = color.navy
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				return new Intrinsic.Result(new ValString(disp.backColor.ToHexString()));
			};
			textModule["backColor"] = f.GetFunc();

			// TextDisplay.column
			//	Get or set the column of the text cursor, where subsequent printing
			//	will begin.  Column values range from 0 on the left to 67 on the right.
			// Example:
			//	text.column = 60; print "HEY!"
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				return new Intrinsic.Result(new ValNumber(disp.GetCursor().col));
			};
			textModule["column"] = f.GetFunc();
		
			// TextDisplay.row
			//	Get or set the row of the text cursor, where subsequent printing will
			//	begin.  Row values range from 0 at the bottom of the screen, to 25 at the top.
			// Example:
			//	text.row = 25; print "At the top!"
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				return new Intrinsic.Result(new ValNumber(disp.GetCursor().row));
			};
			textModule["row"] = f.GetFunc();
		
			// TextDisplay.inverse
			//	Get or set whether subsequent printing should be done in "inverse" mode,
			//	where the foreground and background colors are swapped.  (Note that this
			//	mode may also be controlled by printing two special characters: 
			//	char(134) sets inverse to true, and char(135) sets inverse to false.)
			// Example:
			//	text.inverse = true; print " BLOCKY "; text.inverse = false
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				return new Intrinsic.Result(ValNumber.Truth(disp.inverse));
			};
			textModule["inverse"] = f.GetFunc();
		
			// TextDisplay.delimiter
			//	This value is a string which is printed after every [[print]] output.
			//	Its default value is char(13), which is a carriage return (moves the
			//	cursor to the start of the next line).  You may set it to "" (empty
			//	string) if you want no delimiter, allowing you to print several things
			//	in a row all on the same line.  Note that TextDisplay.clear does not
			//	reset this value; you will need to manually reassign char(13) to it
			//	to restore normal behavior.
			// Example:
			//	text.delimiter = ""
			//	print "one"
			//	print "two"
			//	text.delimiter = char(13)
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				return new Intrinsic.Result(new ValString(disp.delimiter));
			};
			textModule["delimiter"] = f.GetFunc();
		
			// TextDisplay.cell
			//	Returns the character stored at a given row and column
			//	of the text display.
			// x (number, default 0): column of interest
			// y (number, default 0): row of interest
			// See also: TextDisplay.setCell
			// Example:
			//	print text.cell(0,25)		// print character in top-left corner
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.code = (context, partialResult) => {
				TextDisplay.Cell cell = ReferencedCell(context);
				if (cell == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(new ValString(cell.character.ToString()));
			};
			textModule["cell"] = f.GetFunc();

			// TextDisplay.setCell
			//	Directly sets a character into a given row and column of the text
			//	display.  This does not use (or change) the cursor position, nor
			//	does it apply the current text colors or inverse mode; it only changes
			//	the character displayed.
			// See also: TextDisplay.cell
			// x (number or list, default 0): column of cell to change
			// y (number or list, default 0): row of cell to change
			// character (string): character to store in the given cell
			// Example: text.setCell 0, 10, "@"
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.AddParam("character", ValString.empty);
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				Value x = context.GetLocal("x");
				Value y = context.GetLocal("y");
				string s = context.variables.GetString("character");
				char c = (s != null && s.Length > 0 ? s[0] : '\0');
				if (!(x is ValList) && !(y is ValList)) {
					// trivial case: scalar x and y
					var cell = disp.Get(y.IntValue(), x.IntValue());
					if (cell == null) return Intrinsic.Result.Null;
					cell.character = c;
					//disp.UpdateCell(cell);
				} else {
					// harder case: list of values for x and/or y
					List<Value> xlist;
					if (x is ValList) xlist = ((ValList)x).values;
					else xlist = new List<Value>() { x };
					List<Value> ylist;
					if (y is ValList) ylist = ((ValList)y).values;
					else ylist = new List<Value>() { y };
					foreach (Value yval in ylist) {
						foreach (Value xval in xlist) {
							var cell = disp.Get(yval.IntValue(), xval.IntValue());
							if (cell == null) continue;
							cell.character = c;
							//disp.UpdateCell(cell);
						}
					}
				}
				return Intrinsic.Result.Null;
			};
			textModule["setCell"] = f.GetFunc();

			// TextDisplay.cellColor
			//	Returns the foreground color of the given cell.  Note that this may
			//	appear as either the text color or the surrounding color, depending
			//	on whether the given cell is in inverse mode.
			// x (number, default 0): column of interest
			// y (number, default 0): row of interest
			// See also: TextDisplay.setCellColor
			// Example: print text.cellColor 0, 10
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.code = (context, partialResult) => {
				TextDisplay.Cell cell = ReferencedCell(context);
				if (cell == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(new ValString(cell.foreColor.ToHexString()));
			};
			textModule["cellColor"] = f.GetFunc();

			// TextDisplay.setCellColor
			//	Changes the foreground color of the given cell.  Note that this may
			//	appear as either the text color or the surrounding color, depending
			//	on whether the given cell is in inverse mode.
			// x (number or list, default 0): column of cell to change
			// y (number or list, default 0): row of cell to change
			// color (string, default "#FFFFFF"): color to apply
			// See also: TextDisplay.cellColor
			// Example: text.setCellColor 0, 10, color.red
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.AddParam("color", new ValString("#FFFFFF"));
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				string s = context.GetLocalString("color");
				Color color = s.ToColor();
				Value x = context.GetLocal("x");
				Value y = context.GetLocal("y");
				if (!(x is ValList) && !(y is ValList)) {
					// trivial case: scalar x and y
					var cell = disp.Get(y.IntValue(), x.IntValue());
					if (cell != null) {
						cell.foreColor = color;
						//disp.UpdateCell(cell);
					}
				} else {
					// harder case: list of values for x and/or y
					List<Value> xlist;
					if (x is ValList) xlist = ((ValList)x).values;
					else xlist = new List<Value>() { x };
					List<Value> ylist;
					if (y is ValList) ylist = ((ValList)y).values;
					else ylist = new List<Value>() { y };
					foreach (Value yval in ylist) {
						foreach (Value xval in xlist) {
							var cell = disp.Get(yval.IntValue(), xval.IntValue());
							if (cell == null) continue;
							cell.foreColor = color;
							//disp.UpdateCell(cell);
						}
					}
				}
				return Intrinsic.Result.Null;
			};
			textModule["setCellColor"] = f.GetFunc();

			// TextDisplay.cellBackColor
			//	Returns the background color of the given cell.  Note that this may
			//	appear as either the text color or the surrounding color, depending
			//	on whether the given cell is in inverse mode.
			// x (number, default 0): column of interest
			// y (number, default 0): row of interest
			// See also: TextDisplay.setCellBackColor
			// Example: print text.cellBackColor 0, 10
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.code = (context, partialResult) => {
				TextDisplay.Cell cell = ReferencedCell(context);
				if (cell == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(new ValString(cell.backColor.ToHexString()));
			};
			textModule["cellBackColor"] = f.GetFunc();

			// TextDisplay.setCellBackColor
			//	Changes the background color of the given cell.  Note that this may
			//	appear as either the text color or the surrounding color, depending
			//	on whether the given cell is in inverse mode.
			// x (number or list, default 0): column of cell to change
			// y (number or list, default 0): row of cell to change
			// color (string, default "#FFFFFF"): color to apply
			// See also: TextDisplay.cellBackColor
			// Example: text.setCellBackColor 0, 10, color.blue
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.AddParam("color", new ValString("#FFFFFF"));
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				string s = context.GetLocalString("color");
				Color color = s.ToColor();
				Value x = context.GetLocal("x");
				Value y = context.GetLocal("y");
				if (!(x is ValList) && !(y is ValList)) {
					// trivial case: scalar x and y
					var cell = disp.Get(y.IntValue(), x.IntValue());
					if (cell == null) return Intrinsic.Result.Null;
					cell.backColor = color;
					//disp.UpdateCell(cell);
				} else {
					// harder case: list of values for x and/or y
					List<Value> xlist;
					if (x is ValList) xlist = ((ValList)x).values;
					else xlist = new List<Value>() { x };
					List<Value> ylist;
					if (y is ValList) ylist = ((ValList)y).values;
					else ylist = new List<Value>() { y };
					foreach (Value yval in ylist) {
						foreach (Value xval in xlist) {
							var cell = disp.Get(yval.IntValue(), xval.IntValue());
							if (cell == null) continue;
							cell.backColor = color;
							//disp.UpdateCell(cell);
						}
					}
				}
				return Intrinsic.Result.Null;
			};
			textModule["setCellBackColor"] = f.GetFunc();

			// TextDisplay.print
			//	Print a given string to this text display, followed by whatever
			//	TextDisplay.delimiter contains.  For the default text display,
			//	this is equivalent to [[print]] by itself, so calling this as a
			//	TextDisplay method is mainly useful when you have set up multiple
			//	text displays.
			// s (string): string to print
			// Example:
			//	text.print "Hello World!"
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("s");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				TextDisplay disp = sh.textDisplay;
				string s = context.variables.GetString("s");
				disp.Print(s);
				if (!string.IsNullOrEmpty(disp.delimiter)) disp.Print(disp.delimiter);
				return Intrinsic.Result.Null;
			};
			textModule["print"] = f.GetFunc();
		
			textModule.assignOverride = (key, value) => {
				TextDisplay disp = Shell.runningInstance.textDisplay;
				if (value == null) value = ValNumber.zero;
				switch (key.ToString()) {
				case "color":
					disp.textColor = value.ToString().ToColor();
					break;
				case "backColor":
					disp.backColor = value.ToString().ToColor();
					break;
				case "column":
					disp.SetCursor(disp.cursorY, value.IntValue());
					break;
				case "row":
					disp.SetCursor(value.IntValue(), disp.cursorX);
					break;
				case "inverse":
					disp.inverse = value.BoolValue();
					break;
				case "delimiter":
					disp.delimiter = value.ToString();
					break;
				}
				return true;
			};
			return textModule;
		}

		/// <summary>
		/// Helper method for various Text class methods that get
		/// a cell from the self, x, and y parameters in the context.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		static TextDisplay.Cell ReferencedCell(TAC.Context context, out TextDisplay disp) {
			Shell sh = context.interpreter.hostData as Shell;
			disp = sh.textDisplay;
			int x = context.GetLocalInt("x");
			int y = context.GetLocalInt("y");
			return disp.Get(y, x);		
		}

		static TextDisplay.Cell ReferencedCell(TAC.Context context) {
			TextDisplay disp;
			return ReferencedCell(context, out disp);
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

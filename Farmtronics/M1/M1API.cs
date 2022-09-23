/*
This static class implements the APIs that extend MiniScript with
custom intrinsic functions/classes for use on the M-1.
*/

using System.Collections.Generic;
using System.Globalization;
using Farmtronics.M1.Filesystem;
using Farmtronics.M1.GUI;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using Miniscript;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace Farmtronics.M1 {
	static class M1API  {

		static bool initialized;

		public static Shell shell;		// these should be assigned whenever a shell is accessed
		public static Console console;	// (usually by calling Init)

		public static ValString _stackAtBreak = new ValString("_stackAtBreak");
		static ValString _size = new ValString("size");
		static ValString _name = new ValString("name");
		static ValString _handle = new ValString("_handle");

		public static ValMap locationsMap;	// key: name; value: Location subclass

		public static void Init(Shell shell) {
			M1API.shell = shell;
			console = shell.console;

			if (initialized) return;
			initialized = true;

			// language host info
			string version = ModEntry.instance.ModManifest.Version.ToString();
			HostInfo.name = ModEntry.instance.ModManifest.Name;
			HostInfo.version = double.Parse(version.Remove(version.LastIndexOf('.'), 1));
			HostInfo.info = "https://github.com/JoeStrout/Farmtronics/";
		
			Intrinsic f;

			// global intrinsics
			// ...let's try to keep these alphabetical, shall we?

			f = Intrinsic.Create("_debugLog");
			f.AddParam("s");
			f.code = (context, partialResult) => {
				string s = context.variables.GetString("s");
				ModEntry.instance.Monitor.Log(s);
				return Intrinsic.Result.Null;
			};

			f = Intrinsic.Create("_lerpColor");
			f.AddParam("colorA", "#FFFFFF");
			f.AddParam("colorB", "#FFFFFF");
			f.AddParam("t", 0.5f);
			f.code = (context, partialResult) => {
				var colorA = ColorUtils.ToColor(context.GetLocalString("colorA"));
				var colorB = ColorUtils.ToColor(context.GetLocalString("colorB"));
				var t = context.GetLocalFloat("t");
				var resultColor = ColorUtils.Lerp(colorA, colorB, t);
				return new Intrinsic.Result(new ValString(resultColor.ToHexString()));
			};

			f = Intrinsic.Create("bot");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				if (sh.bot == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(BotModule());
			};

			f = Intrinsic.Create("env");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(sh.env);
			};

			f = Intrinsic.Create("exit");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				sh.Exit();
				sh.console.keyBuffer.Clear();
				return Intrinsic.Result.Null;
			};


			f = Intrinsic.Create("farm");
			f.code = (context, partialResult) => {
				var loc = (Farm)Game1.getLocationFromName("Farm");
				return new Intrinsic.Result(LocationSubclass(loc));
			};
			
			f = Intrinsic.Create("file");
			f.code = (context, partialResult) => {
				return new Intrinsic.Result(FileModule());
			};
			// Note: FileModule() defines a few intrinsics (like cd) which are also accessed
			// globally, so we better call in now to make sure those are defined right away:
			FileModule();

			f = Intrinsic.Create("FileHandle");
			f.code = (context, partialResult) => {
				return new Intrinsic.Result(FileHandleClass());
			};

			f = Intrinsic.Create("getLocation");
			f.AddParam("name");
			f.code = (context, partialResult) => {
				string name = context.GetLocalString("name");
				if (string.IsNullOrEmpty(name)) return Intrinsic.Result.Null;
				var loc = Game1.getLocationFromName(name);
				if (loc == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(LocationSubclass(loc));
			};

			f = Intrinsic.Create("locations");
			f.code = (context, partialResult) => {
				if (locationsMap == null) {
					locationsMap = new ValMap();
					foreach (var loc in Game1.locations) {
						locationsMap[loc.Name] = LocationSubclass(loc);
					}
				}
				return new Intrinsic.Result(locationsMap);
			};

			f = Intrinsic.Create("import");
			f.AddParam("libname");
			f.code = (context, partialResult) => {
				if (partialResult != null) {
					// When we're invoked with a partial result, it means that the import
					// function has finished, and stored its result (the values that were
					// created by the import code) in Temp 0.
					ValMap importedValues = context.GetTemp(0) as ValMap;
					// Now we're going to do something slightly evil.  We're going to reach
					// up into the *parent* context, and store these imported values under
					// the import library name.  Thus, there will always be a standard name
					// by which you can refer to the imported stuff.
					TAC.Context callerContext = context.parent;
					callerContext.SetVar(partialResult.result.ToString(), importedValues);
					return Intrinsic.Result.Null;
				}
				// When we're invoked without a partial result, it's time to start the import.
				// Begin by finding the actual code.
				Shell sh = context.interpreter.hostData as Shell;
				Value libnameVal = context.GetVar("libname");
				string libname = libnameVal == null ? null : libnameVal.ToString();
				if (string.IsNullOrEmpty(libname)) {
					throw new RuntimeException("import: libname required");
				}
			
				// Figure out what directories to look for import modules in.
				string[] libDirs = new string[0];
				Value importPaths = sh.GetEnv("importPaths");
				if (importPaths is ValList) {
					var iplist = importPaths as ValList;
					libDirs = new string[iplist.values.Count];
					for (int i=0; i<iplist.values.Count; i++) {
						libDirs[i] = iplist.values[i].ToString();
					}
				} else if (importPaths != null) {
					// Not a list?  Assume a semicolon-delimited string.
					libDirs = importPaths.ToString().Split(new char[] {';'});
				}

				//ModEntry.instance.Monitor.Log("Got " + libDirs.Length + " lib dirs: " + string.Join(", ", libDirs));
				List<string> lines = null;
				foreach (string dir in libDirs) {
					string path = dir;
					if (path.Length == 0 || path[path.Length-1] != '/') path += "/";
					path += libname + ".ms";
					string err;
					path = sh.ResolvePath(path, out err);
					Disk disk = FileUtils.GetDisk(ref path);
					if (disk == null) continue;
					lines = disk.ReadLines(path);
					if (lines != null) break;
				}
				if (lines == null) throw new RuntimeException("import: library not found: " + libname);
			
				// Now, parse that code, and build a function around it that returns
				// its own locals as its result.  Push a manual call.
				var parser = new Parser();
				parser.errorContext = libname + ".ms";
				parser.Parse(string.Join("\n", lines.ToArray()));
				Function import = parser.CreateImport();
				sh.interpreter.vm.ManuallyPushCall(new ValFunction(import), new ValTemp(0));
				// That call will not be able to run until we return from this intrinsic.
				// So, return a partial result, with the lib name.  We'll get invoked
				// again after the import function has finished running.
				return new Intrinsic.Result(new ValString(libname), false);
			};
				
			f = Intrinsic.Create("input");
			f.AddParam("prompt");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				if (sh.inputReceived != null) {
					string s = sh.inputReceived;
					sh.inputReceived = null;
					return new Intrinsic.Result(s);
				}
				Console con = sh.console;
				if (partialResult == null) {
					Value prompt = context.GetVar("prompt");
					if (prompt != null) sh.textDisplay.Print(prompt.ToString());
					//con.autocompCallback = null;
					con.StartInput();
				}
				return new Intrinsic.Result(ValNumber.one, false);	// not done yet; but non-null partialResult means we've started!
			};
		
			f = Intrinsic.Create("key");
			f.code = (context, partialResult) => {
				return new Intrinsic.Result(KeyModule());
			};

			f = Intrinsic.Create("Location");
			f.AddParam("name");
			f.code = (context, partialResult) => {
				Value name = context.GetVar("name");

				GameLocation loc;
				if (name != null) {
					loc = Game1.getLocationFromName(name.ToString());
				} else {
					Shell sh = context.interpreter.hostData as Shell;
					var bot = sh.bot;
					if (bot == null) {
						loc = Game1.currentLocation;
					} else {
						loc = sh.bot.currentLocation;
					}
				}
				if (loc == null) {
					return Intrinsic.Result.Null;
				}
				return new Intrinsic.Result(LocationClass());
			};

			f = Intrinsic.Create("run");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				sh.Break(true);
				sh.runProgram = true;
				context.vm.yielding = true;

				string sourcePath = "";
				var sourceFile = context.GetVar("_sourceFile");
				if (sourceFile != null) sourcePath = sourceFile.ToString();
				ToDoManager.NoteRun(sourcePath);

				return Intrinsic.Result.Null;
			};

			f = Intrinsic.Create("text");
			f.code = (context, partialResult) => {
				return new Intrinsic.Result(TextModule());
			};

			f = Intrinsic.Create("_taskDone");
			f.AddParam("taskNum");
			f.code = (context, partialResult) => {
				int taskNum = context.GetLocalInt("taskNum");
				if (taskNum < 0 || taskNum >= (int)Task.kQtyTasks) return Intrinsic.Result.Null;
				var task = (Task)taskNum;
				return new Intrinsic.Result(ValNumber.Truth(ToDoManager.IsTaskDone(task)));
			};

			f = Intrinsic.Create("_sendMail");
			f.code = (context, partialResult) => {
				ToDoManager.SendFirstBotMail();
				return Intrinsic.Result.Null;
			};

			// The world intrinsic gives access to information about the world of SDV.
			// Currently mostly the things related to time.
			f = Intrinsic.Create("world");
			f.code = (context, partialResult) => {
				return new Intrinsic.Result(WorldInfo());
			};
		}

		static bool DisallowAllAssignment(Value key, Value value) {
			throw new RuntimeException("Assignment to protected map");
		}

		static ValMap botModule;
		static HashSet<string> botProtectedKeys;
		public static ValMap BotModule() {
			if (botModule != null) return botModule;

			botModule = new ValMap();

			Intrinsic f;

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(new ValString(sh.bot.BotName));
			};
			botModule["name"] = f.GetFunc();

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
				return new Intrinsic.Result(new ValNumber(sh.bot.energy));
			};
			botModule["energy"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(new ValString(sh.bot.statusColor.ToHexString()));
			};
			botModule["statusColor"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				// Move the bot
				sh.bot.MoveForward();
				return Intrinsic.Result.Null;
			};
			botModule["forward"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				ValList result = new ValList();
				if (sh.bot.inventory != null) {
					foreach (var item in sh.bot.inventory) {
						result.values.Add(TileInfo.ToMap(item));
					}
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
				var result = new ValMap();
				result["x"] = new ValNumber(pos.X);
				result["y"] = new ValNumber(pos.Y);
				result["area"] = LocationSubclass(loc);
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
			// ToDo: accept a count of items to place.
			// For now, we'll just always place as many as possible.
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				int itemsPlaced = sh.bot.PlaceItem();
				return new Intrinsic.Result(itemsPlaced);
			};
			botModule["placeItem"] = f.GetFunc();

			f = Intrinsic.Create("");
			f.AddParam("slot", 0);
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				bool result = sh.bot.TakeItem(context.GetLocalInt("slot"));
				return result ? Intrinsic.Result.True : Intrinsic.Result.False;
			};
			botModule["takeItem"] = f.GetFunc();

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

			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;

				bool result = sh.bot.Harvest();
				return result ? Intrinsic.Result.True : Intrinsic.Result.False;
			};
			botModule["harvest"] = f.GetFunc();

			botProtectedKeys = new HashSet<string>();
			foreach (Value key in botModule.Keys) {
				botProtectedKeys.Add(key.ToString());
			}

			botModule.assignOverride = (key,value) => {
				string keyStr = key.ToString();
				if (keyStr == "_") return false;
				//ModEntry.instance.Monitor.Log($"botModule {key} = {value}");
				if (keyStr == "name") {
					string name = value.ToString();
					if (!string.IsNullOrEmpty(name)) Shell.runningInstance.bot.BotName = name;
					return true;
				} else if (keyStr == "statusColor") {
					Shell.runningInstance.bot.statusColor = value.ToString().ToColor();
					return true;
				} else if (keyStr == "currentToolIndex") {
					Shell.runningInstance.bot.currentToolIndex = value.IntValue();
					return true;
				} else if (botProtectedKeys.Contains(keyStr)) return true;
				return false;	// allow the assignment
			};

			return botModule;
		}

		static ValMap fileModule;
		static ValMap FileModule() {
			if (fileModule != null) return fileModule;
			fileModule = new ValMap();
			fileModule.assignOverride = DisallowAllAssignment;
		
			// File.curdir (also goes by "pwd")
			Intrinsic f = Intrinsic.Create("pwd");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				return new Intrinsic.Result(sh.env["curdir"]);
			};
			fileModule["curdir"] = f.GetFunc();
		
			// File.setdir (also goes by "cd")
			f = Intrinsic.Create("cd");
			f.AddParam("dirPath", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetVar("dirPath").ToString();
				if (string.IsNullOrEmpty(path)) path = sh.GetEnv("home").ToString();
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);

				ToDoManager.NoteCd(path);

				sh.env["curdir"] = new ValString(path);
				return Intrinsic.Result.Null;
			};
			fileModule["setdir"] = f.GetFunc();

		
			// File.makedir
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");

				string err = null;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);

				Disk disk = FileUtils.GetDisk(ref path);
				if (!disk.IsWriteable()) return new Intrinsic.Result("Error: disk is not writeable");
				if (!path.EndsWith("/")) path += "/";
				if (disk.Exists(path)) return new Intrinsic.Result("Error: file already exists");
				disk.MakeDir(path, out err);
				if (err == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(err);
			};
			fileModule["makedir"] = f.GetFunc();
		
			// File.children
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				ModEntry.instance.Monitor.Log("File.children: path=[" + path + "]");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				if (path == "/") {
					// Special case: listing the disks.
					var disks = new List<Value>();
					var diskNames = new List<string>(FileUtils.disks.Keys);
					diskNames.Sort();
					foreach (string name in diskNames) {
						disks.Add(new ValString("/" + name));
					}
					return new Intrinsic.Result(new ValList(disks));
				}
				Disk disk = FileUtils.GetDisk(ref path);
				if (disk == null) return Intrinsic.Result.Null;
				Value result = disk.GetFileNames(path).ToValue();
				return new Intrinsic.Result(result);
			};
			fileModule["children"] = f.GetFunc();
		
			// File.name
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				return new Intrinsic.Result(FileUtils.GetFileName(path));
			};
			fileModule["name"] = f.GetFunc();

			// File.parent
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				int pos = path.LastIndexOf("/");
				if (pos == 0) return new Intrinsic.Result("/");
				if (pos < 0) return Intrinsic.Result.Null;
				return new Intrinsic.Result(path.Substring(0, pos));
			};
			fileModule["parent"] = f.GetFunc();

			// File.exists
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				if (FileUtils.Exists(path)) return Intrinsic.Result.True;
				return Intrinsic.Result.False;
			};
			fileModule["exists"] = f.GetFunc();

			// File.info
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				FileInfo info = FileUtils.GetInfo(path);
				if (info == null) return Intrinsic.Result.Null;
				var result = new ValMap();
				result["path"] = new ValString(path);
				result["isDirectory"] = ValNumber.Truth(info.isDirectory);
				result["size"] = new ValNumber(info.size);
				result["date"] = new ValString(info.date);
				result["comment"] = new ValString(info.comment);
				return new Intrinsic.Result(result);
			};
			fileModule["info"] = f.GetFunc();

			// File.child
			f = Intrinsic.Create("");
			f.AddParam("basePath", "");
			f.AddParam("subpath", "");
			f.code = (context, partialResult) => {
				string basePath = context.GetLocalString("basePath");
				string subpath = context.GetLocalString("subpath");
				return new Intrinsic.Result(FileUtils.PathCombine(basePath, subpath));
			};
			fileModule["child"] = f.GetFunc();

			// File.delete (also known as global delete)
			f = Intrinsic.Create("delete");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				err = FileUtils.Delete(path);
				if (err == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(err);
			};
			fileModule["delete"] = f.GetFunc();

			// File.move
			f = Intrinsic.Create("");
			f.AddParam("oldPath", "");
			f.AddParam("newPath", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string err;
				string oldPath = context.GetLocalString("oldPath");
				oldPath = sh.ResolvePath(oldPath, out err);
				if (oldPath == null) return new Intrinsic.Result(err);
			
				string newPath = context.GetLocalString("newPath");
				newPath = sh.ResolvePath(newPath, out err);
				if (newPath == null) return new Intrinsic.Result(err);
			
				err = FileUtils.MoveOrCopy(oldPath, newPath, true, false);
				if (err == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(err);
			};
			fileModule["move"] = f.GetFunc();

			// File.copy
			f = Intrinsic.Create("");
			f.AddParam("oldPath", "");
			f.AddParam("newPath", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string err;
				string oldPath = context.GetLocalString("oldPath");
				oldPath = sh.ResolvePath(oldPath, out err);
				if (oldPath == null) return new Intrinsic.Result(err);
			
				string newPath = context.GetLocalString("newPath");
				newPath = sh.ResolvePath(newPath, out err);
				if (newPath == null) return new Intrinsic.Result(err);
			
				err = FileUtils.MoveOrCopy(oldPath, newPath, false, false);
				if (err == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(err);
			};
			fileModule["copy"] = f.GetFunc();
		
			// File.open
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.AddParam("mode", "rw+");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string mode = context.GetLocalString("mode").ToLower();
				if (mode.Contains("b")) return new Intrinsic.Result("Error: binary mode not supported");
				string err = null;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				if ((mode == "r" || mode == "r+") && !FileUtils.Exists(path)) return new Intrinsic.Result("Error: file not found");
				var file = new OpenFile(path, mode);
				ValMap result = new ValMap();
				result.SetElem(ValString.magicIsA, FileHandleClass());
				result.map[_handle] = new ValWrapper(file);
				result.assignOverride = (key, value) => {
					if (key.ToString() == "position") file.position = value.IntValue();
					return true;
				};
				return new Intrinsic.Result(result);
			};
			fileModule["open"] = f.GetFunc();
		
			// File.readLines
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err = null;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				Disk disk = FileUtils.GetDisk(ref path);
				if (disk == null) return Intrinsic.Result.Null;
				Value result = disk.ReadLines(path).ToValue();
				return new Intrinsic.Result(result);
			};
			fileModule["readLines"] = f.GetFunc();
		
			// File.writeLines
			f = Intrinsic.Create("");
			f.AddParam("path");
			f.AddParam("lines");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string origPath = context.GetLocalString("path");
				Value linesVal = context.GetLocal("lines");
				if (linesVal == null) return new Intrinsic.Result("Error: lines parameter is required");
			
				string err = null;
				string path = sh.ResolvePath(origPath, out err);
				if (path == null) return new Intrinsic.Result(err);

				try {
					Disk disk = FileUtils.GetDisk(ref path);
					if (!disk.IsWriteable()) return new Intrinsic.Result("Error: disk is not writeable");
					disk.WriteLines(path, linesVal.ToStrings());
				} catch (System.Exception) {
					return new Intrinsic.Result("Error: unable to write " + origPath);
				}
				return Intrinsic.Result.Null;
			};
			fileModule["writeLines"] = f.GetFunc();
/*		
			// File.loadImage
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				Disk disk = FileUtils.GetDisk(ref path);
				if (disk == null) return Intrinsic.Result.Null;
				byte[] data = disk.ReadBinary(path);
				if (data == null) return Intrinsic.Result.Null;
			
				Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				if (!ImageConversion.LoadImage(tex, data, false)) return Intrinsic.Result.Null;
				//ModEntry.instance.Monitor.Log("LoadImage returned true.  And size " + tex.width + " x " + tex.height);
				tex.anisoLevel = 1;
				tex.filterMode = FilterMode.Point;
				tex.wrapMode = TextureWrapMode.Clamp;
				return new Intrinsic.Result(TextureToImage(tex));
			};
			fileModule["loadImage"] = f.GetFunc();

			// File.saveImage
			f = Intrinsic.Create("");
			f.AddParam("path");
			f.AddParam("image");
			f.AddParam("quality", 80);
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				ValMap imageVal = context.GetLocal("image") as ValMap;
				if (imageVal == null) return new Intrinsic.Result("Error: image parameter is required");
				ValWrapper imgv = imageVal["_handle"] as ValWrapper;
				if (imgv == null || !(imgv.content is ImageHandle)) return Intrinsic.Result.Null;
				var tex = (ImageHandle)imgv.content;
			
				string err = null;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);

				Disk disk = FileUtils.GetDisk(ref path);
				if (!disk.IsWriteable()) return new Intrinsic.Result("Error: disk is not writeable");

				byte[] bytes = null;
				if (path.EndsWith(".jpg") || path.EndsWith(".jpeg")) bytes = tex.texture2D.EncodeToJPG(context.variables.GetInt("quality",80));
				else if (path.EndsWith(".tga")) bytes = tex.texture2D.EncodeToTGA();
				else bytes = tex.texture2D.EncodeToPNG();
				if (bytes == null) return new Intrinsic.Result("Error: unable to encode image");
			
				disk.WriteBinary(path, bytes);
				return Intrinsic.Result.Null;
			};
			fileModule["saveImage"] = f.GetFunc();
		
			// File.loadSound
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				if (partialResult != null && partialResult.result is ValWrapper) {
					return ContinueAsyncSoundLoader(partialResult);
				}
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				Disk disk = FileUtils.GetDisk(ref path);
				if (disk == null) return Intrinsic.Result.Null;
				byte[] data = disk.ReadBinary(path);
				if (data == null) return Intrinsic.Result.Null;
			
				return ReturnAsyncSoundLoader(data, FileUtils.GetFileName(path));
			};
			fileModule["loadSound"] = f.GetFunc();

			// File.loadRaw
			f = Intrinsic.Create("");
			f.AddParam("path", "");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				string err;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);
				Disk disk = FileUtils.GetDisk(ref path);
				if (disk == null) return Intrinsic.Result.Null;
				byte[] data = disk.ReadBinary(path);
				if (data == null) return Intrinsic.Result.Null;
			
				ValMap rawDataInst = new ValMap();
				rawDataInst.SetElem(ValString.magicIsA, RawDataClass());
				rawDataInst.map[_handle] = new ValWrapper(new BinaryData(data));
				return new Intrinsic.Result(rawDataInst);
			};
			fileModule["loadRaw"] = f.GetFunc();

			// File.saveRaw
			f = Intrinsic.Create("");
			f.AddParam("path");
			f.AddParam("rawData");
			f.code = (context, partialResult) => {
				Shell sh = context.interpreter.hostData as Shell;
				string path = context.GetLocalString("path");
				ValMap rawDataVal = context.GetLocal("rawData") as ValMap;
				ValWrapper handle = null;
				if (rawDataVal != null) handle = rawDataVal["_handle"] as ValWrapper;
				BinaryData bd = null;
				if (handle != null) bd = handle.content as BinaryData;
				if (bd == null) return new Intrinsic.Result("Error: RawData parameter is required");
			
				string err = null;
				path = sh.ResolvePath(path, out err);
				if (path == null) return new Intrinsic.Result(err);

				Disk disk = FileUtils.GetDisk(ref path);
				if (!disk.IsWriteable()) return new Intrinsic.Result("Error: disk is not writeable");

				disk.WriteBinary(path, bd.bytes);
				return Intrinsic.Result.Null;
			};
			fileModule["saveRaw"] = f.GetFunc();
*/		
			return fileModule;
		}
		
		static ValMap fileHandleClass;
		public static ValMap FileHandleClass() {
			if (fileHandleClass != null) return fileHandleClass;

			fileHandleClass = new ValMap();
			fileHandleClass.map[_handle] = null;		// (wraps an OpenFile object)

			// .isOpen
			var f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (err != null) return new Intrinsic.Result(err);
				if (file == null || !file.isOpen) return Intrinsic.Result.False;
				return Intrinsic.Result.True;
			};		
			fileHandleClass["isOpen"] = f.GetFunc();
		
			// .position
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (file == null) return Intrinsic.Result.False;
				return new Intrinsic.Result(file.position);
			};		
			fileHandleClass["position"] = f.GetFunc();
		
			// .atEnd
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (file == null || !file.isAtEnd) return Intrinsic.Result.False;
				return Intrinsic.Result.True;
			};		
			fileHandleClass["atEnd"] = f.GetFunc();
		
			// .write
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("s", "");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (err != null) return new Intrinsic.Result(err);
				string s = context.GetLocalString("s");
				file.Write(s);
				if (file.error == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(file.error);
			};		
			fileHandleClass["write"] = f.GetFunc();
		
			// .writeLine
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("s", "");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (err != null) return new Intrinsic.Result(err);
				string s = context.GetLocalString("s");
				file.Write(s);
				file.Write("\n");
				if (file.error == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(file.error);
			};		
			fileHandleClass["writeLine"] = f.GetFunc();
		
			// .read
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("codePointCount");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (err != null) return Intrinsic.Result.Null;
				string s;
				Value count = context.GetLocal("codePointCount");
				if (count == null) s = file.ReadToEnd();
				else s = file.ReadChars(count.IntValue());
				return new Intrinsic.Result(s);
			};		
			fileHandleClass["read"] = f.GetFunc();
		
			// .readLine
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (err != null) return Intrinsic.Result.Null;
				string s = file.ReadLine();
				return new Intrinsic.Result(s);
			};		
			fileHandleClass["readLine"] = f.GetFunc();
		
			// .close
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.code = (context, partialResult) => {
				string err;
				OpenFile file = GetOpenFile(context, out err);
				if (err != null) return new Intrinsic.Result(err);
				if (file != null) file.Close();
				return Intrinsic.Result.Null;
			};		
			fileHandleClass["close"] = f.GetFunc();
				
			fileHandleClass.assignOverride = DisallowAllAssignment;
			return fileHandleClass;
		}
	
		/// <summary>
		///  Helper method to find the OpenFile referred to by a method on
		/// a FileHandle object.  Returns the object, or null and sets error.
		/// </summary>
		static OpenFile GetOpenFile(TAC.Context context, out string err) {
			err = null;
			ValMap self = context.GetVar("self") as ValMap;
			Value handle;
			self.TryGetValue("_handle", out handle);
			if (!(handle is ValWrapper)) {
				err = "Error: file handle invalid";
				return null;
			}
			var result = (handle as ValWrapper).content as OpenFile;
			if (result == null) err = "Error: file handle not set";
			return result;
		}
	


		static ValList keyNames = null;
		static ValMap keyModule;
		static Dictionary<string, SButton> keyNameMap;
		static ValMap KeyModule() {
			if (keyModule != null) return keyModule;
			keyModule = new ValMap();
			keyModule.assignOverride = DisallowAllAssignment;
	
			Intrinsic f;		

			// key.available
			//	Returns whether there is a keypress available in the input buffer.
			//	If true, you can call key.get to get the next key immediately.
			//	Note that does not detect modifier keys (shift, alt, etc.).
			// Example: while not key.available; end while  // waits until some key is pressed
			// See also: key.clear; key.get
			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				return Shell.runningInstance.console.keyBuffer.Count > 0 ? Intrinsic.Result.True : Intrinsic.Result.False;
			};
			keyModule["available"] = f.GetFunc();
		
			// key.clear
			//	Clear the keyboard input buffer.  This is often used before exiting
			//	a game, so that any key presses made during the game don't spill out
			//	into the command line.
			// Example: key.clear
			// See also: key.available; key.get
			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				Shell.runningInstance.console.keyBuffer.Clear();
				return Intrinsic.Result.Null;
			};
			keyModule["clear"] = f.GetFunc();
		
			// key.get
			//	Remove and return the next key in the keyboard input buffer.  If the
			//	input buffer is currently clear (empty), then this method waits until
			//	a key is pressed.  Note that modifier keys (shift, alt, etc.) pressed
			//	alone do not go into the input buffer.
			// Example: print "You pressed: " + key.get
			// See also: key.available; key.get
			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				if (Shell.runningInstance.console.keyBuffer.Count == 0) return Intrinsic.Result.Waiting;
				string key = Shell.runningInstance.console.keyBuffer.Dequeue().ToString();
				return new Intrinsic.Result(key);
			};
			keyModule["get"] = f.GetFunc();

			// key.pressed
			//	Detect whether a specific key or button input is currently pressed.  
			//	These include modifier keys (e.g. "left shift", "right alt") as 
			//	well as mouse buttons (e.g. "mouse 0") and joystick/gamepad buttons
			//	("joystick 1 button 0", etc.).  With regard to joystick buttons, 
			//	if you don't specify a number (e.g. "joystick button 0"), then
			//	it will detect a press of button 0 on *any* joystick.
			//	See key.keyNames for all the possible names to use with this method.
			// keyName (string, default "space"): key/button to press
			// Example: while not key.pressed("left"); end while   // waits until left arrow pressed
			// See also: key.keyNames; key.axis
			f = Intrinsic.Create("");
			f.AddParam("keyName", "space");
			f.code = (context, partialResult) => {
				string keyName = context.GetLocalString("keyName");
				SButton button = SButton.None;
				if (keyNameMap == null) {
					// Note: in SMAPI, use test_input to check key codes
					keyNameMap = new Dictionary<string, SButton>();
					keyNameMap["left ctrl"] = SButton.LeftControl;
					keyNameMap["right ctrl"] = SButton.RightControl;
					keyNameMap["left alt"] = SButton.LeftAlt;
					keyNameMap["right alt"] = SButton.RightAlt;
					keyNameMap["left shift"] = SButton.LeftShift;
					keyNameMap["right shift"] = SButton.RightShift;
					keyNameMap["up"] = SButton.Up;
					keyNameMap["down"] = SButton.Down;
					keyNameMap["left"] = SButton.Left;
					keyNameMap["right"] = SButton.Right;
					for (int num=0; num<=9; num++) {
						keyNameMap[num.ToString(CultureInfo.InvariantCulture)] = SButton.D0 + num;
						keyNameMap["[" + num + "]"] = SButton.NumPad0 + num;
					}
					for (char c='a'; c<='z'; c++) {
						keyNameMap[c.ToString()] = SButton.A + (c - 'a');
					}
					for (int f=1; f<=15; f++) {
						keyNameMap["f" + f] = SButton.F1 + (f - 1);
					}
					keyNameMap["-"] = SButton.OemMinus;
					keyNameMap["="] = SButton.OemPlus;
					keyNameMap["["] = SButton.OemOpenBrackets;
					keyNameMap["]"] = SButton.OemCloseBrackets;
					keyNameMap["\\"] = SButton.OemPipe;
					keyNameMap[","] = SButton.OemComma;
					keyNameMap["."] = SButton.OemPeriod;
					keyNameMap["/"] = SButton.OemQuestion;
					keyNameMap[";"] = SButton.OemSemicolon;
					keyNameMap["'"] = SButton.OemQuotes;
					keyNameMap["`"] = SButton.OemTilde;
					keyNameMap["[+]"] = SButton.Add;
					keyNameMap["[-]"] = SButton.Subtract;
					keyNameMap["[*]"] = SButton.Multiply;
					keyNameMap["[/]"] = SButton.Divide;
					keyNameMap["equals"] = SButton.Execute;
					keyNameMap["clear"] = SButton.OemClear;
					keyNameMap["backspace"] = SButton.Back;
					keyNameMap["tab"] = SButton.Tab;
					keyNameMap["return"] = SButton.Enter;
					keyNameMap["enter"] = SButton.Enter;
					keyNameMap["escape"] = SButton.Escape;
					keyNameMap["space"] = SButton.Space;
					keyNameMap["delete"] = SButton.Delete;
					keyNameMap["insert"] = SButton.Insert;
					keyNameMap["home"] = SButton.Home;
					keyNameMap["end"] = SButton.End;
					keyNameMap["page up"] = SButton.PageUp;
					keyNameMap["page down"] = SButton.PageDown;
					keyNameMap["mouse 0"] = SButton.MouseLeft;
					keyNameMap["mouse 1"] = SButton.MouseRight;
					keyNameMap["mouse 2"] = SButton.MouseMiddle;
					keyNameMap["mouse 3"] = SButton.MouseX1;
					keyNameMap["mouse 4"] = SButton.MouseX2;
				}
				keyNameMap.TryGetValue(keyName, out button);
				if (button == SButton.None) throw new RuntimeException($"Invalid key name: {keyName}");
				bool result = ModEntry.instance.Helper.Input.IsDown(button);
				return result ? Intrinsic.Result.True : Intrinsic.Result.False;
			};
			keyModule["pressed"] = f.GetFunc();
		
			// key.axis
			//	Return the numeric value (from -1 to 1) of an input axis.  Available
			//	axis names are "Horizontal" and "Vertical", which can be activated
			//	by both WASD and arrow keys as well as any joystick or gamepad;
			//	"JoyAxis1" through "JoyAxis29" which detect axis inputs from any
			//	joystick or gamepad, and "Joy1Axis1" through "Joy8Axis29" which detect
			//	axis inputs from specific joystick/gamepad 1 through 8.
			// axisName (string, default "Horizontal"): name of axis to get
			// Example: print key.axis("Vertical")
			// See also: key.pressed
/*			f = Intrinsic.Create("");
			f.AddParam("axisName", "Horizontal");
			f.code = (context, partialResult) => {
				string axisName = context.GetLocalString("axisName");
				try {
					return new Intrinsic.Result(Input.GetAxis(axisName));
				} catch (System.ArgumentException e) {
					//ModEntry.instance.Monitor.Log("Invalid axis name: " + axisName);
					return Intrinsic.Result.Null;
				}
			};
			keyModule["axis"] = f.GetFunc();
*/		
			// key.keyNames
			//	Returns a list of all the key names available for use with key.pressed.
			//	This can be used, for example, to check all possible inputs, if waiting
			//	for the user to press anything to continue, or while configuring their
			//	input preferences.
			// Example:
			//		while true
			//			for n in key.keyNames
			//				if key.pressed(n) then print n
			//			end for
			//		end while
			// See also: key.pressed
			f = Intrinsic.Create("");
			f.code = (context, partialResult) => {
				if (keyNames == null) {
					keyNames = new ValList();
					string keys = "a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|y|z|"
						+ "1|2|3|4|5|6|7|8|9|0|-|=|[|]|\\|,|.|/|;|'|`|"
						+ "f1|f2|f3|f4|f5|f6|f7|f8|f9|f10|f11|f12|f13|f14|f15|"
						+ "up|down|left|right|"
						+ "[1]|[2]|[3]|[4]|[5]|[6]|[7]|[8]|[9]|[0]|[+]|[-]|[*]|[/]|enter|equals|clear|" // note: clear may not actually work
						+ "left shift|right shift|left ctrl|right ctrl|left alt|right alt|left cmd|right cmd|"
						+ "backspace|tab|return|escape|space|delete|insert|home|end|page up|page down|"
						+ "mouse 0|mouse 1|mouse 2|mouse 3|mouse 4|mouse 5|mouse 6";
					foreach (string s in keys.Split(new char[]{'|'})) {
						keyNames.values.Add(new ValString(s));
					}
					for (int j=0; j<5; j++) {
						string jname = "joystick";
						if (j > 0) jname += " " + j;
						for (int i=0; i<16; i++) {
							keyNames.values.Add(new ValString(jname + " button " + i));
						}
					}
			
				}
				return new Intrinsic.Result(keyNames);
			};
			keyModule["keyNames"] = f.GetFunc();

			return keyModule;
		}

		static ValMap locationClass = null;
		public static ValMap LocationClass() {
			if (locationClass != null) return locationClass;
			locationClass = new ValMap();
			locationClass.map[_name] = new ValString("Location");
			locationClass.map[new ValString("width")] = ValNumber.zero;
			locationClass.map[new ValString("height")] = ValNumber.zero;

			Intrinsic f;

			// Location.tile (gets info on a particular tile in this location)
			f = Intrinsic.Create("");
			f.AddParam("self");
			f.AddParam("x", ValNumber.zero);
			f.AddParam("y", ValNumber.zero);
			f.code = (context, partialResult) => {
				ValMap self = context.GetVar("self") as ValMap;
				int x = context.GetLocalInt("x", 0);
				int y = context.GetLocalInt("y", 0);
				Vector2 xy = new Vector2(x,y);
				string name = self.Lookup(_name).ToString();
				var loc = Game1.getLocationFromName(name);
				if (loc == null) return Intrinsic.Result.Null;

				ValMap result = TileInfo.GetInfo(loc, xy);
				if (result == null) return Intrinsic.Result.Null;
				return new Intrinsic.Result(result);
			};
			locationClass["tile"] = f.GetFunc();

			return locationClass;
		}

		public static Dictionary<string, ValMap> locationCache = new Dictionary<string, ValMap>();
		public static ValMap LocationSubclass(GameLocation loc) {
			if (locationCache.ContainsKey(loc.NameOrUniqueName)) {
				return locationCache[loc.NameOrUniqueName];
			}
			var subclass = new ValMap();
			subclass.map[ValString.magicIsA] = LocationClass();
			subclass.map[_name] = new ValString(loc.NameOrUniqueName);
			subclass.map[new ValString("width")] = new ValNumber(loc.map.Layers[0].LayerWidth);
			subclass.map[new ValString("height")] = new ValNumber(loc.map.Layers[0].LayerHeight);
			locationCache.Add(loc.NameOrUniqueName, subclass);

			return subclass;
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
				Cell cell = ReferencedCell(context);
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
				Cell cell = ReferencedCell(context);
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
				Cell cell = ReferencedCell(context);
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



		static ValMap worldInfo;

		/// <summary>
		/// Creates a module for accessing data about the SDV world.
		/// </summary>
		/// <returns>A value map with functions to get information about the world.</returns>
		static ValMap WorldInfo() {
			if (worldInfo == null) {
				worldInfo = new ValMap();
				worldInfo.assignOverride = DisallowAllAssignment;

				Intrinsic f;
				f = Intrinsic.Create("");
				f.code = (context, partialResult) => {
					var messages = ModEntry.instance.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages").GetValue();
					var result = new ValList();
					foreach (ChatMessage msg in messages) {
						string msgText = ChatMessage.makeMessagePlaintext(msg.message, false);
						result.values.Add(new ValString(msgText));
                    }
					return new Intrinsic.Result(result);
                };
				worldInfo["chatMessages"] = f.GetFunc();

				f = Intrinsic.Create("");
				f.AddParam("message", "");
				f.code = (context, partialResult) => {
					string msg = context.variables.GetString("message");
					Shell sh = context.interpreter.hostData as Shell;
					string name;
					if (sh.bot == null) name = "Home Computer";
					else name = sh.bot.name;
					TextDisplay disp = sh.textDisplay;
					Game1.chatBox.addMessage(name + ": " + msg, disp.textColor);
					return Intrinsic.Result.Null;
                };
				worldInfo["chat"] = f.GetFunc();
			}

			// The in-game time on this day.
			// Can exceed 2400 when the farmer refuses to sleep.
			worldInfo["timeOfDay"] = new ValNumber(Game1.timeOfDay);

			// Days since start is the amount of in-game days since this farm was started.
			// Day 1 of year 1 is 1 in this function.
			worldInfo["daySinceGameStart"] = new ValNumber(SDate.Now().DaysSinceStart);

			// The current day in the in-game season.
			worldInfo["dayOfSeason"] = new ValNumber(SDate.Now().Day);

			// The number of the in-game day of week (0 = Sunday).
			worldInfo["dayOfWeek"] = new ValNumber((int)SDate.Now().DayOfWeek);

			// The name of the in-game day.
			worldInfo["dayOfWeekName"] = new ValString(SDate.Now().DayOfWeek.ToString());

			// The in-game year, starts at 1.
			worldInfo["year"] = new ValNumber(SDate.Now().Year);

			// The numeric representation for the current in-game season (0 = spring).
			worldInfo["season"] = new ValNumber(SDate.Now().SeasonIndex);

			// The human-readable representation for the current in-game season.
			worldInfo["seasonName"] = new ValString(SDate.Now().Season);

			// The current weather
			{
				var loc = (Farm)Game1.getLocationFromName("Farm");
				var weather = Game1.netWorldState.Value.GetWeatherForLocation(loc.GetLocationContext());
				string result = "sunny";
				if (weather.isLightning.Value) result = "stormy";
				else if (weather.isRaining.Value) result = "raining";
				else if (weather.isSnowing.Value) result = "snowing";
				else if (weather.isDebrisWeather.Value) result = "windy";
				worldInfo["weather"] = new ValString(result);
			};

			// Daily luck
			worldInfo["luck"] = new ValNumber(Game1.player.DailyLuck);

			return worldInfo;
		}

		/// <summary>
		/// Helper method for various Text class methods that get
		/// a cell from the self, x, and y parameters in the context.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		static Cell ReferencedCell(TAC.Context context, out TextDisplay disp) {
			Shell sh = context.interpreter.hostData as Shell;
			disp = sh.textDisplay;
			int x = context.GetLocalInt("x");
			int y = context.GetLocalInt("y");
			return disp.Get(y, x);		
		}

		static Cell ReferencedCell(TAC.Context context) {
			TextDisplay disp;
			return ReferencedCell(context, out disp);
		}
	
		static ValList ToList(double a, double b) {
			var result = new ValList();
			result.values.Add(new ValNumber(a));
			result.values.Add(new ValNumber(b));
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

	
	public class ValWrapper : Value {
		public readonly object content;

		public ValWrapper(object content) {
			this.content = content;
			if (content is ValWrapperNotificationReceiver) {
				((ValWrapperNotificationReceiver)content).WrapperAdded(this);
			}
		}

		~ValWrapper() {
			if (content is ValWrapperNotificationReceiver) {
				((ValWrapperNotificationReceiver)content).WrapperRemoved(this);
			}
		}

		public override string ToString(TAC.Machine vm) {
			return content.ToString().Replace("UnityEngine.", "");
		}

		public override int Hash(int recursionDepth=16) {
			return content.GetHashCode();
		}

		public override double Equality(Value rhs, int recursionDepth=16) {
			return rhs is ValWrapper && ((ValWrapper)rhs).content == content ? 1 : 0;
		}
	}

	public interface ValWrapperNotificationReceiver {
		void WrapperAdded(ValWrapper wrapper);
		void WrapperRemoved(ValWrapper wrapper);
	}

}

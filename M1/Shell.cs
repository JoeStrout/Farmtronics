/*
This script represents the shell of the machine -- the command interpreter.
It connects a MiniScript interpreter to the Console which interacts with
the user. 
*/

using System.Collections.Generic;
using System.IO;
using StardewModdingAPI;
using Miniscript;
using Microsoft.Xna.Framework;
using Farmtronics.Bot;
using Farmtronics.Utils;

namespace Farmtronics {
	public class Shell {
		static Value _bootOpts = new ValString("bootOpts");
		static Value _controlC = new ValString("controlC");
		public Console console { get; private set; }
		public BotObject bot {  get; private set; }
		public bool allowControlCBreak {
			get {
				ValMap bootOpts = env.Lookup(_bootOpts) as ValMap;
				if (bootOpts == null) return true;
				Value v = bootOpts.Lookup(_controlC);
				return v == null || v.BoolValue();
			}
		}

		public static Shell runningInstance;

		public ValMap env;

		public Interpreter interpreter { get; private set; }
		public bool runProgram;
		public string inputReceived;		// stores input while app is running, for _input intrinsic

		Disk sysDisk;

		ValString curStatusColor;
		ValString curScreenColor;

		public TextDisplay textDisplay {  get {  return console.display; } }
		
		public Shell() {
			console = new Console(this);

			// prepare the interpreter
			interpreter = new Interpreter(null, PrintLineWithTaskCheck, PrintLine);
			interpreter.implicitOutput = PrintLine;
			interpreter.hostData = this;
		}

		public void Init(BotObject botContext=null) {
			this.bot = botContext;
			M1API.Init(this);

			var display = console.display;
			display.backColor = new Color(0.31f, 0.11f, 0.86f);
			display.Clear();

			var colors = new Color[] { Color.Red, Color.Yellow, Color.Green, Color.Purple };

			display.SetCursor(19, 3);
			for (int i=0; i<4; i++) {
				display.textColor = colors[i]; display.Print("*");
			}
			display.textColor = Color.Azure; display.Print(" Farmtronics " + (botContext==null ? "Home" : "Bot") + " Computer ");
			for (int i=0; i<4; i++) {
				display.textColor = colors[3-i]; display.Print("*");
			}
			display.textColor = Color.White;
			display.NextLine();

			if (interpreter.vm == null) {
				interpreter.REPL("", 0);	// (forces creation of a VM)
				AddGlobals();
			}

			{
				var d = new RealFileDisk();
				d.readOnly = true;
				d.Open(Path.Combine(ModEntry.instance.Helper.DirectoryPath, "assets", "sysdisk"));
				sysDisk = d;
				FileUtils.disks["sys"] = sysDisk;
			}
			if (!string.IsNullOrEmpty(Constants.CurrentSavePath)) {
				var d = new RealFileDisk();
				d.readOnly = false;
				d.Open(Path.Combine(Constants.CurrentSavePath, "usrdisk"));
				FileUtils.disks["usr"] = d;
			}

			// Prepare the env map
			env = new ValMap();
			if (FileUtils.disks.ContainsKey("usr") && FileUtils.disks["usr"] != null) {
				env["curdir"] = new ValString("/usr/");
			} else {
				env["curdir"] = new ValString("/sys/demo");
			}
			env["home"] = new ValString("/usr/");
			env["prompt"] = new ValString("]");
			env["morePrompt"] = new ValString("...]");

			// NOTE: importPaths is also in the reset function in startup.ms.
			// If you change this in either place, change it in both!
			var importPaths = new List<string>{".", "/usr/lib", "/sys/lib"};
			env["importPaths"] = importPaths.ToValue();		

			RunStartupScripts();
		}

		void FixHostInfo() {
			if (bot == null) HostInfo.name = "Farmtronics Home Computer";
			else HostInfo.name = "Farmtronics Bot";
        }

		void RunStartupScripts() {
			// load and run the startup script(s)
			runningInstance = this;

			// /sys/startup.ms

			string startupScript = sysDisk.ReadText("startup.ms");
			if (!string.IsNullOrEmpty(startupScript)) {
				try {
					FixHostInfo();
					interpreter.REPL(startupScript);
				} catch (System.Exception err) {
					ModEntry.instance.Monitor.Log("Error running /sys/startup.ms: " + err.ToString());
				}
			}

			// /usr/startup.ms
			if (FileUtils.disks.ContainsKey("usr") && FileUtils.disks["usr"] != null) {
				//ModEntry.instance.Monitor.Log("About to read startup.ms");
				startupScript = FileUtils.disks["usr"].ReadText("startup.ms");
				if (!string.IsNullOrEmpty(startupScript)) BeginRun(startupScript);
			}

			// Print friendly prompt, unless our startup script is still running.
			if (!interpreter.Running()) {
				PrintLine("");
				PrintLine("Ready!");
				PrintLine("");
			}
		}

		public void Update(GameTime gameTime) {
			var inp = ModEntry.instance.Helper.Input;

			if (interpreter == null) return;		// still loading
			runningInstance = this;
			if (interpreter.NeedMoreInput()) {
				GetCommand();		// (though in this case, this really means: get ANOTHER command!)
			} else if (interpreter.Running()) {
				// continue the running code
				interpreter.RunUntilDone(0.03f);
			} else if (runProgram) {
				//ModEntry.instance.Monitor.Log($"{bot.name} runProgram flag detected; starting new program");
				runProgram = false;
				interpreter.Stop();
				Value sourceVal = interpreter.GetGlobalValue("_source");
				string source = (sourceVal == null ? null : sourceVal.JoinToString());
				BeginRun(source);
			} else {
				// nothing running; get another command!
				GetCommand();
			}
		}

		void GetCommand() {
			if (console.InputInProgress()) return;		// already working on it!
			TextDisplay disp = console.display;
			if (disp.GetCursor().col != 0) disp.NextLine();
			string prompt = (interpreter.NeedMoreInput() ? "...]" : "]");
			Value promptVal;
			if (env.map.TryGetValue(new ValString(
						interpreter.NeedMoreInput() ? "morePrompt" : "prompt"), out promptVal)) {
				prompt = promptVal.ToString();
			}
			disp.Print(prompt);
			//autocompleter.machine = interpreter.vm;
			//console.autocompCallback = autocompleter.GetSuggestion;
			console.StartInput();
		}

		/// <summary>
		/// Given a (possibly partial) path, expand it to a full
		/// path from our current working directory, and resolve
		/// any . and .. entries in it to get a proper full path.
		/// If the path is invalid, return null and set error.
		/// </summary>
		public string ResolvePath(string path, out string error) {
			string curdir = GetEnv("curdir").ToString();
			return FileUtils.ResolvePath(curdir, path, out error);
		}
	
		public Value GetEnv(string key) {
			Value result = null;
			env.map.TryGetValue(new ValString(key), out result);
			return result;
		}
	
		public string GetEnvString(string key) {
			Value val = GetEnv(key);
			return val == null ? null : val.ToString();
		}

		public void HandleCommand(string command) {
			if (interpreter.Running() && !interpreter.NeedMoreInput()) {
				inputReceived = command;
				return;
			}
		
			command = command.Trim();
			string lcmd = command.ToLower();
		
			if (interpreter == null) ModEntry.instance.Monitor.Log("Error: Interpreter null?!?");

			runningInstance = this;
			FixHostInfo();
			interpreter.REPL(command, 0.1f);
		}
	
		void BeginRun(string source) {
			//ModEntry.instance.Monitor.Log("BeginRun; Program source: " + source);
			System.GC.Collect();
			runningInstance = this;

			if (interpreter.vm == null) interpreter.REPL("", 0);	// (forces creation of a VM)
			else interpreter.vm.Reset();
			if (string.IsNullOrEmpty(source)) return;
		
			ValMap globals = interpreter.vm.globalContext.variables;
			if (globals != null) globals.map.Remove(M1API._stackAtBreak);
			AddGlobals();

			interpreter.Reset(source);
			try {
				interpreter.Compile();
			} catch (MiniscriptException me) {
				ModEntry.instance.Monitor.Log("Caught MiniScript exception: " + me);
			}
			if (interpreter.vm == null) interpreter.REPL("", 0);
			interpreter.vm.globalContext.variables = globals;
			interpreter.RunUntilDone(0.03f);
			//lastNonidleTime = Time.time;
			if (interpreter.NeedMoreInput()) {
				// If the interpreter wants more input at this point, it's because the program
				// has an unterminated if/while/for/function block.  Let's just cancel the run.
				ModEntry.instance.Monitor.Log("Canceling run in BeginRun");
				Break(true);
			}		
		}

		public void Run() {
			Break(true);
			console.keyBuffer.Clear();
			console.TypeInput("run\n");
		}

		public void Break(bool silent=false) {
			if (!silent && !allowControlCBreak) return;
		
			// grab the full stack and tuck it away for future reference
			ValList stack = M1API.StackList(interpreter.vm);
		
			// also find the first non-null entry, to display right away
			SourceLoc loc = null;
			if (interpreter.vm != null) {
				foreach (var stackLoc in interpreter.vm.GetStack()) {
					loc = stackLoc;
					if (loc != null) break;
				}
			}
			interpreter.Stop();
			console.AbortInput();
			console.keyBuffer.Clear();
			if (!silent) {
				string msg = "BREAK";
				if (loc != null) {
					msg += " at ";
					if (loc.context != null) msg += loc.context + " ";
					msg += "line " + loc.lineNum;
				}
				textDisplay.Print(msg + "\n");
				//ModEntry.instance.Monitor.Log("printed: " + msg);
			}
			ValMap globals = interpreter.vm.globalContext.variables;
			interpreter.Reset();
			interpreter.REPL("");	// (forces creation of a VM)
			interpreter.vm.globalContext.variables = globals;
			globals.SetElem(M1API._stackAtBreak, stack);
			AddGlobals();
			//ModEntry.instance.Monitor.Log("Rebuilt VM and restored " + globals.Count + " globals");
		}

		public void AddGlobals() {
			var globals = interpreter.vm.globalContext;
			if (globals.variables == null) globals.variables = new ValMap();
			if (bot != null) {
				curStatusColor = new ValString(bot.statusColor.ToHexString());
				curScreenColor = new ValString(bot.screenColor.ToHexString());
				globals.variables["statusColor"] = curStatusColor;
				globals.variables["screenColor"] = curScreenColor;
				globals.variables.assignOverride = (key, value) => {
					string keyStr = key.ToString();
					if (keyStr == "_") return false;
					//ModEntry.instance.Monitor.Log($"global {key} = {value}");
					if (keyStr == "statusColor") {		// DEPRECATED: now in bot module
						bot.statusColor = value.ToString().ToColor();
					} else if (keyStr == "screenColor") {
						console.backColor = value.ToString().ToColor();
					}
					return false;	// allow the assignment
				};
			} else {
				globals.variables["screenColor"] = new ValString(console.backColor.ToHexString());
				globals.variables.assignOverride = (key, value) => {
					string keyStr = key.ToString();
					if (keyStr == "_") return false;
					//ModEntry.instance.Monitor.Log($"global {key} = {value}");
					if (keyStr == "screenColor") {
						console.backColor = value.ToString().ToColor();
					}
					return false;	// allow the assignment
				};
            }
		}
		
		void Clear() {
			TextDisplay disp = console.display;
			disp.Clear();
			disp.SetCursor(disp.rows-1, 0);
		}
	
		public void Exit() {
			if (interpreter.Running()) {
				//interpreter.vm.globalContext.variables.SetElem(MiniMicroAPI._stackAtBreak, 
				//	MiniMicroAPI.StackList(interpreter.vm));
				interpreter.Stop();
			}
		}

		public void PrintLine(string line) {
			TextDisplay disp = console.display;
			disp.Print(line);
			disp.Print(disp.delimiter);
		}

		void PrintLineWithTaskCheck(string line) {
			PrintLine(line);
			ToDoManager.NotePrintOutput(line);
		}
	}
}

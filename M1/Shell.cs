/*
This script represents the shell of the machine -- the command interpreter.
It connects a MiniScript interpreter to the Console which interacts with
the user. 
*/

using System;
using System.IO;
using StardewValley;
using StardewModdingAPI;
//using StardewValley.Menus;
using Miniscript;
using Microsoft.Xna.Framework;

namespace M1 {
	public class Shell {
		public Console console { get; private set; }
		public Bot bot {  get; private set; }
		public bool allowControlCBreak = true;

		public static Shell runningInstance;

		Interpreter interpreter;
		bool runProgram;
		string inputReceived;		// stores input while app is running, for _input intrinsic

		string sysDiskPath;
		string usrDiskPath;

		ValString curStatusColor;
		ValString curScreenColor;

		public TextDisplay textDisplay {  get {  return console.display; } }
		
		public Shell() {
			console = new M1.Console(this);

			// prepare the interpreter
			interpreter = new Interpreter(null, PrintLine, PrintLine);
			interpreter.implicitOutput = PrintLine;
			interpreter.hostData = this;
		}

		public void Init(Bot botContext=null) {
			this.bot = botContext;
			M1API.Init(this);

			var display = console.display;
			display.backColor = new Color(0.31f, 0.11f, 0.86f);
			display.Clear();

			var colors = new Color[] { Color.Red, Color.Yellow, Color.Green, Color.Purple };

			display.SetCursor(19, 1);
			for (int i=0; i<4; i++) {
				display.textColor = colors[i]; display.Print("*");
			}
			display.textColor = Color.Azure; display.Print(" MiniScript M-1 " + (botContext==null ? "Home" : "Bot") + " Computer ");
			for (int i=0; i<4; i++) {
				display.textColor = colors[3-i]; display.Print("*");
			}
			display.textColor = Color.White;
			display.NextLine();

			if (interpreter.vm == null) {
				interpreter.REPL("", 0);	// (forces creation of a VM)
				AddGlobals();
			}

			sysDiskPath = Path.Combine(ModEntry.helper.DirectoryPath, "assets", "sysdisk");
			if (!string.IsNullOrEmpty(Constants.CurrentSavePath)) {
				usrDiskPath = Path.Combine(Constants.CurrentSavePath, "usrdisk");
			}
			RunStartupScripts();
		}

		void RunStartupScripts() {
			// load and run the startup script(s)
			runningInstance = this;

			// /sys/startup.ms
			string sysStartupPath = Path.Combine(sysDiskPath, "startup.ms");
			if (File.Exists(sysStartupPath)) { 
				string startupScript = File.ReadAllText(sysStartupPath);
				if (!string.IsNullOrEmpty(startupScript)) {
					try {
						interpreter.REPL(startupScript);
					} catch (System.Exception err) {
						Debug.Log("Error running /sys/startup.ms: " + err.ToString());
					}
				}
			}

			// /usr/startup.ms
			if (!string.IsNullOrEmpty(usrDiskPath)) {
				string usrStartupPath = Path.Combine(usrDiskPath, "startup.ms");
				Debug.Log($"Path to startup script: {usrStartupPath}");
				if (File.Exists(usrStartupPath)) {
					string startupScript = File.ReadAllText(usrStartupPath);
					if (!string.IsNullOrEmpty(startupScript)) BeginRun(startupScript);
				} else Debug.Log("No /usr/startup.ms found");
			} else Debug.Log("CurrentSavePath is empty");

			// Print friendly prompt, unless our startup script is still running.
			if (!interpreter.Running()) {
				PrintLine("");
				PrintLine("Ready!");
				PrintLine("");
			}
		}

		public void Update(GameTime gameTime) {
			if (interpreter == null) return;		// still loading
			runningInstance = this;
			if (interpreter.NeedMoreInput()) {
				GetCommand();		// (though in this case, this really means: get ANOTHER command!)
			} else if (interpreter.Running()) {
				// continue the running code
				interpreter.RunUntilDone(0.03f);
			} else if (runProgram) {
				//Debug.Log("runProgram flag detected; starting new program");
				runProgram = false;
				interpreter.Stop();
				//Value sourceVal = interpreter.GetGlobalValue("_source");
				//string source = (sourceVal == null ? null : sourceVal.JoinToString());
				//BeginRun(source);
			} else {
				// nothing running; get another command!
				GetCommand();
			}
		}

		void GetCommand() {
			if (console.InputInProgress()) return;		// already working on it!
			TextDisplay disp = console.display;
			if (disp.GetCursor().col != 0) disp.NextLine();
			string prompt = "]";
			if (interpreter.NeedMoreInput()) prompt = "...]";
			//Value promptVal;
			//if (env.map.TryGetValue(new ValString(
			//			interpreter.NeedMoreInput() ? "morePrompt" : "prompt"), out promptVal)) {
			//	prompt = promptVal.ToString();
			//}
			disp.Print(prompt);
			//autocompleter.machine = interpreter.vm;
			//console.autocompCallback = autocompleter.GetSuggestion;
			console.StartInput();
		}
	

		public void HandleCommand(string command) {
			if (interpreter.Running() && !interpreter.NeedMoreInput()) {
				inputReceived = command;
				return;
			}
		
			command = command.Trim();
			string lcmd = command.ToLower();
		
			if (interpreter == null) ModEntry.instance.print("Error: Interpreter null?!?");

			runningInstance = this;
			interpreter.REPL(command, 0.1f);
		}
	
		void BeginRun(string source) {
			Debug.Log("BeginRun; Program source: " + source);
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
				Debug.Log("Caught MiniScript exception: " + me);
			}
			if (interpreter.vm == null) interpreter.REPL("", 0);
			interpreter.vm.globalContext.variables = globals;
			interpreter.RunUntilDone(0.03f);
			//lastNonidleTime = Time.time;
			if (interpreter.NeedMoreInput()) {
				// If the interpreter wants more input at this point, it's because the program
				// has an unterminated if/while/for/function block.  Let's just cancel the run.
				Debug.Log("Canceling run in BeginRun");
				Break(true);
			}		
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
				//Debug.Log("printed: " + msg);
			}
			ValMap globals = interpreter.vm.globalContext.variables;
			interpreter.Reset();
			interpreter.REPL("");	// (forces creation of a VM)
			interpreter.vm.globalContext.variables = globals;
			globals.SetElem(M1API._stackAtBreak, stack);
			AddGlobals();
			//Debug.Log("Rebuilt VM and restored " + globals.Count + " globals");
		}

		public void AddGlobals() {
			if (bot != null) {
				curStatusColor = new ValString(bot.statusColor.ToHexString());
				curScreenColor = new ValString(bot.screenColor.ToHexString());
				var globals = interpreter.vm.globalContext;
				if (globals.variables == null) globals.variables = new ValMap();
				globals.variables["statusColor"] = curStatusColor;
				globals.variables["screenColor"] = curScreenColor;
				globals.variables.assignOverride = (key, value) => {
					string keyStr = key.ToString();
					if (keyStr == "_") return false;
					Debug.Log($"global {key} = {value}");
					if (keyStr == "statusColor") {
						bot.statusColor = value.ToString().ToColor();
					} else if (keyStr == "screenColor") {
						bot.screenColor = value.ToString().ToColor();
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
	
		void Exit() {
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

	}
}

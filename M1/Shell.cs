/*
This script represents the shell of the machine -- the command interpreter.
It connects a MiniScript interpreter to the Console which interacts with
the user. 
*/

using System;
using StardewValley;
//using StardewModdingAPI;
//using StardewValley.Menus;
using Miniscript;
using Microsoft.Xna.Framework;

namespace M1 {
	public class Shell {
		public Console console { get; private set; }
		Interpreter interpreter;
		bool runProgram;
		static bool intrinsicsAdded;
		string inputReceived;		// stores input while app is running, for _input intrinsic

		public Shell() {
			console = new M1.Console(this);
			AddIntrinsics();

			// prepare the interpreter
			interpreter = new Interpreter(null, PrintLine, PrintLine);
			interpreter.implicitOutput = PrintLine;
			interpreter.hostData = this;
		}

		public void Present() {
			M1API.Init(this);
			console.Present();
		}

		public void Update(GameTime gameTime) {
			if (interpreter == null) return;		// still loading
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
		
			interpreter.REPL(command, 0.1f);
		}
	
		public void PrintLine(string line) {
			TextDisplay disp = console.display;
			disp.Print(line);
			disp.Print(disp.delimiter);
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




		public static void AddIntrinsics() {
			if (intrinsicsAdded) return;
			intrinsicsAdded = true;
		}
	
	}
}

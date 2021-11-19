using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;

namespace M1
{
	public class ModEntry : Mod {
		public static IModHelper helper;
		public static ModEntry instance;

		Shell shell;

		public override void Entry(IModHelper helper) {
			instance = this;
			ModEntry.helper = helper;
			helper.Events.Display.MenuChanged += this.OnMenuChanged;
		}

		public void print(string s) {
            this.Monitor.Log(s, LogLevel.Debug);
		}

		public void OnMenuChanged(object sender, MenuChangedEventArgs e) {
			var dlog = e.NewMenu as DialogueBox;
			if (dlog == null || !dlog.isQuestion || dlog.responses[0].responseKey != "Weather") return;

			// insert our new response
			Response r = new Response("M1", "MiniScript M-1 Home Computer");
			dlog.responses.Insert(dlog.responses.Count-1, r);
			// adjust the dialog height
			var h = SpriteText.getHeightOfString(r.responseText, dlog.width) + 16;
			dlog.heightForQuestions += h; dlog.height += h;
			// intercept the handler (but call the original one for other responses)
			var prevHandler = Game1.currentLocation.afterQuestion;
			Game1.currentLocation.afterQuestion = (who, whichAnswer) => {
				print($"{who} selected channel {whichAnswer}");
				if (whichAnswer == "M1") PresentComputer();
				else prevHandler(who, whichAnswer);
			};
		}

		private void PresentComputer() {
			if (shell == null) shell = new Shell();
			shell.Present();
		}
	}
}

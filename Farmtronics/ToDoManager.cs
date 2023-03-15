// This module detects and keeps track of the state of the "to-do" (quest) tasks,
// mostly based on observing `print` output to the console.
using System.Collections.Generic;
using StardewValley;

namespace Farmtronics {
	static class ToDoManager {
		static int nextForOutputExpected = 1;
		static int nextFizzBuzzExpected = 1;

		static Dictionary<Task, bool> taskDone = new Dictionary<Task, bool>();
		
#if DEBUG
		public static void MarkAllTasksDone() {
			for (int i = 0; i <(int)Task.kQtyTasks; i++)
			{
				MarkTaskDone((Task)i);
			}
		}
#endif

		public static bool IsTaskDone(Task task) {
			if (!taskDone.ContainsKey(task)) {
				string key = $"{ModEntry.instance.ModManifest.UniqueID}/task/{task}";
				if (Game1.player.modData.ContainsKey(key)) {
					taskDone[task] = (Game1.player.modData[key] == "1");
				} else {
					taskDone[task] = false;
				}
			}
			return taskDone[task];
		}

		static string FizzBuzz(int i) {
			if (i % 15 == 0) return "FizzBuzz";
			if (i % 5 == 0) return "Buzz";
			if (i % 3 == 0) return "Fizz";
			return i.ToString();
		}

		public static void NotePrintOutput(string s) {
			if (s == "Hello world!" || s == "Hello World!") {
				MarkTaskDone(Task.helloWorld);
			} else if (s.StartsWith("Editing: ")) {
				MarkTaskDone(Task.editProgram);
			} else if (s.Contains(" lines saved to ") || s.Contains(" line saved to ")) {
				MarkTaskDone(Task.saveProgram);
			}

			if (nextForOutputExpected <= 100 && s == nextForOutputExpected.ToString()) {
				nextForOutputExpected++;
				if (nextForOutputExpected > 100) MarkTaskDone(Task.for1to100);
			} else if (nextForOutputExpected <= 100) nextForOutputExpected = 1;

			if (nextFizzBuzzExpected <= 100 && s == FizzBuzz(nextFizzBuzzExpected)) {
				nextFizzBuzzExpected++;
				if (nextFizzBuzzExpected > 100) MarkTaskDone(Task.fizzBuzz);
			} else if (nextFizzBuzzExpected <= 100) nextFizzBuzzExpected = 1;

		}

		public static void NoteCd(string path) {
			MarkTaskDone(Task.cd);
		}

		public static void NoteRun(string sourcePath) {
			if (sourcePath.StartsWith("/sys/demo/")) {
				MarkTaskDone(Task.runDemo);
			}
		}

		public static bool AllTasksDone() {
			for (int i=0; i<(int)Task.kQtyTasks; i++) {
				if (!IsTaskDone((Task)i)) return false;
			}
			return true;
		}

		static void MarkTaskDone(Task task) {
			if (IsTaskDone(task)) return;	// (task was already done)
			ModEntry.instance.Monitor.Log($"ToDoManager.MarkTaskDone({task})");
			taskDone[task] = true;

			Game1.player.modData[$"{ModEntry.instance.ModManifest.UniqueID}/task/{task}"] = "1";

			// When all tasks have been done, send the mail!
			if (AllTasksDone()) SendFirstBotMail();
		}

		public static void SendFirstBotMail() {
			// Adds instantly, but doesn't save; better for testing:
			//Game1.player.mailbox.Add("FarmtronicsFirstBotMail");
			//Game1.player.recoveredItem = new Bot();

			// Adds mail for the next day, and saves -- what we want for deployment:
			Game1.addMailForTomorrow("FarmtronicsFirstBotMail");
			ModEntry.instance.Monitor.Log("first-bot mail sent; will be delivered in the morning");
		}
	}
}
// This module detects and keeps track of the state of the "to-do" (quest) tasks,
// mostly based on observing `print` output to the console.

using System;
using System.Collections.Generic;

namespace M1 {
	public static class ToDoManager {

		public enum Task {
			helloWorld = 0,
			cd = 1,
			runDemo = 2,
			editProgram = 3,
			saveProgram = 4,
			for1to100 = 5,
			fizzBuzz = 6,
			kQtyTasks
		}

		static Dictionary<Task, bool> taskDone = new Dictionary<Task, bool>();

		public static bool IsTaskDone(Task task) {
			if (!taskDone.ContainsKey(task)) return false;
			return taskDone[task];
		}

		public static void NotePrintOutput(string s) {
			if (s == "Hello world!" || s == "Hello World!") {
				MarkTaskDone(Task.helloWorld);
			}
		}

		public static void NoteCd(string path) {
			MarkTaskDone(Task.cd);
		}

		public static void NoteRun(string sourcePath) {
			if (sourcePath.StartsWith("/sys/demo/")) {
				MarkTaskDone(Task.runDemo);
			}
		}

		static void MarkTaskDone(Task task) {
			if (IsTaskDone(task)) return;	// (task was already done)
			Debug.Log($"ToDoManager.MarkTaskDone({task})");
			taskDone[task] = true;
		}
	}
}

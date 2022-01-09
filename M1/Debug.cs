using System;
using StardewModdingAPI;

namespace Farmtronics {
	public static class Debug {
		public static void Log(string s, object context=null) {
			ModEntry.instance.Monitor.Log(DateTime.Now.ToString("'['HH':'mm':'ss'] '") + s, LogLevel.Debug);
		}
	}
}

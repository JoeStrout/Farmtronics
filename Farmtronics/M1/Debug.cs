using System;
using StardewModdingAPI;

namespace Farmtronics {
	public static class Debug {

		// While debugging, feel free to change the following to
		// LogLevel.Info or even LogLevel.Trace so it all prints.
		// But before committing, please change it back to LogLevel.Error.
		const LogLevel logLevelToPrint = LogLevel.Trace;

		public static void Log(string logMsg, LogLevel logLevel=LogLevel.Info) {
			if (logLevel >= logLevelToPrint) {
				ModEntry.instance.Monitor.Log(logMsg);
			}
		}
	}

}

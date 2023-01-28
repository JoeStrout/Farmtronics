// This class (ModData) defines global data which is saved in the game save file.
// Reference: https://stardewcommunitywiki.com/Modding:Modder_Guide/APIs/Data
//
// We keep around a singleton instance of this (instance), whiche any code that
// has the need can read or update and the changes will get saved with the game.

using System;

namespace Farmtronics {
	public class ModData {

		static ModData _instance;
		public static ModData instance {
			get {
				if (_instance == null) {
					_instance = ModEntry.instance.Helper.Data.ReadSaveData<ModData>("ModData");
					if (_instance == null) _instance = new ModData();
				}
				return _instance;
			}
		}

		public static void Save() {
			ModEntry.instance.Helper.Data.WriteSaveData("ModData", _instance);
		}

		// Global data:
		public string HomeComputerName {  get; set; } = "Home Computer";
	}
}

// This module defines per-player data which is saved in the game save file.
// References:
// https://stardewcommunitywiki.com/Modding:Modder_Guide/APIs/Data
// https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.5#Custom_mod_data
//
// We keep around a singleton instance of this (instance), whiche any code that
// has the need can read or update and the changes will get saved with the game.

using System;
using StardewValley;

namespace Farmtronics {
	public static class PerPlayerData {

		static string HomeComputerNameKey {
			get {  return $"{ModEntry.instance.ModManifest.UniqueID}/HomeComputerName"; }
		}

		public static string HomeComputerName { 
			get {
				string result;
				if (Game1.player.modData.TryGetValue(HomeComputerNameKey, out result)) {
					return result;
				}
				return "Home Computer";
			}
			set {
				Game1.player.modData[HomeComputerNameKey] = value;
			}
		}
	}
}

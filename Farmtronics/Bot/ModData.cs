using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Bot {
	class ModData {
		// mod data keys, used for saving/loading extra data with the game save:
		public const string IS_BOT		= "isBot";
		public const string MOD_VERSION = "modVersion";
		public const string NAME   		= "name";
		public const string ENERGY 		= "energy";
		public const string FACING 		= "facing";
		
		public bool 			IsBot			{ get; internal set; }
		public ISemanticVersion ModVersion		{ get; internal set; }
		public string 			Name			{ get; internal set; }
		public int 				Energy			{ get; internal set; }
		public int 				FacingDirection { get; internal set; }
		
		public static bool TryGetModData(ModDataDictionary data, out ModData modData) {
			modData = new ModData();
			if (!data.TryGetValue(ModEntry.GetModDataKey(IS_BOT), out string isBot)) return false;
			if (!data.TryGetValue(ModEntry.GetModDataKey(MOD_VERSION), out string modVer)) return false;
			if (!SemanticVersion.TryParse(modVer, out ISemanticVersion modVersion)) return false;
			if (!data.TryGetValue(ModEntry.GetModDataKey(NAME), out string name)) return false;
			if (!data.TryGetValue(ModEntry.GetModDataKey(ENERGY), out string energy)) return false;
			if (!data.TryGetValue(ModEntry.GetModDataKey(FACING), out string facing)) return false;
			
			modData.IsBot			= int.Parse(isBot) == 1;
			modData.ModVersion 		= modVersion;
			modData.Name			= name;
			modData.Energy			= int.Parse(energy);
			modData.FacingDirection = int.Parse(facing);
			
			return true;
		}
		
		public void Save(ref ModDataDictionary data) {
			Dictionary<string, string> saveData = new Dictionary<string, string>();
			
			saveData.Add(IS_BOT, IsBot ? "1" : "0");
			saveData.Add(MOD_VERSION, ModVersion.ToString());
			saveData.Add(NAME, Name);
			saveData.Add(ENERGY, Energy.ToString());
			saveData.Add(FACING, FacingDirection.ToString());
			
			data.Set(saveData);
		}
	}
}
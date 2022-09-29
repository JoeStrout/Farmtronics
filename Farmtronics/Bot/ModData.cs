using System.Collections.Generic;
using Farmtronics.Utils;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Bot {
	class ModData {
		// mod data keys, used for saving/loading extra data with the game save:		
		public bool 			IsBot	   { get; internal set; }
		public ISemanticVersion ModVersion { get; internal set; }
		public string 			Name	   { get; internal set; }
		public int 				Energy	   { get; internal set; }
		public int 				Facing	   { get; internal set; }
		
		public static bool TryGetModData(ModDataDictionary data, out ModData modData) {
			modData = new ModData();
			if (!data.TryGetValue(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), out string isBot)) return false;
			if (!data.TryGetValue(ModEntry.GetModDataKey(nameof(Name).FirstToLower()), out string name)) return false;
			if (!data.TryGetValue(ModEntry.GetModDataKey(nameof(Facing).FirstToLower()), out string facing)) return false;

			// TODO: This is a new modData key, we need to make sure we are compatible with older versions.
			if (data.TryGetValue(ModEntry.GetModDataKey(nameof(ModVersion).FirstToLower()), out string modVer) && SemanticVersion.TryParse(modVer, out ISemanticVersion modVersion)) {
				modData.ModVersion = modVersion;
			} else {
				modData.ModVersion = new SemanticVersion(1, 2, 0);
			}
			
			if (data.TryGetValue(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), out string energy)) modData.Energy = int.Parse(energy);
			
			modData.IsBot			= int.Parse(isBot) == 1;
			modData.Name			= name;
			modData.Facing 			= int.Parse(facing);
			
			return true;
		}
		
		public void Save(ref ModDataDictionary data) {
			Dictionary<string, string> saveData = new Dictionary<string, string>();
			
			saveData.Add(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), IsBot ? "1" : "0");
			saveData.Add(ModEntry.GetModDataKey(nameof(ModVersion).FirstToLower()), ModVersion.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Name).FirstToLower()), Name);
			saveData.Add(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), Energy.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Facing).FirstToLower()), Facing.ToString());
			
			data.Set(saveData);
		}
		
		public void RemoveEnergy(ref ModDataDictionary data)
		{
			if (data.ContainsKey((ModEntry.GetModDataKey(nameof(Energy).FirstToLower()))))
				data.Remove((ModEntry.GetModDataKey(nameof(Energy).FirstToLower())));
		}
	}
}
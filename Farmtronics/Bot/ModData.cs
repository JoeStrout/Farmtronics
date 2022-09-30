using System.Collections.Generic;
using Farmtronics.Utils;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Bot {
	class ModData {
		private readonly BotObject bot;
		
		// mod data keys, used for saving/loading extra data with the game save:
		public bool 			IsBot	   { get; internal set; }
		public ISemanticVersion ModVersion { get; internal set; }
		public string 			Name	   { get; internal set; }
		public float			Energy	   { get; internal set; }
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
			
			if (data.TryGetValue(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), out string energy)) modData.Energy = float.Parse(energy);
			
			modData.IsBot			= int.Parse(isBot) == 1;
			modData.Name			= name;
			modData.Facing 			= int.Parse(facing);
			
			return true;
		}
		
		internal void Load(bool applyEnergy = true) {
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), out string isBot)) IsBot = int.Parse(isBot) == 1;
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(ModVersion).FirstToLower()), out string modVer) && SemanticVersion.TryParse(modVer, out ISemanticVersion modVersion)) ModVersion = modVersion;
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(Name).FirstToLower()), out string name)) Name = name;
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), out string energy)) Energy = float.Parse(energy);
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(Facing).FirstToLower()), out string facing)) Facing = int.Parse(facing);
			
			if (ModVersion == null) ModVersion = new SemanticVersion(1, 2, 0);
			if (Name == null) Name = I18n.Bot_Name();
			
			bot.BotName = Name;
			bot.facingDirection = Facing;
			if (applyEnergy) bot.energy = Energy;
		}
		
		private Dictionary<string, string> GetModData() {
			Dictionary<string, string> saveData = new Dictionary<string, string>();

			saveData.Add(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), IsBot ? "1" : "0");
			saveData.Add(ModEntry.GetModDataKey(nameof(ModVersion).FirstToLower()), ModVersion.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Name).FirstToLower()), Name);
			saveData.Add(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), Energy.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Facing).FirstToLower()), Facing.ToString());
			
			return saveData;
		}
		
		public ModData() {
			
		}
		
		public ModData(BotObject bot) {
			this.bot = bot;
			this.Load();
		}
		
		public void Save() {
			if (bot == null) return;
			
			bot.modData.Set(GetModData());
		}
		
		public void Save(ref ModDataDictionary data) {			
			data.Set(GetModData());
		}
		
		public void RemoveEnergy(ref ModDataDictionary data)
		{
			if (data.ContainsKey((ModEntry.GetModDataKey(nameof(Energy).FirstToLower()))))
				data.Remove((ModEntry.GetModDataKey(nameof(Energy).FirstToLower())));
		}
		
		public void RemoveEnergy() {
			if (bot == null) return;
			
			RemoveEnergy(ref bot.modData);
		}
		
		public void Update() {
			if (bot == null) return;

			Name   = bot.BotName;
			Energy = bot.energy;
			Facing = bot.facingDirection;
			
			Save();
		}
	}
}
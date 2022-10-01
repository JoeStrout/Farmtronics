using System.Collections.Generic;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Bot {
	class ModData {
		private readonly BotObject bot;
		
		// mod data keys, used for saving/loading extra data with the game save:
		public bool 			IsBot		{ get; internal set; } = true;
		public ISemanticVersion ModVersion	{ get; internal set; }
		public string 			Name		{ get; internal set; }
		public float			Energy		{ get; internal set; }
		public int 				Facing		{ get; internal set; }

		// the following mod data keys, won't be saved and are only used for multiplayer synchronization
		public Color			ScreenColor { get; internal set; } = Color.Transparent;
		public Color			StatusColor { get; internal set; } = Color.Yellow;
		
		public static bool IsBotData(ModDataDictionary data) {
			if (data.TryGetValue(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), out string isBot)) {
				return int.Parse(isBot) == 1;
			}
			return false;
		}
		
		internal void Load(bool applyEnergy = true) {
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), out string isBot)) IsBot = int.Parse(isBot) == 1;
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(ModVersion).FirstToLower()), out string modVer) && SemanticVersion.TryParse(modVer, out ISemanticVersion modVersion)) ModVersion = modVersion;
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(Name).FirstToLower()), out string name)) Name = name;
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), out string energy)) Energy = float.Parse(energy);
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(Facing).FirstToLower()), out string facing)) Facing = int.Parse(facing);

			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(ScreenColor).FirstToLower()), out string screenColor)) ScreenColor = screenColor.ToColor();
			if (bot.modData.TryGetValue(ModEntry.GetModDataKey(nameof(StatusColor).FirstToLower()), out string statusColor)) StatusColor = statusColor.ToColor();

			if (ModVersion == null) ModVersion = new SemanticVersion(1, 2, 0);
			if (Name == null) Name = I18n.Bot_Name();

			if (bot.BotName != Name) bot.BotName = Name;
			if (bot.facingDirection != Facing) bot.facingDirection = Facing;
			if (applyEnergy && bot.energy != Energy) bot.energy = Energy;

			if (bot.screenColor != ScreenColor) bot.screenColor = ScreenColor;
			if (bot.statusColor != StatusColor) bot.statusColor = StatusColor;
		}
		
		private Dictionary<string, string> GetModData(bool isSaving) {
			Dictionary<string, string> saveData = new Dictionary<string, string>();

			saveData.Add(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), IsBot ? "1" : "0");
			saveData.Add(ModEntry.GetModDataKey(nameof(ModVersion).FirstToLower()), ModVersion.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Name).FirstToLower()), Name);
			saveData.Add(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), Energy.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Facing).FirstToLower()), Facing.ToString());

			if (!isSaving) {
				saveData.Add(ModEntry.GetModDataKey(nameof(ScreenColor).FirstToLower()), ScreenColor.ToHexString());
				saveData.Add(ModEntry.GetModDataKey(nameof(StatusColor).FirstToLower()), StatusColor.ToHexString());
			}
			
			return saveData;
		}
		
		public ModData(BotObject bot) {
			this.bot = bot;
			this.Load(false);
			this.Save(false);
		}

		public void Save(ref ModDataDictionary data, bool isSaving) {
			data.Set(GetModData(isSaving));
		}
		
		public void Save(bool isSaving) {
			Save(ref bot.modData, isSaving);
		}
		
		public void RemoveEnergy(ref ModDataDictionary data) {
			if (data.ContainsKey((ModEntry.GetModDataKey(nameof(Energy).FirstToLower()))))
				data.Remove((ModEntry.GetModDataKey(nameof(Energy).FirstToLower())));
		}
		
		public void RemoveEnergy() {
			RemoveEnergy(ref bot.modData);
		}
		
		public void Update() {
			Name = bot.BotName;
			Energy = bot.energy;
			Facing = bot.facingDirection;

			ScreenColor = bot.screenColor;
			StatusColor = bot.statusColor;
			
			Save(false);
		}
	}
}
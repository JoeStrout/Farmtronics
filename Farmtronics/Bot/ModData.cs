using System;
using System.Collections.Generic;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Bot {
	class ModData {
		private readonly BotObject bot;
		
		// mod data keys, used for saving/loading extra data with the game save:
		public bool 			IsBot		{ get; internal set; }
		public ISemanticVersion ModVersion	{ get; internal set; }
		public string 			Name		{ get; internal set; }
		public float			Energy		{ get; internal set; }
		public int 				Facing		{ get; internal set; }

		// the following mod data keys, won't be saved and are only used for multiplayer synchronization
		public Color			ScreenColor { get; internal set; }
		public Color			StatusColor { get; internal set; }
		
		private static string GetModDataValue(ModDataDictionary data, string key, string defaultValue = "") {
			return data.TryGetValue(ModEntry.GetModDataKey(key.FirstToLower()), out string value) ? value : defaultValue;
		}
		
		private static T GetModDataValue<T>(ModDataDictionary data, string key, T defaultValue = default) {
			return data.TryGetValue(ModEntry.GetModDataKey(key.FirstToLower()), out string value) ? (T)Convert.ChangeType(value, typeof(T)) : defaultValue;
		}
		
		public static bool IsBotData(ModDataDictionary data) {
			return GetModDataValue<int>(data, nameof(IsBot)) == 1;
		}
		
		internal void Load(bool applyEnergy = true) {
			IsBot = GetModDataValue<int>(bot.modData, nameof(IsBot), 1) == 1;
			ModVersion = new SemanticVersion(GetModDataValue(bot.modData, nameof(ModVersion), ModEntry.instance.ModManifest.Version.ToString()));
			Name = GetModDataValue(bot.modData, nameof(Name), I18n.Bot_Name());
			Energy = GetModDataValue<float>(bot.modData, nameof(Energy), Farmer.startingStamina);
			Facing = GetModDataValue<int>(bot.modData, nameof(Facing));
			
			ScreenColor = GetModDataValue(bot.modData, nameof(ScreenColor), Color.Transparent.ToHexString()).ToColor();
			StatusColor = GetModDataValue(bot.modData, nameof(StatusColor), Color.Yellow.ToHexString()).ToColor();

			if (ModVersion.IsOlderThan(ModEntry.instance.ModManifest.Version)) {
				// NOTE: Do ModData update stuff here
				ModVersion = ModEntry.instance.ModManifest.Version;
			}

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
			var energyKey = ModEntry.GetModDataKey(nameof(Energy).FirstToLower());
			if (data.ContainsKey(energyKey))
				data.Remove(energyKey);
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
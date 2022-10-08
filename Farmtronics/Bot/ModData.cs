using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;

namespace Farmtronics.Bot {
	class ModData {
		private readonly BotObject bot;
		private XmlSerializer serializer;
		
		// mod data keys, used for saving/loading extra data with the game save:
		public bool 			IsBot		{ get; internal set; }
		public ISemanticVersion ModVersion	{ get; internal set; }
		public string 			Name		{ get; internal set; }
		public float			Energy		{ get; internal set; }
		public int 				Facing		{ get; internal set; }
		// New with 1.3.0
		public IList<Item>		Inventory	{ get; internal set; }

		// the following mod data keys, won't be saved and are only used for multiplayer synchronization
		public Color			ScreenColor { get; internal set; }
		public Color			StatusColor { get; internal set; }
		// New with 1.3.0
		public float			PositionX	{ get; internal set; }
		public float 			PositionY 	{ get; internal set; }
		
		private static string GetModDataValue(ModDataDictionary data, string key, string defaultValue = "") {
			return data.TryGetValue(ModEntry.GetModDataKey(key.FirstToLower()), out string value) ? value : defaultValue;
		}
		
		private static T GetModDataValue<T>(ModDataDictionary data, string key, T defaultValue = default) {
			return data.TryGetValue(ModEntry.GetModDataKey(key.FirstToLower()), out string value) ? (T)Convert.ChangeType(value, typeof(T)) : defaultValue;
		}
		
		public static bool IsBotData(ModDataDictionary data) {
			return GetModDataValue<int>(data, nameof(IsBot)) == 1;
		}
		
		private string SerializeInventory(IList<Item> inventory) {
			if (inventory == null) return null;
			var stream = new MemoryStream();
			var netInventory = new NetObjectList<Item>(inventory);
			// foreach (var item in inventory.Where(item => item != null)) {
			// 	ModEntry.instance.Monitor.Log($"Serializing item: {item.Name} with id {item.ParentSheetIndex}");
			// }
			serializer.Serialize(stream, netInventory);
			var xml = Encoding.Default.GetString(stream.ToArray());
			// ModEntry.instance.Monitor.Log($"Serialized inventory: {xml}");
			return xml;
		}
		
		private NetObjectList<Item> DeserializeInventory(string inventoryXml) {
			if (string.IsNullOrEmpty(inventoryXml)) return null;
			
			var stream = new MemoryStream(Encoding.Default.GetBytes(inventoryXml));
			var inventory = serializer.Deserialize(stream) as NetObjectList<Item>;
			// foreach (var item in inventory.Where(item => item != null)) {
			// 	ModEntry.instance.Monitor.Log($"Deserialized item {item.Name} with id {item.ParentSheetIndex}");
			// }
			return inventory;
		}
		
		internal void Load(bool applyEnergy = true) {
			IsBot = GetModDataValue<int>(bot.modData, nameof(IsBot), 1) == 1;
			ModVersion = new SemanticVersion(GetModDataValue(bot.modData, nameof(ModVersion), ModEntry.instance.ModManifest.Version.ToString()));
			Name = GetModDataValue(bot.modData, nameof(Name), I18n.Bot_Name(BotManager.botCount));
			Energy = GetModDataValue<float>(bot.modData, nameof(Energy), Farmer.startingStamina);
			Facing = GetModDataValue<int>(bot.modData, nameof(Facing));
			Inventory = DeserializeInventory(GetModDataValue(bot.modData, nameof(Inventory)));
			
			ScreenColor = GetModDataValue(bot.modData, nameof(ScreenColor), Color.Transparent.ToHexString()).ToColor();
			StatusColor = GetModDataValue(bot.modData, nameof(StatusColor), Color.Yellow.ToHexString()).ToColor();
			PositionX	= GetModDataValue<float>(bot.modData, nameof(PositionX), bot.Position.X);
			PositionY	= GetModDataValue<float>(bot.modData, nameof(PositionY), bot.Position.Y);

			if (ModVersion.IsOlderThan(ModEntry.instance.ModManifest.Version)) {
				// NOTE: Do ModData update stuff here
				ModVersion = ModEntry.instance.ModManifest.Version;
			}
			
			Vector2 position = new Vector2(PositionX, PositionY);

			if (bot.Name != Name) bot.Name = bot.DisplayName = Name;
			if (bot.facingDirection != Facing) bot.facingDirection = Facing;
			if (applyEnergy && bot.energy != Energy) bot.energy = Energy;
			if (Inventory != null) {
				bot.inventory.Clear();
				foreach (var item in Inventory) {
					bot.inventory.Add(item);
				}	
			}

			if (bot.screenColor != ScreenColor) bot.screenColor = ScreenColor;
			if (bot.statusColor != StatusColor) bot.statusColor = StatusColor;
			if (bot.Position != position) bot.Position = position;
		}
		
		private Dictionary<string, string> GetModData(bool isSaving) {
			Dictionary<string, string> saveData = new Dictionary<string, string>();

			saveData.Add(ModEntry.GetModDataKey(nameof(IsBot).FirstToLower()), IsBot ? "1" : "0");
			saveData.Add(ModEntry.GetModDataKey(nameof(ModVersion).FirstToLower()), ModVersion.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Name).FirstToLower()), Name);
			saveData.Add(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()), Energy.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Facing).FirstToLower()), Facing.ToString());
			saveData.Add(ModEntry.GetModDataKey(nameof(Inventory).FirstToLower()), SerializeInventory(Inventory));

			if (!isSaving) {
				saveData.Add(ModEntry.GetModDataKey(nameof(ScreenColor).FirstToLower()), ScreenColor.ToHexString());
				saveData.Add(ModEntry.GetModDataKey(nameof(StatusColor).FirstToLower()), StatusColor.ToHexString());
				saveData.Add(ModEntry.GetModDataKey(nameof(PositionX).FirstToLower()), PositionX.ToString());
				saveData.Add(ModEntry.GetModDataKey(nameof(PositionY).FirstToLower()), PositionY.ToString());
			}
			
			return saveData;
		}
		
		public ModData(BotObject bot) {
			this.bot = bot;
#if DEBUG
			this.bot.modData.OnValueAdded += (key, value) => ModEntry.instance.Monitor.Log($"{Name} ModData OnValueAdded: {key}: {value}");
			this.bot.modData.OnValueRemoved += (key, value) => ModEntry.instance.Monitor.Log($"{Name} ModData OnValueRemoved: {key}: {value}");
			this.bot.modData.OnValueTargetUpdated += (key, oldValue, newValue) => ModEntry.instance.Monitor.Log($"{Name} ModData OnValueUpdated: {key}: {oldValue} -> {newValue}");
			this.bot.modData.OnConflictResolve += (key, rejected, accepted) => ModEntry.instance.Monitor.Log($"{Name} ModData OnConflictResolve: {key}: Rejected: {rejected} Accepted: {accepted}");
#endif
			this.serializer = SaveGame.GetSerializer(typeof(NetObjectList<Item>));
			this.Load(false);
			this.Save(false);
		}

		public void Save(ref ModDataDictionary data, bool isSaving) {
			foreach (var kv in GetModData(isSaving)) {
				data[kv.Key] = kv.Value;
			}
			// Remove temp keys
			if (isSaving) {
				data.Remove(ModEntry.GetModDataKey(nameof(ScreenColor).FirstToLower()));
				data.Remove(ModEntry.GetModDataKey(nameof(StatusColor).FirstToLower()));
				data.Remove(ModEntry.GetModDataKey(nameof(PositionX).FirstToLower()));
				data.Remove(ModEntry.GetModDataKey(nameof(PositionY).FirstToLower()));
			}
		}
		
		public void Save(bool isSaving) {
			Save(ref bot.modData, isSaving);
		}
		
		public void RemoveEnergy(ref ModDataDictionary data) {
			data.Remove(ModEntry.GetModDataKey(nameof(Energy).FirstToLower()));
		}
		
		public void Update() {
			Name = bot.Name;
			Energy = bot.energy;
			Facing = bot.facingDirection;
			Inventory = bot.inventory;

			ScreenColor = bot.screenColor;
			StatusColor = bot.statusColor;
			
			PositionX = bot.Position.X;
			PositionY = bot.Position.Y;
			
			Save(false);
		}
		
		public override string ToString() {
			return $"ModData [{Name}]\n\tPosition: {PositionX}/{PositionY}\n\tEnergy: {Energy}\n\tFacing: {Facing}\n\tScreenColor: {ScreenColor}\n\tStatusColor: {StatusColor}";
		}
	}
}
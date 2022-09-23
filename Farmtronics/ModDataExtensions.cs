using System;
using StardewValley;

namespace Farmtronics {
	static class ModDataExtensions {
		public static void SetModData<T>(this ModDataDictionary modData, T model) where T : IModData {
			switch (model) {
				case Bot.ModData botModel:
					modData[ModEntry.GetModDataKey(Bot.ModData.IS_BOT)] = botModel.IsBot ? "1" : "0";
					modData[ModEntry.GetModDataKey(Bot.ModData.NAME)]   = botModel.Name;
					modData[ModEntry.GetModDataKey(Bot.ModData.ENERGY)] = botModel.Energy.ToString();
					modData[ModEntry.GetModDataKey(Bot.ModData.FACING)] = botModel.FacingDirection.ToString();
					break;
				
				default:
					throw new InvalidOperationException("Couldn't find a matching ModData type.");
			}
		}
		
		public static bool HasModData<T>(this ModDataDictionary modData) where T : IModData {
			if (typeof(T) == typeof(Bot.ModData)) {
				if (!(modData.ContainsKey(ModEntry.GetModDataKey(Bot.ModData.IS_BOT))
						&& modData.ContainsKey(ModEntry.GetModDataKey(Bot.ModData.NAME))
						&& modData.ContainsKey(ModEntry.GetModDataKey(Bot.ModData.ENERGY))
						&& modData.ContainsKey(ModEntry.GetModDataKey(Bot.ModData.FACING)))) {
					return false;
				}
				return true;	
			}
			else
			{
				throw new InvalidOperationException("Couldn't find a matching ModData type.");	
			}
		}
		
		public static T GetModData<T>(this ModDataDictionary modData) where T : Bot.ModData,new() {
			return new() {
				IsBot  			= int.Parse(modData[ModEntry.GetModDataKey(Bot.ModData.IS_BOT)]) == 1,
				Name   			= modData[ModEntry.GetModDataKey(Bot.ModData.NAME)],
				Energy 			= int.Parse(modData[ModEntry.GetModDataKey(Bot.ModData.ENERGY)]),
				FacingDirection = int.Parse(modData[ModEntry.GetModDataKey(Bot.ModData.FACING)])
			};
		}
	}
}
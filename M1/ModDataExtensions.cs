using System;
using StardewValley;

namespace Farmtronics {
	static class ModDataExtensions {
		public static string UniqueID;
		
		public static void SetModData<T>(this ModDataDictionary modData, T model) where T : IModData {
			switch (model) {
				case Bot.ModData botModel:
					modData[$"{UniqueID}/{Bot.ModData.IS_BOT}"] = botModel.IsBot ? "1" : "0";
					modData[$"{UniqueID}/{Bot.ModData.NAME}"]   = botModel.Name;
					modData[$"{UniqueID}/{Bot.ModData.ENERGY}"] = botModel.Energy.ToString();
					modData[$"{UniqueID}/{Bot.ModData.FACING}"] = botModel.FacingDirection.ToString();
					break;
				
				default:
					throw new InvalidOperationException("Couldn't find a matching ModData type.");
			}
		}
		
		public static bool HasModData<T>(this ModDataDictionary modData) where T : IModData {
			if (typeof(T) == typeof(Bot.ModData)) {
				if (!(modData.ContainsKey($"{UniqueID}/{Bot.ModData.IS_BOT}")
						&& modData.ContainsKey($"{UniqueID}/{Bot.ModData.NAME}")
						&& modData.ContainsKey($"{UniqueID}/{Bot.ModData.ENERGY}")
						&& modData.ContainsKey($"{UniqueID}/{Bot.ModData.FACING}"))) {
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
				IsBot  			= int.Parse(modData[$"{UniqueID}/{Bot.ModData.IS_BOT}"]) == 1,
				Name   			= modData[$"{UniqueID}/{Bot.ModData.NAME}"],
				Energy 			= int.Parse(modData[$"{UniqueID}/{Bot.ModData.ENERGY}"]),
				FacingDirection = int.Parse(modData[$"{UniqueID}/{Bot.ModData.FACING}"])
			};
		}
	}
}
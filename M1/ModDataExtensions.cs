using System;
using StardewValley;

namespace Farmtronics {
	public static class ModDataExtensions {
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
		
		public static T GetModData<T>(this ModDataDictionary modData) where T : IModData {
			T model = default;
			
			if (typeof(T) == typeof(Bot.ModData)) {
				(model as Bot.ModData).IsBot  		  = int.Parse(modData[$"{UniqueID}/{Bot.ModData.IS_BOT}"]) == 1;
				(model as Bot.ModData).Name   		  = modData[$"{UniqueID}/{Bot.ModData.NAME}"];
				(model as Bot.ModData).Energy 		  = int.Parse(modData[$"{UniqueID}/{Bot.ModData.ENERGY}"]);
				(model as Bot.ModData).FacingDirection = int.Parse(modData[$"{UniqueID}/{Bot.ModData.FACING}"]);
					
				return model;
			}
			else {
				throw new InvalidOperationException("Couldn't find a matching ModData type.");
			}
		}
	}
}
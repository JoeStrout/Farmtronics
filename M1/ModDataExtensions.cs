using System;
using StardewValley;

namespace Farmtronics {
	public static class ModDataExtensions {
		public static string UniqueID;
		
		public static void SetModData<T>(this ModDataDictionary modData, T model) where T : IModData {
			switch (model) {
				case BotModData botModel:
					modData[$"{UniqueID}/{BotModData.IS_BOT}"] = botModel.IsBot ? "1" : "0";
					modData[$"{UniqueID}/{BotModData.NAME}"]   = botModel.Name;
					modData[$"{UniqueID}/{BotModData.ENERGY}"] = botModel.Energy.ToString();
					modData[$"{UniqueID}/{BotModData.FACING}"] = botModel.FacingDirection.ToString();
					break;
				
				default:
					throw new InvalidOperationException("Couldn't find a matching ModData type.");
			}
		}
		
		public static bool HasModData<T>(this ModDataDictionary modData) where T : IModData {
			if (typeof(T) == typeof(BotModData)) {
				if (!(modData.ContainsKey($"{UniqueID}/{BotModData.IS_BOT}")
						&& modData.ContainsKey($"{UniqueID}/{BotModData.NAME}")
						&& modData.ContainsKey($"{UniqueID}/{BotModData.ENERGY}")
						&& modData.ContainsKey($"{UniqueID}/{BotModData.FACING}"))) {
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
			
			if (typeof(T) == typeof(BotModData)) {
				(model as BotModData).IsBot  		  = int.Parse(modData[$"{UniqueID}/{BotModData.IS_BOT}"]) == 1;
				(model as BotModData).Name   		  = modData[$"{UniqueID}/{BotModData.NAME}"];
				(model as BotModData).Energy 		  = int.Parse(modData[$"{UniqueID}/{BotModData.ENERGY}"]);
				(model as BotModData).FacingDirection = int.Parse(modData[$"{UniqueID}/{BotModData.FACING}"]);
					
				return model;
			}
			else {
				throw new InvalidOperationException("Couldn't find a matching ModData type.");
			}
		}
	}
}
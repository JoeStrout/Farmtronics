/*
	This module contains utilities and extension methods to make working
	with mod (either StardewValley or SMAPI) classes easier.

*/
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Network;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;

namespace Farmtronics {
	public static class ModUtils {


		public static int GetInt(this ModDataDictionary d, string key, int defaultValue=0) {
			int result = defaultValue;
			string strVal;
			if (d.TryGetValue(key, out strVal)) int.TryParse(strVal, out result);
			return result;
		}

		public static bool GetBool(this ModDataDictionary d, string key, bool defaultValue=false) {
			string strVal = null;
			if (d.TryGetValue(key, out strVal)) {
				return strVal == "1" || strVal == "T";
			}
			return defaultValue;
		}

		public static string GetString(this ModDataDictionary d, string key, string defaultValue=null) {
			string result = defaultValue;
			d.TryGetValue(key, out result);
			return result;
		}

	}
}

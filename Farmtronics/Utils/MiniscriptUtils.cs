using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Miniscript;

namespace Farmtronics.Utils {
	static class MiniscriptUtils {

		public static ValString str_x = new ValString("x");
		public static ValString str_y = new ValString("y");

		public static Value ToValue(this List<string> strings) {
			if (strings == null) return null;
			var result = new ValList();
			foreach (string name in strings) {
				result.values.Add(new ValString(name));
			}
			return result;
		}

		public static Value ToValue(this List<float> floats) {
			if (floats == null) return null;
			var result = new ValList();
			foreach (float f in floats) {
				if (f == 0) result.values.Add(ValNumber.zero);
				else if (f == 1) result.values.Add(ValNumber.one);
				else result.values.Add(new ValNumber(f));
			}
			return result;
		}

		/*
		public static Value GrfonToValue(string grfonData) {
			GrfonDeserializer des = new GrfonDeserializer(new GrfonStringInput(grfonData));
			GrfonNode result = des.Parse();
			return GrfonToValue(result);
		}

		public static Value GrfonToValue(GrfonNode node) {
			if (node is GrfonCollection) {
				var coll = node as GrfonCollection;
				if (coll.KeyCount > 0) {
					var map = new ValMap();
					foreach (var key in coll.Keys) {
						map[key.ToString()] = GrfonToValue(coll[key]);
					}
					int i = 0;
					foreach (var unkeyedItem in coll) {
						map.map[new ValNumber(i++)] = GrfonToValue(unkeyedItem);
					}
					return map;
				} else {
					var list = new ValList();
					int i = 0;
					foreach (var unkeyedItem in coll) {
						list.values.Add(GrfonToValue(unkeyedItem));
					}
					return list;
				}
			}
			string s = node.ToString();
			if (s == "true" || s == "1") return ValNumber.one;
			if (s == "false" || s == "0") return ValNumber.zero;
			double d;
			if (double.TryParse(s, out d)) return new ValNumber(d);
			return new ValString(s);
		}
		*/

		public static List<string> ToStrings(this Value value) {
			if (value is ValList) {
				var result = new List<string>(((ValList)value).values.Count);
				foreach (var val in ((ValList)value).values) result.Add(val.ToString());
				return result;
			} else {
				return new List<string>(value.ToString().Split(new string[] { "\r\n", "\n", "\r" },
					System.StringSplitOptions.None));
			}
		}

		public static int ToInt(Value value, int defaultValue = 0) {
			if (value == null) return defaultValue;
			return value.IntValue();
		}

		public static bool ToBool(Value value, bool defaultValue = false) {
			if (value == null) return defaultValue;
			return value.BoolValue();
		}

		public static Vector2 ToVector2(this Value item) {
			Vector2 pos = Vector2.Zero;
			if (item is ValList) {
				var itemVals = ((ValList)item).values;
				if (itemVals.Count > 0) pos.X = itemVals[0].FloatValue();
				if (itemVals.Count > 1) pos.Y = itemVals[1].FloatValue();
			} else if (item is ValMap) {
				ValMap map = (ValMap)item;
				pos.X = map.Lookup(str_x).FloatValue();
				pos.Y = map.Lookup(str_y).FloatValue();
			} else if (item != null) {
				pos.X = item.FloatValue();
			}
			return pos;
		}

		public static List<Vector2> ToVector2List(this ValList value) {
			var result = new List<Vector2>(value.values.Count);
			for (int i = 0; i < value.values.Count; i++) {
				Value item = value.values[i];
				result.Add(item.ToVector2());
			}
			return result;
		}

		public static List<int> ToIntList(this ValList value) {
			var result = new List<int>(value.values.Count);
			for (int i = 0; i < value.values.Count; i++) {
				Value item = value.values[i];
				result.Add(item.IntValue());
			}
			return result;
		}

		public static List<float> ToFloatList(this ValList value) {
			var result = new List<float>(value.values.Count);
			for (int i = 0; i < value.values.Count; i++) {
				Value item = value.values[i];
				result.Add(item.FloatValue());
			}
			return result;
		}

		public static Value ToValue(this Vector2 v) {
			ValList item = new ValList();
			item.values.Add(new ValNumber(v.X));
			item.values.Add(new ValNumber(v.Y));
			return item;
		}

		public static ValList ToValue(this List<Vector2> vectors) {
			var result = new ValList();
			for (int i = 0; i < vectors.Count; i++) {
				Vector2 v = vectors[i];
				ValList item = new ValList();
				item.values.Add(new ValNumber(v.X));
				item.values.Add(new ValNumber(v.Y));
				result.values.Add(item);
			}
			return result;
		}

		public static string JoinToString(this Value value, string delimiter = "\n") {
			if (value == null) return null;
			if (value is ValList) {
				var result = new List<string>(((ValList)value).values.Count);
				foreach (var val in ((ValList)value).values) result.Add(val.ToString());
				return string.Join(delimiter, result.ToArray());
			} else {
				return value.ToString();
			}
		}

		public static float GetFloat(this ValMap map, string key, float defaultValue) {
			Value val = null;
			if (!map.TryGetValue(key, out val) || val == null) return defaultValue;
			return val.FloatValue();
		}

		public static double GetDouble(this ValMap map, string key, float defaultValue) {
			Value val = null;
			if (!map.TryGetValue(key, out val) || val == null) return defaultValue;
			return val.DoubleValue();
		}

		public static int GetInt(this ValMap map, string key, int defaultValue) {
			Value val = null;
			if (!map.TryGetValue(key, out val) || val == null) return defaultValue;
			return val.IntValue();
		}

		public static bool GetBool(this ValMap map, string key, bool defaultValue = false) {
			Value val = null;
			if (!map.TryGetValue(key, out val) || val == null) return defaultValue;
			return val.BoolValue();
		}

		public static string GetString(this ValMap map, string key, string defaultValue = null) {
			Value val = null;
			if (!map.TryGetValue(key, out val) || val == null) return defaultValue;
			return val.ToString();
		}

		public static ValMap GetMap(this ValMap map, string key, bool createIfNotFound = false) {
			Value val = null;
			if (!map.TryGetValue(key, out val) || !(map is ValMap)) {
				val = null;
				if (createIfNotFound) {
					val = new ValMap();
					map[key] = val;
				}
			}
			return (ValMap)val;
		}

		public static Vector2 GetVector2(this ValMap map, string key, Vector2 defValue = default(Vector2)) {
			Value val = null;
			if (!map.TryGetValue(key, out val)) return defValue;
			return val.ToVector2();
		}
	}	
}

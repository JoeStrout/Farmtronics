using Microsoft.Xna.Framework;
using System.Collections.Generic;

public static class ColorUtils {
	
	static Dictionary<string, Color> stringToColorMap = new Dictionary<string, Color>();
	
	public static Color ToColor(this uint i) {
		return new Color(
			(byte)((i >> 24) & 0xFF),
			(byte)((i >> 16) & 0xFF),
			(byte)((i >> 8) & 0xFF),
			(byte)(i & 0xFF));
	}
	
	public static Color ToColor(this string s) {
		if (string.IsNullOrEmpty(s)) return new Color(0,0,0,0);
		Color result;
		if (stringToColorMap.TryGetValue(s, out result)) return result;
		if (s[0] == '#' && (s.Length == 7 || s.Length == 9)) {
			var hexStyle = System.Globalization.NumberStyles.HexNumber;
			byte r = byte.Parse(s.Substring(1,2), hexStyle);
			byte g = byte.Parse(s.Substring(3,2), hexStyle);
			byte b = byte.Parse(s.Substring(5,2), hexStyle);
			byte a = 255;
			if (s.Length == 9) a = byte.Parse(s.Substring(7,2), hexStyle);
			result = new Color(r, g, b, a);
			stringToColorMap[s] = result;
			return result;
		}
		return default(Color);
	}
	
	
	public static uint ToUInt(this Color c) {
		return ((uint)c.R << 24) | ((uint)c.G << 16) | ((uint)c.B << 8) | ((uint)c.A);
	}
	
	public static string ToString(this Color c) {
		return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", c.R, c.G, c.B, c.A);
	}
	
	public static string ToHexString(this Color c) {
		return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", c.R, c.G, c.B, c.A);
	}
	
	public static bool IsEqualTo(this Color a, Color b) {
		return a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.B;
	}
	
	public static Color Times(this Color a, Color b) {
		return new Color(
			(byte)((int)a.R * b.R / 255), 
			(byte)((int)a.G * b.G / 255),
			(byte)((int)a.B * b.B / 255), 
			(byte)((int)a.A * b.A / 255));
	}
	
	public static void MultiplyBy(ref this Color a, Color b) {
		a.R = (byte)((int)a.R * b.R / 255);
		a.G = (byte)((int)a.G * b.G / 255);
		a.B = (byte)((int)a.B * b.B / 255);
		a.A = (byte)((int)a.A * b.A / 255);
	}

	public static Color Lerp(Color a, Color b, float t) {
		float ta = 1f - t;
		float tb = t;
		return new Color(
			(byte)((int)a.R * ta + b.R * tb), 
			(byte)((int)a.G * ta + b.G * tb),
			(byte)((int)a.B * ta + b.B * tb), 
			(byte)((int)a.A * ta + b.A * tb));
	}
}

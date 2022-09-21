using System.IO;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace Farmtronics {
	// Contains required assets
	static class Assets {
		public static Texture2D BotSprites    {get; private set;}
		public static Texture2D ScreenOverlay {get; private set;}
		public static Texture2D FontAtlas     {get; private set;}
		public static string[]  FontList      {get; private set;}
		
		
		public static void Initialize(IModHelper helper) {
			BotSprites    = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "BotSprites.png"));
			ScreenOverlay = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "ScreenOverlay.png"));
			FontAtlas 	  = helper.ModContent.Load<Texture2D>(Path.Combine("assets", "fontAtlas.png"));
			FontList	  = System.IO.File.ReadAllLines(Path.Combine(helper.DirectoryPath, "assets", "fontList.txt"));
		}
	}
}
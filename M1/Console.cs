using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;

namespace M1 {
	public class Console : IClickableMenu {
		public new const int width = 800;
		public new const int height = 640;

		int drawScale = 4;
		Rectangle screenArea;	// "work area" of the actual TV screen, in dialog coordinates

		Texture2D[] screenLayers;	// 0=background (border); 1=work area background; 2=overlay
		Rectangle screenSrcR;

		Texture2D fontAtlas;
		Dictionary<char, int> fontCharToIndex;

		public Console()
		: base(Game1.uiViewport.Width/2 - width/2, Game1.uiViewport.Height/2 - height/2, width, height) {
			Game1.player.Halt();

			screenArea = new Rectangle(20*drawScale, 18*drawScale, 160*drawScale, 120*drawScale);	// 640x480 (VGA)!

			screenLayers = new Texture2D[3];
			screenLayers[0] = ModEntry.helper.Content.Load<Texture2D>("assets/ScreenBorder.png");
			screenLayers[1] = ModEntry.helper.Content.Load<Texture2D>("assets/ScreenWorkArea.png");
			screenLayers[2] = ModEntry.helper.Content.Load<Texture2D>("assets/ScreenOverlay.png");
			screenSrcR = new Rectangle(0, 0, 200, 160);

			fontAtlas = ModEntry.helper.Content.Load<Texture2D>("assets/fontAtlas.png");

			string modPath = ModEntry.instance.Helper.DirectoryPath;
			string[] lines = System.IO.File.ReadAllLines(Path.Combine(modPath, "assets", "fontList.txt"));
			ModEntry.instance.print($"read {lines.Length} lines from fontList, starting with {lines[0]}");

			// First line just defines the atlas cell size... ignoring that for now...
			fontCharToIndex = new Dictionary<char, int>();
			for (int i=1; i<lines.Length; i++) {
				// Subsequent lines have the Unicode code point, then a Tab, then the character in UTF-8.
				// We really only need the character.
				int tabPos = lines[i].IndexOf('\t');
				if (tabPos >= 0) fontCharToIndex[lines[i][tabPos+1]] = i-1;
			}
		}

		private void Exit() {
			Game1.exitActiveMenu();
			Game1.player.canMove = true;
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true) {
		}

		public override void receiveRightClick(int x, int y, bool playSound = true) {
		}

		float Clamp(float x, float min, float max) {
			if (x < min) return min;
			if (x > max) return max;
			return x;
		}

		public override void performHoverAction(int x, int y) {
		}

		public override void receiveKeyPress(Keys key) {
			if (key == Keys.Escape) Exit();
		}

		public override void update(GameTime time) {
			base.update(time);
		}
		
		void DrawInDialogCoords(SpriteBatch b, Texture2D srcTex, Rectangle srcRect, float left, float top, float layerDepth=0.95f) {
			b.Draw(srcTex, new Vector2(xPositionOnScreen + left, yPositionOnScreen + top), srcRect, Color.White,
				0, Vector2.Zero, drawScale, SpriteEffects.None, layerDepth);
		}

		void DrawInDialogCoords(SpriteBatch b, Texture2D srcTex, Rectangle srcRect, Vector2 topLeft, float layerDepth=0.95f) {
			DrawInDialogCoords(b, srcTex, srcRect, topLeft.X, topLeft.Y, layerDepth);
		}

		Rectangle AtlasRectForIndex(int index) {
			int col = index % 32;
			int row = index / 32;
			return new Rectangle(col*10, row*14, 10, 14);
		}

		void DrawFontCell(SpriteBatch b, int atlasIndex, int screenCol, int screenRow, float layerDepth=0.95f) {
			var srcR = AtlasRectForIndex(atlasIndex);

			float drawX = xPositionOnScreen + screenArea.Left + screenCol * 16 + 1;
			float drawY= yPositionOnScreen + screenArea.Bottom - screenRow * 24 - 31; 
			
			b.Draw(fontAtlas, new Vector2(drawX, drawY), srcR, Color.White,
				0, Vector2.Zero, 2, SpriteEffects.None, layerDepth);
		}

		bool DrawFontCell(SpriteBatch b, char charToDraw, int screenCol, int screenRow, float layerDepth=0.95f) {
			if (charToDraw == ' ' || charToDraw == 0) return true;
			int atlasIndex = -1;
			if (!fontCharToIndex.TryGetValue(charToDraw, out atlasIndex)) {
				// Char not found; skip control characters, but draw the [?] char for anything else.
				if (charToDraw < ' ') return false;
				atlasIndex = 5;	// index of [?] character
			}

			DrawFontCell(b, atlasIndex, screenCol, screenRow, layerDepth);
			return true;
		}

		void DrawStringAt(SpriteBatch b, string s, int col, int row) {
			foreach (char c in s) {
				if (DrawFontCell(b, c, col, row)) {
					col++;
					if (col >= 40) {
						col = 0;
						row--;
					}
				}
			}
		}

		public override void draw(SpriteBatch b) {
			// fade out the background
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

			// draw the screen background
			b.Draw(screenLayers[0], new Vector2(xPositionOnScreen , yPositionOnScreen), screenSrcR,
				Color.CornflowerBlue,
				0, Vector2.Zero, drawScale, SpriteEffects.None, 0.5f);
			b.Draw(screenLayers[1], new Vector2(xPositionOnScreen , yPositionOnScreen), screenSrcR,
				Color.DeepSkyBlue,
				0, Vector2.Zero, drawScale, SpriteEffects.None, 0.5f);

			// draw content
			DrawStringAt(b, "MiniScript M-1 Ready!", 0, 19);
			for (int row=0; row<19; row++) {
				DrawStringAt(b, $"Row {row}", 0, row);
				DrawStringAt(b, "-->", 37, row);
			}

			// draw bezel/shine on top
			b.Draw(screenLayers[2], new Vector2(xPositionOnScreen , yPositionOnScreen), screenSrcR,
				Color.White,
				0, Vector2.Zero, drawScale, SpriteEffects.None, 0.5f);

		}

	}
}

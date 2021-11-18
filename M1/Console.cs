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

		TextDisplay textDisplay;

		public Console()
		: base(Game1.uiViewport.Width/2 - width/2, Game1.uiViewport.Height/2 - height/2, width, height) {
			Game1.player.Halt();

			screenArea = new Rectangle(20*drawScale, 18*drawScale, 160*drawScale, 120*drawScale);	// 640x480 (VGA)!

			screenLayers = new Texture2D[3];
			screenLayers[0] = ModEntry.helper.Content.Load<Texture2D>("assets/ScreenBorder.png");
			screenLayers[1] = ModEntry.helper.Content.Load<Texture2D>("assets/ScreenWorkArea.png");
			screenLayers[2] = ModEntry.helper.Content.Load<Texture2D>("assets/ScreenOverlay.png");
			screenSrcR = new Rectangle(0, 0, 200, 160);

			textDisplay = new TextDisplay();
			textDisplay.backColor = new Color(0.31f, 0.11f, 0.86f);
			textDisplay.SetCursor(19, 0);
			textDisplay.PrintLine(" **** MiniScript M-1 Home Computer ****");
			textDisplay.NextLine();
			textDisplay.PrintLine("Ready.");
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
			textDisplay.Update(time);
		}
		
		void DrawInDialogCoords(SpriteBatch b, Texture2D srcTex, Rectangle srcRect, float left, float top, float layerDepth=0.95f) {
			b.Draw(srcTex, new Vector2(xPositionOnScreen + left, yPositionOnScreen + top), srcRect, Color.White,
				0, Vector2.Zero, drawScale, SpriteEffects.None, layerDepth);
		}

		void DrawInDialogCoords(SpriteBatch b, Texture2D srcTex, Rectangle srcRect, Vector2 topLeft, float layerDepth=0.95f) {
			DrawInDialogCoords(b, srcTex, srcRect, topLeft.X, topLeft.Y, layerDepth);
		}


	
		public override void draw(SpriteBatch b) {
			// fade out the background
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

			// draw the screen background
			
			b.Draw(screenLayers[0], new Vector2(xPositionOnScreen , yPositionOnScreen), screenSrcR,
				new Color(0.64f, 0.57f, 0.98f),
				0, Vector2.Zero, drawScale, SpriteEffects.None, 0.5f);
			b.Draw(screenLayers[1], new Vector2(xPositionOnScreen , yPositionOnScreen), screenSrcR,
				textDisplay.backColor,
				0, Vector2.Zero, drawScale, SpriteEffects.None, 0.5f);
			
			// draw content
			Rectangle displayArea = new Rectangle(xPositionOnScreen + screenArea.Left,
				yPositionOnScreen + screenArea.Top, screenArea.Width, screenArea.Height);
			textDisplay.Render(b, displayArea);

			
			// draw bezel/shine on top
			b.Draw(screenLayers[2], new Vector2(xPositionOnScreen , yPositionOnScreen), screenSrcR,
				Color.White,
				0, Vector2.Zero, drawScale, SpriteEffects.None, 0.5f);
			
		}

	}
}

/*
This script represents the UI that appears when you activate (open) a bot.
It has several parts:
	1. The player inventory  (managed by the base class, MenuWithInventory)
	2. The bot inventory
	3. A MiniScript console.
*/
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace Farmtronics.Bot {
	class UIMenu : MenuWithInventory {

		BotObject bot;
		InventoryMenu botInventoryMenu;

		int consoleLeft, consoleTop;
		const int consoleHeight = 480;
		const int consoleWidth = 640;

		bool isDragging = false;
		Vector2 lastDragPos;

		static Vector2 preferredPosition;
		static Vector2 lastGameviewSize;

		public UIMenu(BotObject bot) : base(okButton: true, trashCan: true) {
			//print($"Created BotUIMenu for {bot.Name}. PositionOnScreen:{xPositionOnScreen},{yPositionOnScreen}; viewport:{Game1.uiViewport.Width}, {Game1.uiViewport.Height}, shell {bot.shell}");

			this.bot = bot;

			// Layout notes:
			// The position of a Menu is its top-left corner (ignoring the border), in game viewport coordinates.
			// Our height is determined by the sum of the console height (with bot inventory and status next to it),
			// and the player inventory height.
			int playerInvHeight = 224;		// (estimate for size of player UI)
			int totalHeight = playerInvHeight + consoleHeight;

			int widthOfTopStuff = consoleWidth + 220;	// (includes estimate for size of bot UI, plus some padding)

			consoleTop = Game1.uiViewport.Height/2 - totalHeight/2;
			if (consoleTop < 40) consoleTop = 40;	// (empirical fudge factor)
			consoleLeft = Game1.uiViewport.Width/2 + widthOfTopStuff/2 - consoleWidth;

			// Player inventory position is weird.  MenuWithInventory.cs says:
			int yPositionForInventory = yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth + 192 - 16; // + inventoryYOffset;
			// So:
			int playerInvYDelta = consoleTop + totalHeight - playerInvHeight - yPositionForInventory;

			//print($"playerInvHeight:{playerInvHeight}, consoleTop:{consoleTop} (={Game1.uiViewport.Height/2}-{totalHeight/2}), new yPositionOnScreen:{totalHeight - playerInvHeight - yPositionForInventory}");

			movePosition(0, playerInvYDelta);	// (adjust position of player UI)

			botInventoryMenu = new InventoryMenu(Game1.uiViewport.Width/2 - widthOfTopStuff/2, consoleTop, playerInventory: false, bot.inventory,
				capacity: bot.GetActualCapacity(), rows: 6);
			botInventoryMenu.onAddItem += onAddItem;

			trashCan.myID = 106;
			botInventoryMenu.populateClickableComponentList();
			for (int k = 0; k < botInventoryMenu.inventory.Count; k++) {
				if (botInventoryMenu.inventory[k] != null) {
					botInventoryMenu.inventory[k].myID += 53910;
					botInventoryMenu.inventory[k].upNeighborID += 53910;
					botInventoryMenu.inventory[k].rightNeighborID += 53910;
					botInventoryMenu.inventory[k].downNeighborID = ClickableComponent.CUSTOM_SNAP_BEHAVIOR;
					botInventoryMenu.inventory[k].leftNeighborID += 53910;
					botInventoryMenu.inventory[k].fullyImmutable = true;
					if (k % (botInventoryMenu.capacity / botInventoryMenu.rows) == 0) {
						botInventoryMenu.inventory[k].leftNeighborID = dropItemInvisibleButton.myID;
					}
					if (k % (botInventoryMenu.capacity / botInventoryMenu.rows) == botInventoryMenu.capacity / botInventoryMenu.rows - 1) {
						botInventoryMenu.inventory[k].rightNeighborID = trashCan.myID;
					}
				}
			}

			bot.shell.console.RemoveFrameAndPositionAt(consoleLeft + 53, consoleTop + 34);	// (empirical fudge values)

			// Apply stored position preference, if any and if game view is the same size as it was then:
			if (preferredPosition != default(Vector2) && lastGameviewSize.X == Game1.uiViewport.Width && lastGameviewSize.Y == Game1.uiViewport.Height) {
				shift((int)(preferredPosition.X - xPositionOnScreen), (int)(preferredPosition.Y - yPositionOnScreen));
			}
		}

		public override void update(GameTime time) {
			if (isDragging) {
				if (!ModEntry.instance.Helper.Input.IsDown(SButton.MouseLeft)) {
					isDragging = false;
				} else {
					var cursor = ModEntry.instance.Helper.Input.GetCursorPosition();
					Vector2 pos = cursor.GetScaledScreenPixels();
					int dx = (int)(pos.X - lastDragPos.X);
					int dy = (int)(pos.Y - lastDragPos.Y);
					shift(dx, dy);
					lastDragPos = pos;
				}
			}

			base.update(time);
			botInventoryMenu.update(time);
		}

		void shift(int dx, int dy) {
			if (dx == 0 && dy == 0) return;
			movePosition(dx, dy);
			botInventoryMenu.movePosition(dx, dy);
			bot.shell.console.movePosition(dx, dy);
			consoleLeft += dx;  consoleTop += dy;
			preferredPosition = new Vector2(xPositionOnScreen, yPositionOnScreen);
			lastGameviewSize = new Vector2(Game1.uiViewport.Width, Game1.uiViewport.Height);
		}

		public override void receiveKeyPress(Keys key) {
			if (key == Keys.Escape && heldItem != null) {
				// No closing the menu while holding an item (issue #26)
				return;
            }
			bot.shell.console.receiveKeyPress(key);
			if (key == Keys.Delete && heldItem != null && heldItem.canBeTrashed()) {
				Utility.trashItem(heldItem);
				heldItem = null;
			}
		}
		
		private void DoHatAction() {
			if (!Context.IsMultiplayer) return;
			
			// Update the inventory
			bot.data.Update();
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true) {
			bot.shell.console.receiveLeftClick(x, y, playSound);

			// ModEntry.instance.Monitor.Log($"Bot.receiveLeftClick({x}, {y}, {playSound}) while heldItem={heldItem}; inDragArea={inDragArea(x,y)}");
			int slot = botInventoryMenu.getInventoryPositionOfClick(x, y);
			bool checkHat = slot == bot.GetActualCapacity() - 1;
			// ModEntry.instance.Monitor.Log($"Bot.receiveLeftClick: slot={slot}");
			base.receiveLeftClick(x, y, playSound);
			heldItem = botInventoryMenu.leftClick(x, y, heldItem, false);
			
			if (checkHat && heldItem is Hat) DoHatAction();
			
			// ModEntry.instance.Monitor.Log($"after calling botInventoryMenu.leftClick, heldItem = {heldItem}");

			if (heldItem == null && inDragArea(x,y)) {
				var cursor = ModEntry.instance.Helper.Input.GetCursorPosition();
				lastDragPos = cursor.GetScaledScreenPixels();
				isDragging = true;
			}
		}

		public override void receiveRightClick(int x, int y, bool playSound = true) {
			// ModEntry.instance.Monitor.Log($"Bot.receiveRightClick({x}, {y}, {playSound})");
			int slot = botInventoryMenu.getInventoryPositionOfClick(x, y);
			bool checkHat = slot == bot.GetActualCapacity() -1;
			base.receiveRightClick(x, y, playSound);
			heldItem = botInventoryMenu.rightClick(x, y, heldItem, playSound);
			
			if (checkHat && heldItem is Hat) DoHatAction();

			// ModEntry.instance.Monitor.Log($"after calling botInventoryMenu.rightClick, heldItem = {heldItem}");
		}

		// Invoked by InventoryMenu.leftClick when an item is dropped in an inventory slot.
		void onAddItem(Item item, Farmer who) {
			//ModEntry.instance.Monitor.Log($"Bot.onAddItem({item}, {who}");
			// Note: bot inventory has already been added, so we don't really need this.
			DoHatAction();
		}

		public override void performHoverAction(int x, int y) {
			hoveredItem = null;
			hoverText = "";
			base.performHoverAction(x, y);

			Item item_grab_hovered_item = botInventoryMenu.hover(x, y, heldItem);
			if (item_grab_hovered_item != null)	{
				hoveredItem = item_grab_hovered_item;
				//ModEntry.instance.Monitor.Log($"hoveredItem = {hoveredItem}");
			}
		}

		/// <summary>
		/// Return whether the given screen position is in our draggable area.
		/// </summary>
		bool inDragArea(int x, int y) {
			// ModEntry.instance.Monitor.Log($"inDragArea: x={x} y={y}, in botInventoryBounds: {botInventoryBounds().Contains(x, y)}, in consoleBounds: {consoleBounds().Contains(x, y)}");
			if (botInventoryBounds().Contains(x,y)) return false;
			if (consoleBounds().Contains(x,y)) return false;

			var playerInv = inventory;
			Rectangle invRect = new Rectangle(playerInv.xPositionOnScreen, playerInv.yPositionOnScreen, width, height);
			if (invRect.Contains(x,y)) return false;

			return true;
		}

		Rectangle botInventoryBounds() {
			return new Rectangle(
				botInventoryMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder,
				botInventoryMenu.yPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder,
				botInventoryMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2,
				botInventoryMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth * 2);
		}

		Rectangle consoleBounds() {
			int drawWidth = consoleWidth + 60;		// weird fudge values found empircally
			int drawHeight = consoleHeight + 124;	// (not obviously related to borderWidth, spaceToClearTopBorder, etc.)
			return new Rectangle(
				consoleLeft - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder,
				consoleTop - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder,
				drawWidth,
				drawHeight);
		}

		public override void draw(SpriteBatch b) {
			// darken the background
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

			base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);

			// draw the bot inventory
			Rectangle invR = botInventoryBounds();
			Game1.drawDialogueBox(invR.X, invR.Y, invR.Width, invR.Height, speaker: false, drawOnlyBox: true);
			botInventoryMenu.draw(b);

			// highlight the currently selected slot
			Vector2 slotPos = botInventoryMenu.GetSlotDrawPositions()[bot.currentToolIndex];
			b.Draw(Game1.menuTexture, slotPos, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 56), Color.Gray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.5f);

			// draw the console
			Rectangle consoleR = consoleBounds();
			Game1.drawDialogueBox(consoleR.X, consoleR.Y, consoleR.Width, consoleR.Height, speaker: false, drawOnlyBox: true);
			bot.shell.console.draw(b);

/*
			if (poof != null)
			{
				poof.draw(b, localPosition: true);
			}
			foreach (TransferredItemSprite transferredItemSprite in _transferredItemSprites)
			{
				transferredItemSprite.Draw(b);
			}
*/
			if (hoverText != null && hoveredItem == null) {
				if (hoverAmount > 0) {
					IClickableMenu.drawToolTip(b, hoverText, "", null, heldItem: true, -1, 0, -1, -1, null, hoverAmount);
				} else {
					IClickableMenu.drawHoverText(b, hoverText, Game1.smallFont);
				}
			}
			if (hoveredItem != null) {
				IClickableMenu.drawToolTip(b, hoveredItem.getDescription(), hoveredItem.DisplayName, hoveredItem, heldItem != null);
			} else if (hoveredItem != null && botInventoryMenu != null) {
				IClickableMenu.drawToolTip(b, botInventoryMenu.descriptionText, botInventoryMenu.descriptionTitle, hoveredItem, heldItem != null);
			}
			if (heldItem != null) {
				heldItem.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);
			}
			Game1.mouseCursorTransparency = 1f;
			drawMouse(b);
		}
	}
}
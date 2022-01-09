/*
This script represents the UI that appears when you activate (open) a bot.
It has several parts:
	1. The player inventory  (managed by the base class, MenuWithInventory)
	2. The bot inventory
	3. A MiniScript console.
*/
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Farmtronics {
	public class BotUIMenu : MenuWithInventory {

		Bot bot;
		InventoryMenu botInventoryMenu;

		//Shell shell;

		int consoleLeft, consoleTop;
		const int consoleHeight = 480;
		const int consoleWidth = 640;

		public BotUIMenu(Bot bot, Shell shell)
		: base(null, okButton: true, trashCan: true) {
			print($"Created BotUIMenu. PositionOnScreen:{xPositionOnScreen},{yPositionOnScreen}; viewport:{Game1.uiViewport.Width}, {Game1.uiViewport.Height} ");

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

			print($"playerInvHeight:{playerInvHeight}, consoleTop:{consoleTop} (={Game1.uiViewport.Height/2}-{totalHeight/2}), new yPositionOnScreen:{totalHeight - playerInvHeight - yPositionForInventory}");
			movePosition(0, playerInvYDelta);	// (adjust position of player UI)

			botInventoryMenu = new InventoryMenu(Game1.uiViewport.Width/2 - widthOfTopStuff/2, consoleTop, playerInventory: false, bot.inventory,
				capacity:bot.GetActualCapacity(), rows:6);
			botInventoryMenu.onAddItem = onAddItem;

			trashCan.myID = 106;
			botInventoryMenu.populateClickableComponentList();
			for (int k = 0; k < botInventoryMenu.inventory.Count; k++) {
				if (botInventoryMenu.inventory[k] != null) {
					botInventoryMenu.inventory[k].myID += 53910;
					botInventoryMenu.inventory[k].upNeighborID += 53910;
					botInventoryMenu.inventory[k].rightNeighborID += 53910;
					botInventoryMenu.inventory[k].downNeighborID = -7777;
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
		}

		static void print(string s) {
			ModEntry.instance.print(s);
		}

		public override void update(GameTime time) {
			base.update(time);
			botInventoryMenu.update(time);
		}

		public override void receiveKeyPress(Keys key) {
			bot.shell.console.receiveKeyPress(key);
			if (key == Keys.Delete && heldItem != null && heldItem.canBeTrashed()) {
				Utility.trashItem(heldItem);
				heldItem = null;
			}
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true) {
			Debug.Log($"Bot.receiveLeftClick({x}, {y}, {playSound}) while heldItem={heldItem}");
			//int slot = botInventoryMenu.getInventoryPositionOfClick(x, y);
			//Debug.Log($"Bot.receiveLeftClick: slot={slot}");
			base.receiveLeftClick(x, y, playSound);
			heldItem = botInventoryMenu.leftClick(x, y, heldItem, false);
			
			Debug.Log($"after calling botInventoryMenu.leftClick, heldItem = {heldItem}");
		}

		public override void receiveRightClick(int x, int y, bool playSound = true) {
			Debug.Log($"Bot.receiveRightClick({x}, {y}, {playSound})");
			base.receiveRightClick(x, y, playSound);
		}

		// Invoked by InventoryMenu.leftClick when an item is dropped in an inventory slot.
		void onAddItem(Item item, Farmer who) {
			Debug.Log($"Bot.onAddItem({item}, {who}");
			// Note: bot inventory has already been added, so we don't really need this.
		}

		public override void performHoverAction(int x, int y) {
			hoveredItem = null;
			hoverText = "";
			base.performHoverAction(x, y);

			Item item_grab_hovered_item = botInventoryMenu.hover(x, y, heldItem);
			if (item_grab_hovered_item != null)	{
				hoveredItem = item_grab_hovered_item;
				//Debug.Log($"hoveredItem = {hoveredItem}");
			}
		}

		public override void draw(SpriteBatch b) {
			// darken the background
			b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);

			base.draw(b, drawUpperPortion: false, drawDescriptionArea: false);

			// draw the bot inventory
			Game1.drawDialogueBox(
				botInventoryMenu.xPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder,
				botInventoryMenu.yPositionOnScreen - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder,
				botInventoryMenu.width + IClickableMenu.borderWidth * 2 + IClickableMenu.spaceToClearSideBorder * 2,
				botInventoryMenu.height + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth * 2,
				speaker: false, drawOnlyBox: true);
			botInventoryMenu.draw(b);

			// draw the console
			int drawWidth = consoleWidth + 60;		// weird fudge values found empircally
			int drawHeight = consoleHeight + 124;	// (not obviously related to borderWidth, spaceToClearTopBorder, etc.)
			Game1.drawDialogueBox(
				consoleLeft - IClickableMenu.borderWidth - IClickableMenu.spaceToClearSideBorder,
				consoleTop - IClickableMenu.borderWidth - IClickableMenu.spaceToClearTopBorder,
				drawWidth,
				drawHeight,
				speaker: false, drawOnlyBox: true);

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

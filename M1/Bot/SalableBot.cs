/*
This class represents a bot for sale in the shop.  When the bot is purchased,
it returns a new instance of the actual Bot class.
*/

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Farmtronics.Bot {
    public class SalableBot : ISalable {
        static Texture2D botSprites;

        public string DisplayName => Name;

        public string Name => _name;


        public string _name = "Bot";

        public int Stack {
            get { return 1; }
            set { }
        }

        public SalableBot() {
            if (botSprites == null) {
                botSprites = ModEntry.instance.Helper.ModContent.Load<Texture2D>(Path.Combine("assets", "BotSprites.png"));
            }
        }

        public bool actionWhenPurchased() {
            return false;
        }

        public int addToStack(Item stack) {
            return 1;
        }

        public bool CanBuyItem(Farmer farmer) {
            // Pointless right now but could be useful for multiplayer:
            // return farmer.mailRecieved.Contains("FarmtronicsFirstBotMail"); //Should work, haven't tested.
            return Game1.player.mailReceived.Contains("FarmtronicsFirstBotMail");
        }

        public bool canStackWith(ISalable other) {
            return false;
        }

        public void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow) {
            if (botSprites == null) {
                ModEntry.instance.Monitor.Log("Bot.drawInMenu: botSprites is null; bailing out");
                return;
            }

            bool shouldDrawStackNumber = ((drawStackNumber == StackDrawType.Draw && this.maximumStackSize() > 1 && this.Stack > 1)
                || drawStackNumber == StackDrawType.Draw_OneInclusive) && (double)scaleSize > 0.3 && this.Stack != int.MaxValue;

            Rectangle srcRect = new Rectangle(0, 112, 16, 16);
            spriteBatch.Draw(botSprites, location + new Vector2((int)(32f * scaleSize), (int)(32f * scaleSize)), srcRect, color * transparency, 0f,
                new Vector2(8f, 8f) * scaleSize, 4f * scaleSize, SpriteEffects.None, layerDepth);

            if (shouldDrawStackNumber) {
                var loc = location + new Vector2((float)(64 - Utility.getWidthOfTinyDigitString(this.Stack, 3f * scaleSize)) + 3f * scaleSize, 64f - 18f * scaleSize + 2f);
                Utility.drawTinyDigits(this.Stack, spriteBatch, loc, 3f * scaleSize, 1f, color);
            }
        }

        public string getDescription() {
            return "A programmable mechanical wonder.";
        }

        public ISalable GetSalableInstance() {
            // Create a new instance of the actual Bot class.
            return new BotObject(null);
        }

        public bool IsInfiniteStock() {
            return true;
        }

        public int maximumStackSize() {
            return 1;
        }

        public int salePrice() {
            return 50;
        }

        public bool ShouldDrawIcon() {
            return true;
        }
    }
}

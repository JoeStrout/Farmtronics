using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;


namespace Farmtronics
{
	public class ModEntry : Mod {
		public static ModEntry instance;

		Shell shell;
		uint prevTicks;

		public override void Entry(IModHelper helper) {
			instance = this;
			helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
			helper.Events.Display.MenuChanged += this.OnMenuChanged;
			helper.Events.GameLoop.UpdateTicking += UpdateTicking;
#if DEBUG
			// HACK not needed:
			helper.Events.Input.ButtonPressed += this.OnButtonPressed;
#endif
			helper.Events.GameLoop.Saving += this.OnSaving;
			helper.Events.GameLoop.Saved += this.OnSaved;
			helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
			helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
			
			this.Monitor.Log($"CurrentSavePath: {Constants.CurrentSavePath}");
		}


		private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e) {
			Bot.ClearAll();
			shell = null;
		}

		private void UpdateTicking(object sender, UpdateTickingEventArgs e) {
			uint dTicks = e.Ticks - prevTicks;
			var gameTime = new GameTime(new TimeSpan(e.Ticks * 10000000 / 60), new TimeSpan(dTicks * 10000000 / 60));
			Bot.UpdateAll(gameTime);
			prevTicks = e.Ticks;
		}

		// HACK used only for early testing/development:
		public void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
			//this.Monitor.Log($"OnButtonPressed: {e.Button}");
			if (e.Button == SButton.PageUp) {
				this.Monitor.Log($"CurrentSavePath: {Constants.CurrentSavePath}");
				// Create a bot.
				Vector2 pos = Game1.player.position;
				pos.X -= 64;
				Vector2 tilePos = new Vector2((int)(pos.X / 64), (int)(pos.Y / 64));
				var bot = new Bot(tilePos);

				//Game1.currentLocation.dropObject(bot, pos, Game1.viewport, true, (Farmer)null);
				Game1.player.currentLocation.overlayObjects[tilePos] = bot;
			}
		}
		

		public void OnMenuChanged(object sender, MenuChangedEventArgs e) {
			this.Monitor.Log($"Menu opened: {e.NewMenu}");
			if (e.NewMenu is LetterViewerMenu) {
				this.Monitor.Log("Hey hey, it's a LetterViewerMenu!");
				foreach (var msg in Game1.player.mailbox) {
					this.Monitor.Log($"Mail in mailbox: {msg}");
					if (msg == "FarmtronicsFirstBotMail") {
						this.Monitor.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
						Game1.player.recoveredItem = new Bot(null);
						break;
					}
				}
				return;
			}
			if (e.NewMenu is ShopMenu shop) {
				if (shop.portraitPerson != Game1.getCharacterFromName("Pierre")) return;
				if (Game1.player.mailReceived.Contains("FarmtronicsFirstBotMail")) {
					// Add a bot to the store inventory.
					// Let's insert it after Flooring but before Catalogue.
					int index = 0;
					for (; index < shop.forSale.Count; index++) {
						var item = shop.forSale[index];
						this.Monitor.Log($"Shop item {index}: {item} with {item.Name}");
						if (item.Name == "Catalogue" || (index>0 && shop.forSale[index-1].Name == "Flooring")) break;
					}
					var botForSale = new SalableBot();
					shop.forSale.Insert(index, botForSale);
					shop.itemPriceAndStock.Add(botForSale, new int[2] { 2500, int.MaxValue });	// sale price and available stock
				}
			}

			var dlog = e.NewMenu as DialogueBox;
			if (dlog == null) return;
			if (!dlog.isQuestion || dlog.responses[0].responseKey != "Weather") return;

			// TV menu: insert a new option for the Home Computer
			Response r = new Response("Farmtronics", "Farmtronics Home Computer");
			dlog.responses.Insert(dlog.responses.Count-1, r);
			// adjust the dialog height
			var h = SpriteText.getHeightOfString(r.responseText, dlog.width) + 16;
			dlog.heightForQuestions += h; dlog.height += h;
			// intercept the handler (but call the original one for other responses)
			var prevHandler = Game1.currentLocation.afterQuestion;
			Game1.currentLocation.afterQuestion = (who, whichAnswer) => {
				this.Monitor.Log($"{who} selected channel {whichAnswer}");
				if (whichAnswer == "Farmtronics") PresentComputer();
				else prevHandler(who, whichAnswer);
			};
		}

		public void OnSaving(object sender, SavingEventArgs args) {
			if (Context.IsMainPlayer) Bot.ConvertBotsToChests();
		}

		public void OnSaved(object sender, SavedEventArgs args) {
			if (Context.IsMainPlayer) Bot.ConvertChestsToBots();
		}

		public void OnSaveLoaded(object sender, SaveLoadedEventArgs args) {
			if (Context.IsMainPlayer) Bot.ConvertChestsToBots();
		}

		public void OnDayStarted(object sender, DayStartedEventArgs args) {
			this.Monitor.Log($"OnDayStarted");
			// Check whether we have our first-bot letter waiting in the mailbox.
			// If so, set the item to be "recovered" via the mail:
			foreach (var msg in Game1.player.mailbox) {
				this.Monitor.Log($"Mail in mailbox: {msg}");
				if (msg == "FarmtronicsFirstBotMail") {
					this.Monitor.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
					Game1.player.recoveredItem = new Bot(null);
					break;
				}
			}

			// Initialize the home computer and all bots for autostart.
			// This initialization will also cause all startup scripts to run.
			InitComputerShell();
			Bot.InitShellAll();
		}

		/// <summary>
		/// Initializes the home computer shell.
		/// Effectively boots up the home computer if it is not already running.
		/// </summary>
		private void InitComputerShell() {
			if (shell == null) {
				shell = new Shell();
				shell.Init();
			}
		}

		private void PresentComputer() {
			// Initialize the home computer if it is not already running, then present it.
			InitComputerShell();
			shell.console.Present();
		}

        /// <inheritdoc cref="IContentEvents.AssetRequested" />
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e) {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/mail")) {
                e.Edit(asset => {
                    this.Monitor.Log($"ModEntry.Edit(Mail)");
                    var data = asset.AsDictionary<string, string>().Data;
                    data["FarmtronicsFirstBotMail"] = "Dear @,"
                        + "^^Congratulations!  You have been selected to receive a complementary FARMTRONICS BOT, the latest in farm technology!"
                        + "^With this robotic companion, your days of toiling in the fields will soon be over."
                        + "^Check your local stores for additional bots as needed.  Enjoy!"
                        + "^^%item itemRecovery %%";
                    foreach (var msg in Game1.player.mailbox) {
                        this.Monitor.Log($"mail in mailbox: {msg}");
                        if (msg == "FarmtronicsFirstBotMail") {
                            this.Monitor.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
                            Game1.player.recoveredItem = new Bot(null);
                            break;
                        }
                    }
                });
            }
        }
    }
}

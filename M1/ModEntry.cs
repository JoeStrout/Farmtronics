﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;
using StardewValley.TerrainFeatures;


namespace Farmtronics
{
	public class ModEntry : Mod, IAssetEditor {
		public static IModHelper helper;
		public static ModEntry instance;

		Shell shell;

		public override void Entry(IModHelper helper) {
			instance = this;
			ModEntry.helper = helper;
			helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
			helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
			helper.Events.Display.MenuChanged += this.OnMenuChanged;
			helper.Events.GameLoop.UpdateTicking += UpdateTicking;
			//HACK not needed:
			helper.Events.Input.ButtonPressed += this.OnButtonPressed;
			helper.Events.GameLoop.Saving += this.OnSaving;
			helper.Events.GameLoop.Saved += this.OnSaved;
			helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
			helper.Events.GameLoop.DayStarted += this.OnDayStarted;

			print($"CurrentSavePath: {Constants.CurrentSavePath}");
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e) {
			Helper.Content.AssetEditors.Add(this);
		}

		private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e) {
			Bot.ClearAll();
		}

		uint prevTicks;
		private void UpdateTicking(object sender, UpdateTickingEventArgs e) {
			uint dTicks = e.Ticks - prevTicks;
			var gameTime = new GameTime(new TimeSpan(e.Ticks * 10000000 / 60), new TimeSpan(dTicks * 10000000 / 60));
			Bot.UpdateAll(gameTime);
			prevTicks = e.Ticks;
		}

		public void print(string s) {
            this.Monitor.Log(s, LogLevel.Debug);
		}

		// HACK used only for early testing/development:
		public void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
			//print($"OnButtonPressed: {e.Button}");
			if (e.Button == SButton.PageUp) {
				print($"CurrentSavePath: {Constants.CurrentSavePath}");
				// Create a bot.
				var player = Game1.player;
				var loc = player.currentLocation;
				var pos = player.position;
				Vector2 tilePos = new Vector2((int)(pos.X / 64) - 1, (int)(pos.Y / 64));
				var bot = new Bot(tileLocation: tilePos, location: loc, inventory: Bot.defaultInventories());

				//Game1.currentLocation.dropObject(bot, pos, Game1.viewport, true, (Farmer)null);
				Game1.player.currentLocation.overlayObjects[tilePos] = bot;
			}
		}
		

		public void OnMenuChanged(object sender, MenuChangedEventArgs e) {
			Debug.Log($"Menu opened: {e.NewMenu}");
			if (e.NewMenu is LetterViewerMenu) {
				Debug.Log("Hey hey, it's a LetterViewerMenu!");
				foreach (var msg in Game1.player.mailbox) {
					Debug.Log($"Mail in mailbox: {msg}");
					if (msg == "FarmtronicsFirstBotMail") {
						Debug.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
						Game1.player.recoveredItem = new Bot();
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
						Debug.Log($"Shop item {index}: {item} with {item.Name}");
						if (item.Name == "Catalogue" || (index>0 && shop.forSale[index-1].Name == "Flooring")) break;
					}
					var botForSale = new Bot();
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
				print($"{who} selected channel {whichAnswer}");
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
			Debug.Log($"OnDayStarted");
			// Check whether we have our first-bot letter waiting in the mailbox.
			// If so, set the item to be "recovered" via the mail:
			foreach (var msg in Game1.player.mailbox) {
				Debug.Log($"Mail in mailbox: {msg}");
				if (msg == "FarmtronicsFirstBotMail") {
					Debug.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
					Game1.player.recoveredItem = new Bot();
					break;
				}
			}
		}

		private void PresentComputer() {
			if (shell == null) {
				shell = new Shell();
				shell.Init();
			}
			shell.console.Present();

			/*
			var farm = (Farm)Game1.getLocationFromName("Farm");

			var layer = farm.map.Layers[0];
			shell.PrintLine($"Farm size: {layer.LayerWidth} x {layer.LayerHeight}");
			shell.PrintLine($"Farm animals: {farm.getAllFarmAnimals().Count}");
			shell.PrintLine($"Buildings: {farm.buildings.Count}");

			int featureCount = 0;
			int trees=0, bushes=0, grasses=0, hoeDirts=0, paths=0;
			var hoeLocs = new List<string>();
			foreach (KeyValuePair<Vector2, TerrainFeature> kvp in farm.terrainFeatures.Pairs) {
				if (kvp.Value is Tree) trees++;
				else if (kvp.Value is Bush) bushes++;
				else if (kvp.Value is Grass) grasses++;
				else if (kvp.Value is HoeDirt) {
					hoeDirts++;
					hoeLocs.Add(kvp.Key.ToString());	// locations are integers, X right and Y down from top-left
				}
				else if (kvp.Value is Flooring) paths++;
				featureCount++;
			}
			shell.PrintLine($"Trees: {trees}");
			shell.PrintLine($"Bushes: {bushes}");
			shell.PrintLine($"Grass: {grasses}");
			shell.PrintLine($"Tilled Ground: {hoeDirts}");// at: {string.Join(',', hoeLocs)}");
			shell.PrintLine($"Paved: {paths}");
			shell.PrintLine($"Total features: {featureCount}");
			*/
		}

		bool IAssetEditor.CanEdit<T>(IAssetInfo asset) {
			return asset.AssetNameEquals("Data\\mail");
		}

		void IAssetEditor.Edit<T>(IAssetData asset) {
			Debug.Log($"ModEntry.Edit(Mail)");
			var data = asset.AsDictionary<string, string>().Data;
			data["FarmtronicsFirstBotMail"] = "Dear @,"
				+ "^^Congratulations!  You have been selected to receive a complementary FARMTRONICS BOT, the latest in farm technology!"
				+ "^With this robotic companion, your days of toiling in the fields will soon be over."
				+ "^Check your local stores for additional bots as needed.  Enjoy!"
				+ "^^%item itemRecovery %%";
			foreach (var msg in Game1.player.mailbox) {
				Debug.Log($"mail in mailbox: {msg}");
				if (msg == "FarmtronicsFirstBotMail") {
					Debug.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
					Game1.player.recoveredItem = new Bot();
					break;
				}
			}

		}
	}
}

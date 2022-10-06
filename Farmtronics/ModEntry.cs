using System;
using System.IO;
using Farmtronics.Bot;
using Farmtronics.M1;
using Farmtronics.M1.Filesystem;
using Farmtronics.Multiplayer;
using Farmtronics.Utils;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;


namespace Farmtronics
{
	public class ModEntry : Mod {
		private static string MOD_ID;
		public static ModEntry instance;
		
		internal static RealFileDisk sysDisk;
		
		Shell shell;
		uint prevTicks;
		
		public static string GetModDataKey(string key) {
			return $"{MOD_ID}/{key}";
		}

		public override void Entry(IModHelper helper) {
			instance = this;
			MOD_ID = ModManifest.UniqueID;
			I18n.Init(helper.Translation);
			
#if DEBUG
			// HACK not needed:
			helper.Events.Input.ButtonPressed += this.OnButtonPressed;
#endif
			helper.Events.GameLoop.SaveCreated += this.OnSaveCreated;
			helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
			helper.Events.GameLoop.Saving += this.OnSaving;
			helper.Events.GameLoop.Saved += this.OnSaved;
			helper.Events.GameLoop.DayStarted += this.OnDayStarted;
			helper.Events.GameLoop.UpdateTicking += this.UpdateTicking;
			helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

			helper.Events.Display.MenuChanged += this.OnMenuChanged;		
            helper.Events.Content.AssetRequested += this.OnAssetRequested;

			helper.Events.Multiplayer.ModMessageReceived += MultiplayerManager.OnModMessageReceived;
			helper.Events.Multiplayer.PeerContextReceived += MultiplayerManager.OnPeerContextReceived;
			helper.Events.Multiplayer.PeerConnected += MultiplayerManager.OnPeerConnected;
			helper.Events.Multiplayer.PeerDisconnected += MultiplayerManager.OnPeerDisconnected;
			
			Assets.Initialize(helper);
			Monitor.Log($"Loaded fontAtlas with size {Assets.FontAtlas.Width}x{Assets.FontAtlas.Height}");
			Monitor.Log($"read {Assets.FontList.Length} lines from fontList, starting with {Assets.FontList[0]}");
			sysDisk = new RealFileDisk(Path.Combine(ModEntry.instance.Helper.DirectoryPath, "assets", "sysdisk"));
			sysDisk.readOnly = true;
		}

		private void OnSaveCreated(object sender, SaveCreatedEventArgs e) {
			this.Monitor.Log($"CurrentSavePath: {Constants.CurrentSavePath}");
			SaveData.CreateSaveDataDirs();
			SaveData.CreateUsrDisk(Game1.player.UniqueMultiplayerID);
		}

		private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e) {
			BotManager.ClearAll();
			MultiplayerManager.remoteComputer.Clear();
			DiskController.ClearInstances();
			BotManager.botCount = 0;
			shell = null;
		}

		private void UpdateTicking(object sender, UpdateTickingEventArgs e) {
			uint dTicks = e.Ticks - prevTicks;
			var gameTime = new GameTime(new TimeSpan(e.Ticks * 10000000 / 60), new TimeSpan(dTicks * 10000000 / 60));
			BotManager.UpdateAll(gameTime);
			prevTicks = e.Ticks;
		}
		
		// NOTE: Only check the mailbox once per day and only when the player warps to the farm
		//		 This prevents XML serialization errors
		private void OnPlayerWarped(object sender, WarpedEventArgs args) {
			if (!args.IsLocalPlayer || args.NewLocation is not Farm) return;
			
			// Check whether we have our first-bot letter waiting in the mailbox.
			// If so, set the item to be "recovered" via the mail:
			foreach (var msg in Game1.player.mailbox) {
				this.Monitor.Log($"Mail in mailbox: {msg}");
				if (msg == "FarmtronicsFirstBotMail") {
					this.Monitor.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
					var bot = new BotObject();
					bot.owner.Value = Game1.player.UniqueMultiplayerID;
					Game1.player.recoveredItem = bot;
					break;
				}
			}
			
			Helper.Events.Player.Warped -= OnPlayerWarped;
		}

#if DEBUG
		// HACK used only for early testing/development:
		public void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
			//this.Monitor.Log($"OnButtonPressed: {e.Button}");
			switch (e.Button) {
			case SButton.PageUp:
				// Create a bot.
				Vector2 pos = Game1.player.position;
				pos.X -= Game1.tileSize;
				Vector2 tilePos = pos.GetTilePosition();
				var bot = new BotObject(tilePos);
				bot.owner.Value = Game1.player.UniqueMultiplayerID;

				//Game1.currentLocation.dropObject(bot, pos, Game1.viewport, true, (Farmer)null);
				Game1.player.currentLocation.setObject(tilePos, bot);
				BotManager.instances.Add(bot);
				break;
			
			case SButton.PageDown:
				ToDoManager.MarkAllTasksDone();
				this.Monitor.Log("All tasks solved!");
				break;

			case SButton.Insert:
				this.Monitor.Log("Logging ModData of your bots...");
				foreach (var instance in BotManager.instances) {
					this.Monitor.Log($"Bot instance {instance.data.ToString()}");
				}
				this.Monitor.Log("Done!");
				break;
				
			case SButton.NumPad0:
				Vector2 mousePos = this.Helper.Input.GetCursorPosition().Tile;
				this.Monitor.Log($"Performing lookup at mouse position: {mousePos}");
				bool occupied = Game1.player.currentLocation.isTileOccupied(mousePos);
				string name = "null";
				var obj = Game1.player.currentLocation.getObjectAtTile(mousePos.GetIntX(), mousePos.GetIntY());
				if (obj != null) name = obj.Name;
				this.Monitor.Log($"Object Lookup result [occupied: {occupied}]: {name}");
				break;
			}
		}
#endif
		
		public void OnMenuChanged(object sender, MenuChangedEventArgs e) {
			this.Monitor.Log($"Menu opened: {e.NewMenu}");
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
					var botForSale = new BotObject();
					botForSale.owner.Value = Game1.player.UniqueMultiplayerID;
					shop.forSale.Insert(index, botForSale);
					shop.itemPriceAndStock.Add(botForSale, new int[2] { 2500, int.MaxValue });	// sale price and available stock
				}
			}

			var dlog = e.NewMenu as DialogueBox;
			if (dlog == null) return;
			if (!dlog.isQuestion || dlog.responses[0].responseKey != "Weather") return;
			// Only allow players to use the home computer at their own cabin
			if (Game1.player.currentLocation.NameOrUniqueName != Game1.player.homeLocation.Value) return;

			// TV menu: insert a new option for the Home Computer
			Response r = new Response("Farmtronics", I18n.TvChannel_Label());
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
			if (Context.IsMainPlayer) BotManager.ConvertBotsToChests(true);
			BotManager.ClearAll();
		}

		public void OnSaved(object sender, SavedEventArgs args) {
			if (Context.IsMainPlayer) BotManager.ConvertChestsToBots();
		}

		public void OnSaveLoaded(object sender, SaveLoadedEventArgs args) {
			if (Context.IsMainPlayer) {
				SaveData.CreateSaveDataDirs();
				if (SaveData.IsOldSaveDirPresent()) SaveData.MoveOldSaveDir();
				ModEntry.instance.Monitor.Log($"Setting host player ID: {Game1.player.UniqueMultiplayerID}");
				MultiplayerManager.hostID = Game1.player.UniqueMultiplayerID;
				BotManager.ConvertChestsToBots();
			}
		}

		public void OnDayStarted(object sender, DayStartedEventArgs args) {
			Monitor.Log($"OnDayStarted");
			Helper.Events.Player.Warped += OnPlayerWarped;

			// Initialize the home computer and all bots for autostart.
			// This initialization will also cause all startup scripts to run.
			InitComputerShell();
			if (Context.IsMainPlayer) MultiplayerManager.InitRemoteComputer();
			BotManager.InitShellAll();
		}

		/// <summary>
		/// Initializes the home computer shell.
		/// Effectively boots up the home computer if it is not already running.
		/// </summary>
		private void InitComputerShell() {
			if (shell == null) {
				shell = new Shell();
				shell.Init(Game1.player.UniqueMultiplayerID);
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
                    data["FarmtronicsFirstBotMail"] = I18n.Mail_Text("%item itemRecovery %%");
                    foreach (var msg in Game1.player.mailbox) {
                        this.Monitor.Log($"mail in mailbox: {msg}");
                        if (msg == "FarmtronicsFirstBotMail") {
                            this.Monitor.Log($"Changing recoveredItem from {Game1.player.recoveredItem} to Bot");
							var bot = new BotObject();
							bot.owner.Value = Game1.player.UniqueMultiplayerID;
                            Game1.player.recoveredItem = bot;
                            break;
                        }
                    }
                });
            }
        }
    }
}

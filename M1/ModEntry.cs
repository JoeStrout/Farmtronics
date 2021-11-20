using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.BellsAndWhistles;
using StardewValley.TerrainFeatures;


namespace M1
{
	public class ModEntry : Mod {
		public static IModHelper helper;
		public static ModEntry instance;

		Shell shell;

		public override void Entry(IModHelper helper) {
			instance = this;
			ModEntry.helper = helper;
			helper.Events.Display.MenuChanged += this.OnMenuChanged;

			helper.Events.Input.ButtonPressed += this.OnButtonPressed;
		}

		public void print(string s) {
            this.Monitor.Log(s, LogLevel.Debug);
		}

		Farmer robot;
		public void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
			if (e.Button == SButton.PageUp) {
				print("Spawning robot farmer");
				var initialTools = new List<Item>();
				initialTools.Add(new StardewValley.Tools.Hoe());
				Farmer f = new Farmer(new FarmerSprite("Characters\\Farmer\\farmer_base"),
					new Vector2(Game1.player.Position.X - 64f, Game1.player.Position.Y), 2,
					Dialogue.randomName(), initialTools, isMale: true);
				//f.changeShirt(random.Next(40));
				//f.changePants(new Color(random.Next(255), random.Next(255), random.Next(255)));
				//f.changeHairStyle(random.Next(FarmerRenderer.hairStylesTexture.Height / 96 * 8));
				//if (random.NextDouble() < 0.5)
				//{
				//	f.changeHat(random.Next(-1, FarmerRenderer.hatsTexture.Height / 80 * 12));
				//}
				//else
				//{
				//	player.changeHat(-1);
				//}
				//f.changeHairColor(new Color(random.Next(255), random.Next(255), random.Next(255)));
				//f.changeSkinColor(random.Next(16));
				f.FarmerSprite.setOwner(f);
				f.currentLocation = Game1.currentLocation;
				// Do not add to Game1.otherFarmers... that is for network players,
				// and causes all manner of failure for our robot farmer.
				//Game1.otherFarmers.Add(Game1.random.Next(), f);
				// ...but without that, our farmer does not appear in game. :(

				// Hmm, maybe;
				//f.setTileLocation(new Vector2(MathF.Floor(f.Position.X/64f), MathF.Floor(f.Position.Y/64f)));
				// Nope.
				// OK, how about:
				//Game1.otherFarmers.Add(Game1.random.Next(), new StardewValley.Network.NetFarmerRoot(f));

				//print("Farmer spawned with name " + f.Name);
				//print("Game1.serverHost: " + Game1.serverHost);
				//print("Roots:");
				//var m = new Multiplayer();
				//int i=0;
				//foreach (var farmerRoot in m.farmerRoots()) {
				//	print($"{i}: {farmerRoot}");
				//	i++;
				//}

				robot = f;

				// Also create a visible item at the same location:
				robot.currentLocation.dropObject(
					new StardewValley.Object(820, 1, false, -1, 0), robot.position, Game1.viewport, true, (Farmer)null);

			}
			if (e.Button == SButton.PageDown) {
				// This works!  There's no tool animation, of course, but it does/
				// have the effect of using that tool on the environment.  Neat!
				robot.CurrentToolIndex = 0;			
				Game1.toolAnimationDone(robot);
				// So!  I think we just need to spawn an item in the world to
				// represent the robot, and also have an invisible farmer at that
				// same location.  Then have the farmer use the tools, while the
				// object moves around going through the motions!
			}
		}

		public void OnMenuChanged(object sender, MenuChangedEventArgs e) {
			var dlog = e.NewMenu as DialogueBox;
			if (dlog == null || !dlog.isQuestion || dlog.responses[0].responseKey != "Weather") return;

			// insert our new response
			Response r = new Response("M1", "MiniScript M-1 Home Computer");
			dlog.responses.Insert(dlog.responses.Count-1, r);
			// adjust the dialog height
			var h = SpriteText.getHeightOfString(r.responseText, dlog.width) + 16;
			dlog.heightForQuestions += h; dlog.height += h;
			// intercept the handler (but call the original one for other responses)
			var prevHandler = Game1.currentLocation.afterQuestion;
			Game1.currentLocation.afterQuestion = (who, whichAnswer) => {
				print($"{who} selected channel {whichAnswer}");
				if (whichAnswer == "M1") PresentComputer();
				else prevHandler(who, whichAnswer);
			};
		}

		private void PresentComputer() {
			if (shell == null) shell = new Shell();
			shell.Present();

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

		}
	}
}

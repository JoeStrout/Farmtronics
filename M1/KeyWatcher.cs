/*
This class watches a key to be pressed, and reports when this should be treated
as a key input (due to initial press or repeat).
*/

using System;
using StardewModdingAPI;
using Microsoft.Xna.Framework;

namespace Farmtronics {
	public class KeyWatcher {
		public SButton keyButton;
		public char keyChar;
		public bool justPressedOrRepeats { get; private set; }

		TimeSpan lastUpdate;
		public bool isDown {  get; private set; }
		double nextRepeatTime;

		public KeyWatcher(SButton keyButton, char keyChar) {
			this.keyButton = keyButton;
			this.keyChar = keyChar;
		}

		public void Update(GameTime gameTime) {
			if (gameTime.TotalGameTime == lastUpdate) return;	// already updated
			lastUpdate = gameTime.TotalGameTime;

			var inp = ModEntry.instance.Helper.Input;
			bool nowDown = inp.IsDown(keyButton);

			if (!nowDown) {
				isDown = justPressedOrRepeats = false;
			} else {
				double now = gameTime.TotalGameTime.TotalSeconds;
				if (!isDown) {
					// initial press
					justPressedOrRepeats = true;
					nextRepeatTime = now + 0.5f;
				} else if (now > nextRepeatTime) {
					// auto-repeat
					justPressedOrRepeats = true;
					nextRepeatTime = now + 0.1f;
				} else {
					// held, but not time for a repeat yet
					justPressedOrRepeats = false;
				}
				isDown = true;
			}
		}
	}
}

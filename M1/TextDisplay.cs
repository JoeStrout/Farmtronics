using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Farmtronics {

	public class TextDisplay {
	
		public class Cell {
			public char character = ' ';
			public Color foreColor;
			public Color backColor;
			public bool inverse = false;		// used only for the hardware cursor!
		
			public Cell(Color foreColor, Color backColor) {
				this.foreColor = foreColor;
				this.backColor = backColor;
			}
		
			public override string ToString() {
				return string.Format("Cell[{0}]", character);
			}

		
			public void Copy(Cell other) {
				foreColor = other.foreColor;
				backColor = other.backColor;
				inverse = other.inverse;
				character = other.character;
			}
		}
	
		#region Public Properties
	
		public int cols = 40;
		public int rows = 20;
		public float colSpacing = 16;
		public float rowSpacing = 24;
		public Color textColor = Color.White;
		public Color backColor = Color.DeepSkyBlue;
		public bool inverse;
		public float cursorOnTime = 0.7f;
		public float cursorOffTime = 0.3f;
		public string delimiter = "\n";

		public Action onScrolled;

		#endregion
		//--------------------------------------------------------------------------------
		#region Private Properties
	
		Cell[,] cells;		// indexed with row, col
	
		public int cursorX { get; private set; }
		public int cursorY { get; private set; }
		
		bool cursorShown =  false;		// whether the cursor should be shown at all (except for blinking)
		bool cursorBlinking = false;	// whether cursor is currently hidden just due to blinking
		float cursorTime;
	
		Texture2D fontAtlas;
		Dictionary<char, int> fontCharToIndex;
		Texture2D whiteTex;


		#endregion
		//--------------------------------------------------------------------------------
		#region Public Methods
	
		public TextDisplay() {
			cells = new Cell[rows, cols];
			for (int row=0; row<rows; row++) {
				for (int col=0; col<cols; col++) {
					cells[row, col] = new Cell(textColor, backColor);
				}
			}
			

			fontAtlas = ModEntry.instance.Helper.ModContent.Load<Texture2D>(Path.Combine("assets", "fontAtlas.png"));
			ModEntry.instance.Monitor.Log($"Loaded fontAtlas with size {fontAtlas.Width}x{fontAtlas.Height}");

			string modPath = ModEntry.instance.Helper.DirectoryPath;
			string[] lines = System.IO.File.ReadAllLines(Path.Combine(modPath, "assets", "fontList.txt"));
			ModEntry.instance.Monitor.Log($"read {lines.Length} lines from fontList, starting with {lines[0]}");

			// First line just defines the atlas cell size... ignoring that for now...
			fontCharToIndex = new Dictionary<char, int>();
			for (int i=1; i<lines.Length; i++) {
				// Subsequent lines have the Unicode code point, then a Tab, then the character in UTF-8.
				// We really only need the character.
				int tabPos = lines[i].IndexOf('\t');
				if (tabPos >= 0) fontCharToIndex[lines[i][tabPos+1]] = i-1;
			}

			whiteTex = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
			whiteTex.SetData(new[] { Color.White });
		}

		public void Clear() {
			Fill(' ');
			cursorShown = false;
		}
	
		public void ClearRow(int row) {
			FillRow(row, ' ');
		}
	
		public void Fill(char c) {
			for (int row=0; row<rows; row++) {
				for (int col=0; col<cols; col++) {
					Cell cell = cells[row, col];
					cell.character = c;
					cell.inverse = inverse;
					cell.foreColor = textColor;
					cell.backColor = backColor;
				}
			}
		}
	
		public void FillRow(int row, char c) {
			for (int col=0; col<cols; col++) {
				Cell cell = cells[row, col];
				cell.character = c;
				cell.inverse = inverse;
				cell.foreColor = textColor;
				cell.backColor = backColor;
			}
		}
	
		public void Put(char c) {
			if (c == '\n' | c == '\r') {
				NextLine();
			} else if (c == '\t') {
				do {
					Set(cursorY, cursorX, ' ');
					Advance();
				} while (cursorX % 4 != 0);
			} else if (c == 7) {
				Beep();
			} else if (c == 8) {
				Backup();
			} else if (c == 134) {
				inverse = true;
			} else if (c == 135) {
				inverse = false;
			} else {
				Set(cursorY, cursorX, c);
				Advance();
			}
		}

		public void Beep() {
			// ToDo
		}

		public void Advance() {
			cursorX++;
			if (cursorX >= cols) NextLine();		
		}
	
		public void Backup() {
			int oldX = cursorX, oldY = cursorY;
			HideCursorVisual();
			cursorX--;
			if (cursorX < 0) {
				if (cursorY >= rows-1) {
					cursorX = 0;
					return;
				}
				cursorY++;
				cursorX = cols-1;
			}
		}
	
		public void Set(int row, int col, char c) {
			Set(row, col, c, textColor, backColor);
		}
	
		public void Set(int row, int col, char c, Color textColor, Color backColor) {
			Cell cell = cells[row, col];
		
			if (inverse) {
				cell.foreColor = backColor;
				cell.backColor = textColor;
			} else {
				cell.foreColor = textColor;
				cell.backColor = backColor;
			}
			cell.character = c;		
		}
	
		public Cell Get(int row, int col) {
			if (row < 0 || row >= rows || col < 0 || col >= cols) return null;
			return cells[row, col];
		}
	
		public void Print(string s) {
			HideCursorVisual();
			//ModEntry.instance.Monitor.Log($"Printing `{s}` with colors {textColor},{backColor}");
			if (s != null) foreach (char c in s) Put(c);
		}
	
		public void NextLine() {
			cursorY--;
			while (cursorY < 0) Scroll();
			cursorX = 0;
		}

		public void PrintLine(string s) {
			Print(s);
			NextLine();
		}

		public void Scroll() {
			for (int row = rows-1; row > 0; row--) {
				for (int col=0; col<cols; col++) {
					cells[row, col].Copy(cells[row-1, col]);
				}
			}
			if (cursorY < rows-1) cursorY++;
			ClearRow(0);
			if (onScrolled != null) onScrolled.Invoke();
		}
	
		/// <summary>
		/// Hide the visual display of the cursor, if any.  Does not actually
		/// turn the cursor off.
		/// </summary>
		void HideCursorVisual() {
			if (!cursorShown) return;
			cells[cursorY, cursorX].inverse = false;
			if (cells[cursorY, cursorX].backColor != backColor) {
				// Looks like the cursor is over some weird-colored text...
				// probably from Autocomplete.  Adjust colors accordingly.
				cells[cursorY, cursorX].foreColor = Color.Lerp(textColor, backColor, 0.75f);
				cells[cursorY, cursorX].backColor = backColor;
			}
		}
	
		public void HideCursor() {
			//ModEntry.instance.Monitor.Log("Hiding cursor at " + cursorY + ", " + cursorX);
			HideCursorVisual();
			cursorTime = 0;
			cursorShown = false;
		}
	
		public void ShowCursor() {
			//ModEntry.instance.Monitor.Log("Showing cursor at " + cursorY + ", " + cursorX);
			cells[cursorY, cursorX].inverse = true;
			if (cells[cursorY, cursorX].foreColor != textColor) {
				// Looks like the cursor is over some weird-colored text...
				// probably from Autocomplete.  Adjust colors accordingly.
				cells[cursorY, cursorX].foreColor = textColor;
				cells[cursorY, cursorX].backColor = Color.Lerp(backColor, textColor, 0.75f);
			}
			cursorTime = 0;
			cursorShown = true;
		}
	
		public RowCol GetCursor() {
			return new RowCol(cursorY, cursorX);
		}
	
		public void SetCursor(RowCol pos) {
			SetCursor(pos.row, pos.col);
		}
	
		public void SetCursor(int row, int col) {
			bool wasShown = cursorShown;
			if (cursorShown) HideCursor();
			cursorX = col;
			if (cursorX < 0) cursorX = 0;
			else if (cursorX >= cols) cursorX = cols-1;
			cursorY = row;
			if (cursorY < 0) cursorY = 0;
			if (cursorY >= rows) cursorY = rows-1;
			if (wasShown) ShowCursor();
		}

		public RowCol RowColForXY(Vector2 localPos) {
			RowCol v = new RowCol() { 
				col = (int)MathF.Floor(localPos.X / colSpacing),
				row = (int)MathF.Floor(localPos.Y / rowSpacing)
			};
			if (v.col < 0) v.col = 0;
			if (v.col >= cols) v.col = cols-1;
			if (v.row < 0) v.row = 0;
			if (v.row >= rows) v.row = rows-1;
			return v;
		}

		public void Render(SpriteBatch b, Rectangle displayArea) {
			// Start by drawing the background, wherever the background color
			// isn't transparent.
			for (int row=0; row<rows; row++) {
				int col=0;
				while (col < cols) {
					Color cellBg = Color.White;
					// skip ahead to next cell that's a nonstransparent
					while (col < cols) {
						cellBg = cells[row,col].inverse ? cells[row,col].foreColor : cells[row,col].backColor;
						if (cellBg.A > 0) break;
						col++;
					}
					if (col >= cols) break;
					// Now start a rectangle at this location.
					int startCol = col;
					Rectangle rect = new Rectangle(displayArea.Left + col*16 + 1, displayArea.Bottom - row * 24 - 24, 15, 24);
					// now skip ahead until a cell that's a different color from this one
					while (++col < cols) {
						var newBg = cells[row,col].inverse ? cells[row,col].foreColor : cells[row,col].backColor;
						if (newBg != cellBg) break;
						rect.Width += 16;
					}
					// And draw!
					FillRect(b, rect, cellBg);
					//ModEntry.instance.print($"Background fill: {rect} in {cellBg} -- row {row}, col {startCol}-{col}");
				}
			}

			// Now render the text (foreground)
			for (int row=0; row<rows; row++) {
				for (int col=0; col<cols; col++) {
					Color c = cells[row, col].inverse ? cells[row,col].backColor : cells[row,col].foreColor;
					DrawFontCell(b, cells[row, col].character, c, col, row, displayArea);
				}
			}
		}

		public void Update(GameTime gameTime) {
			if (cursorShown) {
				// Make the cursor blink!
				cursorTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (!cursorBlinking && cursorTime > cursorOnTime) {
					HideCursor();
					cursorShown = true;
					cursorBlinking = true;				
				} else if (cursorBlinking && cursorTime > cursorOffTime) {
					ShowCursor();
					cursorBlinking = false;				
				}
			}

		}

			/*
		protected override void FillOutMap() {
			base.FillOutMap();
			map.assignOverride = (key, value) => {
				if (value == null) value = ValNumber.zero;
				switch (key.ToString()) {
				case "color":
					textColor = value.ToString().ToColor();
					break;
				case "backColor":
					backColor = value.ToString().ToColor();
					break;
				case "column":
					SetCursor(cursorY, value.IntValue());
					break;
				case "row":
					SetCursor(value.IntValue(), cursorX);
					break;
				case "inverse":
					inverse = value.BoolValue();
					break;
				case "delimiter":
					delimiter = value.ToString();
					break;
				case "mode":
					var mode = (DisplayManager.Mode)value.IntValue();
					DisplayManager.instance.SetDisplayMode(layerNum, mode);
					break;
				}
				return true;
			};
			// If any property values were set in an instance map before we got around to
			// hooking that up to this class, then apply those values now (and remove from the instance).
			foreach (string key in new string[]{"color", "backColor", "column", "row", "inverse", "delimiter"}) {
				var keyval = new ValString(key);
				ValMap mapKeyWasFoundIn;
				Value value = map.Lookup(keyval, out mapKeyWasFoundIn);
				if (mapKeyWasFoundIn == map) {
					map.assignOverride(keyval, value);
					map.map.Remove(keyval);
				}
			}
		}
	
		public override DisplayManager.Mode GetMode() {
			return DisplayManager.Mode.Text;
		}
		*/

		#endregion
		//--------------------------------------------------------------------------------
		#region Private Methods

		Rectangle AtlasRectForIndex(int index) {
			int col = index % 32;
			int row = index / 32;
			return new Rectangle(col*10, row*14, 10, 14);
		}

		void DrawFontCell(SpriteBatch b, int atlasIndex, Color color, int screenCol, int screenRow, Rectangle displayArea) {
			var srcR = AtlasRectForIndex(atlasIndex);

			float drawX = displayArea.Left + screenCol * 16 + 1;
			float drawY = displayArea.Bottom - screenRow * 24 - 27; 
			
			b.Draw(fontAtlas, new Vector2(drawX, drawY), srcR, color,
				0, Vector2.Zero, 2, SpriteEffects.None, 0.95f);
		}

		bool DrawFontCell(SpriteBatch b, char charToDraw, Color color, int screenCol, int screenRow, Rectangle displayArea) {
			if (charToDraw == ' ' || charToDraw == 0) return true;
			int atlasIndex = -1;
			if (!fontCharToIndex.TryGetValue(charToDraw, out atlasIndex)) {
				// Char not found; skip control characters, but draw the [?] char for anything else.
				if (charToDraw < ' ') return false;
				atlasIndex = 5;	// index of [?] character
			}

			DrawFontCell(b, atlasIndex, color, screenCol, screenRow, displayArea);
			return true;
		}

		void FillRect(SpriteBatch b, Rectangle rect, Color color) {
			b.Draw(whiteTex, rect, color);
		}
	
		#endregion
	}

}
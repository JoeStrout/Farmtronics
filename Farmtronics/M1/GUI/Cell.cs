
using Microsoft.Xna.Framework;

namespace Farmtronics.M1.GUI {
	class Cell {
		public char character = ' ';
		public Color foreColor;
		public Color backColor;
		public bool inverse = false;        // used only for the hardware cursor!

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
}

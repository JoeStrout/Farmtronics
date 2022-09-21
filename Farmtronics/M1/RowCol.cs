namespace Farmtronics.M1 {
	struct RowCol {
		public int row;
		public int col;

		public RowCol(int row, int col) {
			this.row = row;
			this.col = col;
		}

		public override string ToString() {
			return string.Format("row {0}, col {1}", row, col);
		}

		public bool Equals(RowCol p) {
			return p.row == row && p.col == col;
		}

		public static bool operator ==(RowCol lhs, RowCol rhs) {
			return lhs.Equals(rhs);
		}

		public static bool operator !=(RowCol lhs, RowCol rhs) {
			return !lhs.Equals(rhs);
		}

		public override bool Equals(object obj) {
			return obj is RowCol && Equals((RowCol)obj);
		}

		public override int GetHashCode() {
			return row.GetHashCode() ^ col.GetHashCode();
		}
	}	
}

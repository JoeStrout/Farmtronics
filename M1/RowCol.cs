
public struct RowCol {
	public int row;
	public int col;
	
	public RowCol(int row, int col) {
		this.row = row;
		this.col = col;
	}
	
	public override string ToString() {
		return string.Format("row {0}, col {1}", row, col);
	}
}

import "chars"
import "listUtil"

weirdCharsLeft = chars.printable + [chars.tab,chars.left,chars.right,chars.up,chars.down]
weirdCharsLeft.sort
weirdCharsLeft.removeVal chars.inverseOn
weirdCharsLeft.removeVal chars.inverseOff
weirdCharsLeft.removeVal chars.bell

clear
gfx.scale = 3
gfx.scrollY = 640*gfx.scale - 640

isValidChar = function(codept)
	if codept > 32 and codept < 128 then return true
	if codept > 191 and codept < 256 then return true
	c = char(codept)
	if weirdCharsLeft.contains(c) then return true
	return false
end function

fontDefLines = ["10x14"]

col = 0
row = 0
// text.delimiter = ""
text.row = 6
addChar = function(codept)
	if isValidChar(codept) then 
		c = color.white 
		weirdCharsLeft.removeVal char(codept)
	else 
		//c = color.red
		return
	end if
	x = col * 10
	y = 640 - 14 * (row+1)
	//gfx.line x, y, x+8, y, "#FF00FF88"
	// print char(codept)
	gfx.print char(codept), x, y, c, "small"
	fontDefLines.push codept + chars.tab + char(codept)
	globals.col = col + 1
	if col >= 32 then
		globals.col = 0
		globals.row = row + 1
		// print char(13)
	end if
end function

for codept in range(0, 255)
	addChar codept
end for
for c in weirdCharsLeft[:]
	if c isa string then 	addChar c.code
end for

row = row + 1
atlas = gfx.getImage(0, 640-row*14, 320, row*14)
file.saveImage "fontAtlas.png", atlas
file.writeLines "fontList.txt", fontDefLines
text.row = 4

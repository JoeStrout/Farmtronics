// Text Utilities (related to displaying text on the Mini Micro screen).

import "stringUtil"

// Print without a carriage return (i.e. moving to the next line).
printNoCR = function(s)
	oldDelim = text.delimiter
	text.delimiter = ""
	print s
	text.delimiter = oldDelim
end function

// Draw a button with the given caption at the cursor position.
drawButton = function(caption)
	text.inverse = not text.inverse
	printNoCR char(57856) + caption + char(57857)
	text.inverse = not text.inverse
end function

// Print the given string, wrapped on word breaks (spaces) to
// fit in the given width.  Subsequent lines begin at the same
// text.column as the current text.column value.
// Parameters:
//		s: string to print
//		width: character limit per line; if null, goes to edge of screen
//		maxLines: maximum lines to print; or if null, no limit
// Returns: remaining (unprinted) portion of string, or null
printWrap = function(s, width=null, maxLines=null)
	left = text.column
	if width == null then width = 39 - left
	lineCount = 0
	while s
		text.column = left
		if s.len <= width then
			print s
			return null
		end if
		foundCut = false
		for i in range(width, 0)
			if s[i] == " " then
				print s[:i]
				s = s[i+1:]
				foundCut = true
				break
			end if
		end for
		if not foundCut then
			// Couldn't find a space to cut on...
			// so, out of desperation, just cut at width
			print s[:width]
			s = s[width:]
		end if
		lineCount = lineCount + 1
		if maxLines and lineCount == maxLines then return s
	end while
end function

// Get the text column for a given screen pixel X coordinate.
textCol = function(pixelX)
	return floor((pixelX-5)/14)
end function

// Get the text row for a given screen pixel Y coordinate.
textRow = function(pixelY)
	return floor((pixelY-6)/24)
end function

// Print a string starting at the given column and row.
// Also, if you use this to print to row 0, it avoids the usual scrolling.
printAt = function(column, row, s)
	text.row = row
	text.column = column
	if row == 0 then
		// avoid scrolling when printing to the bottom row
		delim = text.delimiter
		text.delimiter = ""
		print s
		text.delimiter = delim
	else
		print s
	end if
end function

// Print a string centered on the given column and row.
printCenteredAt = function(column, row, s)
	printAt column - s.len/2, row, s
end function

// clearToEOL: Clear from the current cursor position to the end of the line
// (by filling the rest of the line with spaces, or whatever character you specify).
// Also applies default foreground and background colors.
clearToEOL = function(character=" ")
	cols = range(text.column, 67)
	text.setCell cols, text.row, character
	text.setCellColor cols, text.row, text.color
	text.setCellBackColor cols, text.row, text.backColor
end function

// clearRow: Clear the entirety of the given row by filling it with spaces
// (or whatever character you specify).
// Also applies default foreground and background colors.
clearRow = function(row=null, character=" ")
	if row == null then row = text.row
	if not row isa list then row = [row]
	allCols = range(0, 67)
	for r in row
		text.setCell allCols, r, character
		text.setCellColor allCols, r, text.color
		text.setCellBackColor allCols, r, text.backColor
	end for
end function

// buttonLeft and buttonRight: combines the inverse toggles with
// the button cap glyphs.  Print these before or after a button caption.
buttonLeft = char(134) + char(57856)
buttonRight = char(57857) + char(135)

// button function: returns a complete printable button with the given
// caption, optionally padded to some caption length.
button = function(caption, minCaptionLength=0)
	if caption.len < minCaptionLength then
		pad = minCaptionLength - caption.len
		caption = " " * floor(pad/2) + caption + " " * ceil(pad/2)
	end if
	return buttonLeft + caption + buttonRight
end function

// Cell: a little class that represents the complete contents (including
// colors) of a particular row and column on a text display.
Cell = {}
Cell.character = ""
Cell.color = ""
Cell.backColor = ""
Cell.get = function(column, row)
	result = new Cell
	result.character = text.cell(column, row)
	result.color = text.cellColor(column, row)
	result.backColor = text.cellBackColor(column, row)
	return result
end function
Cell.apply = function(column, row)
	text.setCell column, row, self.character
	text.setCellColor column, row, self.color
	text.setCellBackColor column, row, self.backColor
end function

// getContent: Get the entire content of the text display as a 2D array
// of Cell objects in [row][col] order.
// (You can then restore this later with setContent.)
getContent = function
	rows = []
	for row in range(0, 25)
		cols = []
		for col in range(0, 67)
			cols.push Cell.get(col, row)
		end for
		rows.push cols
	end for
	return rows
end function

// setContent: restore the content of the text display with the given
// 2D array of Cell objects in [row][col] order.
setContent = function(data)
	row = 0
	for dataRow in data
		col = 0
		for c in dataRow
			c.apply col, row
			col = col + 1
			if col > 67 then break
		end for
		row = row + 1
		if row > 25 then break
	end for
end function

// Dialog class: used to define a simple message dialog,
// with title, message, and 1-3 buttons.  Create a new Dialog,
// with Dialog.make, assign all its other properties as desired,
// then call .show on it.  The return value is the button that
// was clicked by the user.
Dialog = {}
Dialog.title = "Alert"
Dialog.message = "Your message here."
Dialog.width = 34
Dialog.backColor = "#AAAACC"
Dialog.titleColor = "#CCCCFF"
Dialog.textColor = "#444444"
DialogButton = {}
DialogButton.caption = "Button"
DialogButton.visible = false
DialogButton.key = ""  // shortcut key
DialogButton.width = function; return self.caption.len + 2; end function
DialogButton.x = 20
DialogButton.y = 10
DialogButton.draw = function
	c = text.backColor
	text.row = self.y; text.column = self.x; print buttonLeft
	text.backColor = color.black
	text.row = self.y; text.column = self.x + 1; print self.caption
	text.backColor = c
	text.row = self.y; text.column = self.x + 1 + self.caption.len; print buttonRight
end function
DialogButton.contains = function(pt)
	return textRow(pt.y) == self.y and 
	   textCol(pt.x) >= self.x and textCol(pt.x) < self.x + self.width
end function
DialogButton.trackHit = function
	c = text.cellBackColor(self.x, self.y)
// Full functionality here is awaiting the mouse API...
//	while mouse.button
//		text.color = c
//		if self.contains(mouse) then text.color = color.lerp(c, color.black)
//		self.draw
//		yield
//	end while
	if text.color == c then return false
	text.color = c
	self.draw
	return true
end function

Dialog.make = function(title="", message="Your message here.")
	dlog = new Dialog
	dlog.title = title
	dlog.message = message
	dlog.okBtn = new DialogButton
	dlog.okBtn.caption = "OK"
	dlog.okBtn.visible = true
	dlog.okBtn.key = char(10)  // (return key)
	dlog.altBtn = new DialogButton
	dlog.cancelBtn = new DialogButton
	dlog.cancelBtn.caption = "Cancel"
	dlog.cancelBtn.key = char(27)
	if title.len > dlog.width then dlog.width = title.len
	dlog.msgLines = message.wrap(dlog.width-4)
	dlog.height = dlog.msgLines.len + 4
	return dlog
end function

Dialog.show = function(timeout=null)
	priorText = getContent
	priorTextColor = text.color
	priorBgColor = text.backColor
	priorDelim = text.delimiter

	rows = range(9 - floor(self.height/2), 10 + ceil(self.height/2))
	cols = range(20 - floor(self.width/2), 19 + ceil(self.width/2))
	
	text.delimiter = char(13)
	text.row = 12 + ceil(self.height/2)
	x = 20 - self.width/2
	
	// draw title
	text.color = self.textColor
	text.backColor = self.titleColor
	spacesNeeded = self.width - self.title.len
	text.row = rows[-1] + 1; text.column = cols[0]
	print " " * floor(spacesNeeded/2) + self.title + " " * ceil(spacesNeeded/2)
	
	// draw box and message
	text.setCell cols, rows, " "
	text.setCellBackColor cols, rows, self.backColor
	text.backColor = self.backColor
	text.row = rows[-2]
	for line in self.msgLines
		text.column = cols[2]; print line
	end for
	
	// draw buttons
	btnRow = rows[1]
	btnX = cols[-1] - 1
	buttons = [self.okBtn, self.altBtn, self.cancelBtn]
	for btn in buttons
		if not btn.visible then continue
		text.color = "#CCCCCC"  // (button color)
		if btn.key == char(10) then text.color = "#EEEEFF"
		btn.x = btnX - btn.width
		if btn == self.cancelBtn then btn.x = x + 2
		btn.y = btnRow
		btn.draw
		btnX = btn.x - 2
	end for
	
	// now wait until we get a key shortcut or a valid click on a button,
	// or (optionally) time out
	btnHit = null
	startTime = time
	while not btnHit
//		if mouse.button then
//			for btn in buttons
//				if not btn.visible or not btn.contains(mouse) then continue
//				if btn.trackHit then
//					btnHit = btn
//					break
//				end if
//			end for
//		end if
		if key.available then
			k = key.get
			if k.code == 3 or k.code == 13 then k = char(10)
			for btn in buttons
				if btn.visible and btn.key == k then
					btnHit = btn
					break
				end if
			end for
		end if
		if timeout != null and time > startTime + timeout then break
	end while
	
	text.color = priorTextColor
	text.backColor = priorBgColor
	setContent priorText
	text.delimiter = priorDelim
	return btnHit
end function

if locals == globals then
	d = Dialog.make("Message Test", 
	"Hello world!  This is a test of the nifty text-based Dialog system.")
	d.altBtn.visible = true
	d.altBtn.key = "b"
	d.cancelBtn.visible = true
	print d.show.caption
end if

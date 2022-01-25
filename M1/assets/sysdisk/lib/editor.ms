// This is the Farmtronic text editor.  It is imported and invoked
// via the `edit` command defined in /sys/startup.ms.

data = [""]
scrollY = 0
scrollX = 0

screenH = 20  // screen height, in rows
screenW = 40  // screen width, in columns
screenLines = screenH - 1  // number of lines of text we can show
TAB = char(9)

cursorX = 0  // line number, from 0 to data.len-1
cursorY = 0  // column number, from 0 to data[cursorX].len
idealCursorX = cursorX

pal = {}  // "palette"
pal.background = "#222222"
pal.text = "#CCCCCC"
pal.cursor = "#CCCCFF"
pal.topBar = "#AAAAAA"
pal.topBarText = "#8888CC"

lineLen = function(lineNum)
	if lineNum < data.len then return data[lineNum].len
	return 0
end function

drawTopBar = function()
	text.backColor = pal.topBar
	text.color = pal.topBarText
	text.row = screenH-1; text.column = 0
	print " " + _sourceFile
	print " " * (screenW - 17 - text.column)
	print "^Q: Quit   L: " + (cursorY + 1)
	print " " * (screenW - text.column)
end function

refreshLine = function(lineNum)
	y = lineNum - scrollY  // (rows from the top)
	if y < 0 or y >= screenLines then return
	text.backColor = pal.background
	text.color = pal.text
	text.row = screenLines - 1 - y; text.column = 0
	if lineNum < data.len then
		s = data[lineNum][scrollX:]
		if s.len < screenW then s = s + " " * (screenW - s.len)
		if s.len > screenW then s = s[:screenW]
	else 
		s = " " * screenW
	end if
	if lineNum == cursorY then outer.cursorShown = false
	print s
end function

printLine = function(row, s)
	if s.len < screenW then
		s = s + " " * (screenW - s.len)
	else if s.len > screenW then
		s = s[:screenW]
	end if
	text.row = row; text.column = 0
	if row > 0 then
		print s
	else
		print s[:-1]
		text.setCell screenW-1, 0, s[-1]
		text.setCellColor screenW-1, 0, text.color
		text.setCellBackColor screenW-1, 0, text.backColor
	end if
end function

refreshRow = function(screenRow)
	text.backColor = pal.background
	text.color = pal.text
	i = scrollY + screenLines - 1 - screenRow
	text.row = screenRow; text.column = 0
	if i < data.len then
		s = data[i][scrollX:]
	else
		s = ""
	end if
	printLine screenRow, s
	if i == cursorY then outer.cursorShown = false
end function

refreshDisplay = function()
	for row in range(screenLines-1, 0)
		refreshRow row
	end for
	outer.cursorShown = false
	text.backColor = color.clear
end function

showCursor = function(showIt=true)
	if cursorY < scrollY or cursorY >= scrollY + screenLines then return
	if cursorX < scrollX then return
	x = cursorX - scrollX
	if x >= screenW then return
	y = screenLines - 1 - cursorY + scrollY
	if showIt then
		text.setCellColor x, y, color.black
		text.setCellBackColor x, y, pal.cursor
	else
		text.setCellColor x, y, pal.text
		text.setCellBackColor x, y, pal.background
	end if
	outer.cursorShown = showIt
end function

hideCursor = function(); showCursor false; end function

limitCursor = function()
	if cursorY < 0 then
		outer.cursorY = 0
		outer.cursorX = 0
	else if cursorY > data.len then
		outer.cursorY = data.len
	end if
	if cursorX < 0 then
		outer.cursorX = 0
	else if cursorX > lineLen(cursorY) then
		outer.cursorX = lineLen(cursorY)
	end if
end function

scrollCursorIntoView = function()
	scrolled = false
	while cursorY < scrollY
		outer.scrollY = scrollY - floor(screenLines/2)
		if scrollY < 0 then outer.scrollY = 0
		scrolled = true
	end while
	while cursorY >= scrollY + screenLines
		outer.scrollY = scrollY + floor(screenLines/2)
		maxScroll = data.len - floor(screenLines/2)
		if scrollY > maxScroll then outer.scrollY = maxScroll
		scrolled = true
	end while
	while cursorX < scrollX
		outer.scrollX = scrollX - screenW/2
		if scrollX < 0 then outer.scrollX = 0
		scrolled = true
	end while
	while cursorX >= scrollX + screenW
		outer.scrollX = scrollX + screenW/2
		scrolled = true
	end while
	if scrolled then refreshDisplay
end function

isWhiteOrPunc = function(c)
	return c <= " " or "()[]{}.,?!+-*/\|@#$%^&=".indexOf(c) != null
end function

nextWord = function(dir)
	// Advance the cursor to the next word boundary in the
	// given direction (within the current line).
	if cursorY >= data.len then return
	s = data[cursorY]
	x = cursorX
	if dir < 0 then
		// skip whitespace
		while x > 0 and isWhiteOrPunc(s[x-1])
			x = x - 1
		end while
		// then back up until we hit whitespace again
		while x > 0 and not isWhiteOrPunc(s[x-1])
			x = x - 1
		end while
	else
		// skip whitespace
		while x+1 < s.len and (x+1==s.len or isWhiteOrPunc(s[x+1]))
			x = x + 1
		end while
		// then advance until we hit whitespace again
		while x < s.len and (x==s.len or not isWhiteOrPunc(s[x]))
			x = x + 1
		end while
	end if
	outer.cursorX = x
end function

showCommand = function(keyPress, desc)
	refreshRow 0
	s = keyPress
	if desc then s = s + ": " + desc
	text.row = 0
	text.column = screenW - s.len
	text.color = color.black
	text.backColor = "#FFFFAA"
	print s[:-1]
	text.setCell screenW-1, 0, s[-1]
	text.setCellColor screenW-1, 0, text.color
	text.setCellBackColor screenW-1, 0, text.backColor
end function	

handleControlKey = function(k)
	kcode = k.code
	desc = "(undefined)"

	anyAlt = key.pressed("left alt") or key.pressed("right alt")
	anyShift = key.pressed("left shift") or key.pressed("right shift")

	keyPress = "^" + char(64 + kcode)
	if kcode > 31 then keyPress = "^" + char(kcode)
	if anyShift then keyPress = "Shift-" + keyPress

	if keyPress == "^A" then  // ctrl-A (start of line)
		desc = "LineStart"
		outer.cursorX = 0
	else if keyPress == "^E" then // ctrl-E (end of line)
		desc = "LineEnd"
		outer.cursorX = lineLen(cursorY)
	else if keyPress == "^U" then	// Page Up
		desc = "PageUp"
		outer.cursorY = outer.cursorY - screenLines
	else if keyPress == "^D" then	// Page Down
		desc = "PageDown"
		outer.cursorY = outer.cursorY + screenLines		
	else if keyPress == "^Q" then  // Escape or Ctrl+X
		desc = "Quit"
		outer.quitting = true
	end if
	
	if keyPress then showCommand keyPress, desc
end function

handleKey = function(k, fakeControl=false)
	anyCtrl = key.pressed("left ctrl") or key.pressed("right ctrl") or fakeControl
	anyAlt = key.pressed("left alt") or key.pressed("right alt")
	byWord = anyAlt or anyCtrl
	kcode = k.code
	hideCursor
	
	// Careful with 17-21: these are generated by the arrow keys,
	// which may be used in conjunction with Control, but still
	// should be treated differently than ^Q, ^R, etc.
	isArrowKey = false
	if kcode >= 17 and kcode <=20 then
		keys = ["left", "right", "up", "down"]
		if key.pressed(keys[kcode-17]) then isArrowKey = true
	end if
	
	if anyCtrl and not isArrowKey then
		handleControlKey k
	else if kcode == 17 then  // left
		keyPress = ""
		outer.cursorX = cursorX - 1
		if cursorX < 0 and cursorY > 0 then
			outer.cursorY = cursorY - 1
			outer.cursorX = lineLen(cursorY)
		end if
		if byWord then nextWord -1
		outer.idealCursorX = cursorX
	else if kcode == 18 then  // right
		keyPress = ""
		outer.cursorX = cursorX + 1
		if cursorX > lineLen(cursorY) and cursorY < data.len then
			outer.cursorY = cursorY + 1
			outer.cursorX = 0
		end if
		if byWord then nextWord 1
		outer.idealCursorX = cursorX
	else if kcode == 19 then  // up
		keyPress = ""
		if byWord then amount = screenLines else amount = 1
		outer.cursorY = cursorY - amount
		outer.cursorX = idealCursorX
	else if kcode == 20 then  // down
		keyPress = ""
		if byWord then amount = screenLines else amount = 1
		outer.cursorY = cursorY + amount
		outer.cursorX = idealCursorX
	else if kcode == 10 then  // return
		keyPress = ""
		if cursorY >= data.len then
			data.push ""
		else
			data.insert cursorY+1, data[cursorY][cursorX:]
		end if
		data[cursorY] = data[cursorY][:cursorX]
		outer.cursorY = cursorY + 1
		outer.cursorX = 0
		refreshDisplay
	else if kcode == 127 then  // forward-delete
		keyPress = ""
		if cursorX >= data[cursorY].len then
			if cursorY < data.len-1 then
				data[cursorY] = data[cursorY] + data[cursorY+1]
				data.remove cursorY + 1
				refreshDisplay
			end if
		else
			startPos = cursorX
			outer.cursorX = cursorX + 1
			if byWord then nextWord 1
			data[cursorY] = data[cursorY][:startPos] + data[cursorY][cursorX:]
			outer.cursorX = startPos
			refreshLine cursorY
		end if
	else if kcode == 8 then  // backspace
		keyPress = ""
		if cursorX > 0 then
			endPos = cursorX
			outer.cursorX = cursorX - 1
			if byWord then nextWord -1
			data[cursorY] = data[cursorY][:cursorX] + data[cursorY][endPos:]
			refreshLine cursorY
		else if cursorY > 0 then
			x = data[cursorY-1].len
			if cursorY < data.len then
				data[cursorY-1] = data[cursorY-1] + data[cursorY]
				data.remove cursorY
			end if
			outer.cursorY = cursorY - 1
			outer.cursorX = x
			refreshDisplay
		end if
	else if kcode == 27 then  // Escape
		outer.quitting = true
	else if k >= " " then  // printable key
		anyAlt = key.pressed("left alt") or key.pressed("right alt")
		anyShift = key.pressed("left shift") or key.pressed("right shift")
		byWord = anyCtrl or anyAlt
	
		if cursorY >= data.len then
			data.push k
		else if not data[cursorY] then
			data[cursorY] = k
		else
			data[cursorY] = data[cursorY][:cursorX] + k + data[cursorY][cursorX:]			
		end if
		outer.cursorX = cursorX + 1
		refreshLine cursorY
	end if

	limitCursor
	scrollCursorIntoView
	drawTopBar
	showCursor	
end function

update = function()
	wantCursor = (time % 1 < 0.75)
	if wantCursor != cursorShown then showCursor wantCursor
	if key.available then
		handleKey key.get
	else if key.pressed("left ctrl") or key.pressed("right ctrl") then
		for c in "1234567890[]/=\;-',.`"
			if key.pressed(c) then
				while key.pressed(c); yield; end while
				handleControlKey c
			end if
		end for
	else if key.pressed("page up") then
		while key.pressed("page up"); yield; end while
		handleKey "U", true
	else if key.pressed("page down") then
		while key.pressed("page down"); yield; end while
		handleKey "D", true	
	end if
end function

// editText: main entry point.
// Pass in a list of strings representing the text to edit.
editText = function(textToEdit)
	if textToEdit == null then textToEdit = []
	if textToEdit.len == 0 then textToEdit.push ""
	outer.data = textToEdit
	// We don't handle tabs properly yet, so let's just:
	for i in data.indexes; data[i] = data[i].replace(TAB, "  "); end for
	origColor = text.color
	origBackColor = text.backColor
	text.backColor = color.clear
	text.delimiter = ""
	clear
	drawTopBar
	refreshDisplay
	
	if not env.hasIndex("bootOpts") then env.bootOpts = {}
	env.bootOpts.controlC = false	// disable Control-C to break
	
	outer.quitting = false
	while not quitting
		yield
		update
	end while
	key.clear

	env.bootOpts.controlC = true	// re-eable Control-C to break

	text.delimiter = char(13)
	text.color = origColor
	text.backColor = origBackColor
	text.clear
	text.row = 1
end function

if locals == globals then
	editText _source
end if

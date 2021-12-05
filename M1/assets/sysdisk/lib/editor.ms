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
pal.topBarText = "#000044"

lineLen = function(lineNum)
	if lineNum < data.len then return data[lineNum].len
	return 0
end function

drawTopBar = function()
	text.backColor = pal.topBar
	text.color = pal.topBarText
	text.row = screenH-1; text.column = 0
	print " " + _sourceFile
	print " " * (screenW - 10 - text.column)
	print "Line: " + cursorY
	print " " * (screenW - text.column)
end function

refreshLine = function(lineNum)
	y = lineNum - scrollY  // (rows from the top)
	if y < 0 or y >= screenLines then return
	text.backColor = pal.background
	text.color = pal.text
	text.row = screenLines - 1 - y; text.column = 0
	if lineNum < data.len then
		s = data[lineNum].replace(TAB, "  ")[scrollX:]
		if s.len < screenW then s = s + " " * (screenW - s.len)
		if s.len > screenW then s = s[:screenW]
	else 
		s = " " * screenW
	end if
	if lineNum == cursorY then outer.cursorShown = false
	print s
end function

refreshDisplay = function()
	row = screenLines - 1
	i = scrollY
	text.backColor = pal.background
	text.color = pal.text
	while i < scrollY + screenLines
		text.row = row; text.column = 0
		if i < data.len then
			s = data[i].replace(TAB, "  ")[scrollX:]
			if s.len < screenW then s = s + " " * (screenW - s.len)
			if s.len > screenW then s = s[:screenW]
		else
			s = " " * screenW
		end if
		print s
		row = row - 1; i = i + 1		
	end while
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

handleKey = function(k)
	kcode = k.code
	
	byWord = key.pressed("left ctrl") or key.pressed("right ctrl") or
	key.pressed("left alt") or key.pressed("right alt")
	
	hideCursor
	if kcode == 1 then  // ctrl-A (start of line)
		outer.cursorX = 0
	else if kcode == 5 then // ctrl-E (end of line)
		outer.cursorX = lineLen(cursorY)
	else 		if kcode == 17 then  // left
		outer.cursorX = cursorX - 1
		if cursorX < 0 and cursorY > 0 then
			outer.cursorY = cursorY - 1
			outer.cursorX = lineLen(cursorY)
		end if
		if byWord then nextWord(-1)
		outer.idealCursorX = cursorX
	else if kcode == 18 then  // right
		outer.cursorX = cursorX + 1
		if cursorX > lineLen(cursorY) and cursorY < data.len then
			outer.cursorY = cursorY + 1
			outer.cursorX = 0
		end if
		if byWord then nextWord(1)
		outer.idealCursorX = cursorX
	else if kcode == 19 then  // up
		if byWord then amount = screenLines else amount = 1
		outer.cursorY = cursorY - amount
		outer.cursorX = idealCursorX
	else if kcode == 20 then  // down
		if byWord then amount = screenLines else amount = 1
		outer.cursorY = cursorY + amount
		outer.cursorX = idealCursorX
	else if kcode == 10 then  // return
		data.insert cursorY+1, data[cursorY][cursorX:]
		data[cursorY] = data[cursorY][:cursorX]
		outer.cursorY = cursorY + 1
		outer.cursorX = 0
		refreshDisplay
	else if kcode == 127 then  // forward-delete
	else if kcode == 8 then  // backspace
		if cursorX > 0 then
			data[cursorY] = data[cursorY][:cursorX-1] + data[cursorY][cursorX:]
			outer.cursorX = cursorX - 1
			refreshLine cursorY
		else if cursorY > 0 then
			x = data[cursorY-1].len
			data[cursorY-1] = data[cursorY-1] + data[cursorY]
			data.remove cursorY
			outer.cursorY = cursorY - 1
			outer.cursorX = x
			refreshDisplay
		end if
	else if kcode > 31 then  // printable key
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
	
	// for development & debugging:
	text.row = screenH-1; text.column = screenW/2 
	text.backColor = pal.topBar; text.color = "#888888"
	print kcode + (" byWord" * byWord + "       " * (not byWord))
	
end function

update = function()
	wantCursor = (time % 1 < 0.75)
	if wantCursor != cursorShown then showCursor wantCursor
	if key.available then handleKey key.get
end function

// editText: main entry point.
// Pass in a list of strings representing the text to edit.
editText = function(textToEdit)
	if textToEdit == null or not textToEdit then textToEdit = [""]
	outer.data = textToEdit
	
	text.backColor = color.clear
	text.delimiter = ""
	clear
	drawTopBar
	refreshDisplay
	
	while not key.pressed("escape")
		yield
		update
	end while
	key.clear
	
	text.delimiter = char(13)
	text.backColor = color.clear
	text.clear
	text.row = 1
end function

if locals == globals then
	edit _source
end if

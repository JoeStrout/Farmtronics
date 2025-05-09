_printOneLine = function(s)
	cutPos = 39 - text.column
	if cutPos >= s.len then
		print s
	else
		row = text.row
		print s[:cutPos]
		text.setCell 39, row, s[cutPos]
		text.setCellColor 39, row, text.color
		text.setCellBackColor 39, row, text.backColor
	end if
end function

_printMark = function(s, altColor="#00FF00")
	colors = [text.color, altColor]
	which = 0
	oldDelim = text.delimiter
	text.delimiter = ""
	while len(s)
		pos = s.indexOf("`")
		if pos == null then
			_printOneLine s
			break
		end if
		_printOneLine s[:pos]
		which = not which
		text.color = colors[which]
		s = s[pos+1:]
	end while
	text.color = colors[0]
	text.delimiter = oldDelim
	print
end function

help = function(topic)
	if topic == "" or topic == null then topic = "_default"
	if topic == "graphics" then topic = "gfx"
	lines = file.readLines("/sys/help/" + topic + ".txt")
	if not lines then
		_printMark "No help available for `""" + topic + """`."
		print "Help topics:"
		text.delimiter = ""
		for topic in file.children("/sys/help").sort
			if topic[0] == "_" then continue
			topic = topic - ".txt"
			if text.column > 24 then print char(13)
			print """" + topic + """" + " " * (19 - topic.len)
		end for
		text.delimiter = char(13)
		print
	else
		pageThrough lines, true
	end if
end function

usr = function
	_printMark "Remember, you `must` use quotation marks  around file paths.", "#FFFF00"
	_printMark "   Wrong: `cd /usr`", "#FF0000"
	_printMark "   Right: `cd ""/usr""`"
end function
sys = function
	_printMark "Remember, you `must` use quotation marks  around file paths.", "#FFFF00"
	_printMark "   Wrong: `cd /sys`", "#FF0000"
	_printMark "   Right: `cd ""/sys""`"
end function


// Convert a 0-255 number to a 2-digit hex string
hex2 = function(val)
	result = 0
	digits = "0123456789ABCDEF"
	val = floor(val)
	if val < 0 then return "00"
	if val >= 255 then return "FF"
	return digits[val / 16] + digits[val % 16]
end function

// color module (matching Mini Micro)
color = {}
color.clear     = "#00000000"
color.black 	= "#000000"
color.white		= "#FFFFFF"
color.gray		= "#808080"
color.silver	= "#C0C0C0"
color.maroon	= "#800000"
color.red		= "#FF0000"
color.olive		= "#808000"
color.yellow	= "#FFFF00"
color.orange	= "#FF8000"
color.green		= "#008000"
color.lime		= "#00FF00"
color.teal		= "#008080"
color.aqua		= "#00FFFF"
color.navy		= "#000080"
color.blue		= "#0000FF"
color.purple	= "#800080"
color.fuchsia	= "#FF00FF"
color.brown		= "#996633"
color.pink		= "#FF8080"
color.rgb = function(r, g, b)
	return "#" + hex2(r) + hex2(g) + hex2(b)
end function
color.rgba = function(r, g, b, a)
	return "#" + hex2(r) + hex2(g) + hex2(b) + hex2(a)
end function
color.lerp = function(colorA, colorB, t=0.5)
	return _lerpColor(colorA, colorB, t)
end function
// ToDo: functions below, once supporting APIs are in place
//color.toList = function(colorString)
//	return _colorToRGBA(colorString)
//end function
//color.fromList = function(rgbaList)
//	return _RGBAtoColor(rgbaList)
//end function
//color.toListHSV = function(colorString)
//	return _colorToHSVA(colorString)
//end function
//color.fromListHSV = function(hsvaList)
//	return _HSVAtoColor(hsvaList)
//end function

pageThrough = function(lines, markup=false)
	text.delimiter = ""
	printed = 0
	for i in range(0, lines.len-1)
		line = "" + lines[i]
		if markup then
			_printMark line[:40 + line.split("`").len - 1]
		else
			_printOneLine line[:40]
		end if
		print char(13)
		printed = printed + 1
		if printed > 18 and i+1 < lines.len then
			text.inverse = true
			print "[" + (lines.len - i - 1) + " more]"
			text.inverse = false
			done = false
			while not done
				k = key.get
				ikey = code(k)
				if ikey == 27 or k == "q" or k == "Q" then done = true
				if ikey == 10 or ikey == 13 or ikey == 3 or k == " " then break
				yield
			end while
			text.column = 0
			print " " * 32
			text.column = 0
			if done then break
			printed = 0
		end if
	end for
	text.delimiter = char(13)
end function

dir = function(path=".")
	spaces = " "*80
	
	dirInfo = file.info(path)
	if dirInfo == null then
		print "Invalid path"
		return
	end if
	
	lines = [dirInfo.path + " : "]
	
	files = file.children(dirInfo.path)
	if files.len == 0 then
		print lines[0]
		print "  (Directory empty)"
		return
	end if
	
	files.sort
	for i in range(0, files.len-1)
		finfo = file.info(file.child(dirInfo.path,files[i]))
		if finfo == null then
			lines.push "file.info failed on: " + file.child(path, files[i])
			continue
		end if
		namecol = (files[i]+spaces)[:17]
		sizecol = (spaces+finfo.size)[-8:]
		if finfo.isDirectory then sizecol = "     DIR"
		lines.push "  " + namecol + sizecol + "  " + finfo.date[:10]
	end for
	pageThrough lines
end function

_apiClassNames = {
Location:"Location",
}

mapName = function(map)
	if map.hasIndex("_name") then return map._name
	if _apiClassNames.hasIndex(map) then return _apiClassNames[map]
	for kv in globals
		if @kv.value == map then return @kv.key
	end for
	return null
end function

pprintToList = function(outList, obj, prefix="", indent=0, suffix="")
	s = str(@obj)
	if @obj == null then s = "null"
	if @obj isa funcRef then s = "function" + s[8:]
	if s.len + prefix.len + indent < 38 then
		outList.push " "*indent + prefix + s + suffix
		return
	end if
	if indent > 32 then
		outList.push " "*indent + prefix + "..." + suffix
		return
	end if
	if @obj isa list then
		outList.push " "*indent + prefix + "["
		for i in obj.indexes
			pprintToList outList, obj[i], "", indent + 4, "," * (i < obj.len-1)
		end for
		outList.push " "*indent + "]" + suffix
	else if @obj isa map then
		outList.push " "*indent + prefix + "{"
		entries = []; for kv in obj; entries.push kv; end for
		for i in entries.indexes
			kv = entries[i]
			value = null
			if @kv.value isa map then value = mapName(@kv.value)
			if value == null then value = @kv.value
			pprintToList outList, @value, @kv.key + ": ", indent+4, "," * (i < entries.len-1)
		end for
		outList.push " "*indent + "}" + suffix
	else
		outList.push " "*indent + prefix + s + suffix
	end if
end function

pprint = function(obj)
	lines = []
	pprintToList lines, obj
	pageThrough lines
end function

_viewImage = function(pic)
	print "Viewing of images is not implemented."
	return
end function

view = function(path)
	if not path then
		print "file name or path is required"
		return
	end if
	//	if path isa Image then
	//		_viewImage path
	//		return
	//	end if
	info = file.info(path)
	if info == null then
		print "File not found (" + path + ")"
		return
	end if
	if path[-4:] == ".png" or path[-4:] == ".jpg" or path[-5:] == ".jpeg" then
		_viewImage file.loadImage(path)
	else if path[-4:] == ".txt" or path[-3:] == ".ms" or path[-4:] == ".tsv" or path[-5:] == ".json" or path[-3:] == ".md" then
		// View a text file or MiniScript source file
		lines = file.readLines(path)
		if lines isa string then
			print lines  // (really, an error message)
		else
			// Wrap each line to 40 columns.
			i = 0
			while i < lines.len
				lines[i] = lines[i].replace(char(9), "    ")
				if lines[i].len > 40 then
					lines.insert i+1, lines[i][40:]
					lines[i] = lines[i][:40]
				end if
				i = i + 1
			end while
			pageThrough lines
		end if
	else if path[-4:] == ".wav" or path[-4:] == ".ogg" then
		print "Playing sounds is not implemented."
		//		// Play a sound
		//		snd = file.loadSound(path)
		//		if snd == null then
		//			print "Unable to load sound: " + path
		//		else
		//			snd.play
		//		end if
	else
		print "No viewer available for this file type"
	end if
end function

_sourceFile = null
_source = []
load = function(path)
	if not path then
		print "file name or path required"
		return
	end if
	if path[-3:] != ".ms" then path = path + ".ms"
	if path[0] != "/" then path = file.child(file.curdir, path)
	lines = file.readLines(path)
	if lines == null then
		print "Unable to read " + path
	else
		globals._source = lines
		globals._sourceFile = path
		print lines.len + " line" + ("s" * (lines.len != 1)) + " loaded from " + path
	end if
	return null
end function

save = function(path)
	if path == null then path = _sourceFile
	if not path then
		print "Filename not specified; unable to save"
		return
	end if
	if path[-3:] != ".ms" then path = path + ".ms"
	if path[0] != "/" then path = file.child(file.curdir, path)
	err = file.writeLines(path, _source)
	if err == null then
		globals._sourceFile = path
		print _source.len + " line" + ("s" * (_source.len != 1)) + " saved to " + path
	else
		return err
	end if
end function

source = function(fromLine=0, toLine=-1)
	if not globals.hasIndex("_source") or not _source then return
	if toLine == -1 then toLine = _source.len-1
	return _source[fromLine:toLine+1].join(char(13))
end function

edit = function
	import "editor"
	if not globals.hasIndex("_source") or not _source then globals._source = [""]
	print "Editing: " + _sourceFile
	editor.editText _source
end function

clear = function
	// Clear and reset all displays.
	text.clear
	text.row = 19
end function

_saveGlobals = function
	globals._savedGlobals = {} + globals
end function

_restoreGlobals = function
	for key in globals.indexes
		if key[0] != "_" then globals.remove(key)
	end for
	for key in _savedGlobals.indexes
		if key[0] != "_" then globals[key] = _savedGlobals[key]
	end for
end function

reset = function
	//Sound.stopAll
	globals._source = []
	globals._sourceFile = null
	_restoreGlobals
	env.importPaths = [".", "/usr/lib", "/sys/lib"]
	print "Program source cleared and reset"
	return null
end function

tip = function
	tips = file.readLines("/sys/tips.txt")
	return tips[rnd * tips.len].replace("\n", char(13))
end function

toDo = function
	import "toDo"
	toDo.present
end function
todo = @toDo	// (allow mistyping just this one time, because newbies)

mkdir = @file.makedir  // (handy and expected alias)

if me.isBot then
	// Additional functions for robots only
	here = function
		return me.position.area
	end function
	
	me.ahead = function
		f = me.facing
		pos = me.position
		if f == 0 then return here.tile(pos.x, pos.y-1)
		if f == 1 then return here.tile(pos.x+1, pos.y)
		if f == 2 then return here.tile(pos.x, pos.y+1)
		if f == 3 then return here.tile(pos.x-1, pos.y)
	end function
	
	me.currentToolName = function
		item = me.inventory[me.currentToolIndex]
		if not item then return "nothing"
		return item.name
	end function

	me.select = function(toolNameOrIndex)
		inv = me.inventory
		if toolNameOrIndex isa number then
			me.currentToolIndex = toolNameOrIndex
			print "Using " + me.currentToolName + " (index " + toolNameOrIndex + ")"
			return
		end if
		toolName = str(toolNameOrIndex).lower
		for i in inv.indexes
			if inv[i] and inv[i].name.lower == toolName then
				me.currentToolIndex = i
				print "Using " + me.currentToolName + " (index " + i + ")"
				return
			end if
		end for
		print "No tool found matching: " + toolNameOrIndex
	end function

	me.use = function(toolNameOrIndex)
		_printMark "`me.use` is deprecated and will be removed."
		_printMark "Please call `me.select` instead."
		me.select toolNameOrIndex
	end function

	// Clear ahead: try to select the right tool for whatever is in front
	// of the me, and then use that tool up to 20 times to try to clear
	// it.  Returns 1 (true) on success, 0 (false) on failure.
	me.clearAhead = function
		obstacle = me.ahead
		if not obstacle then return true
		what = obstacle.type
		if obstacle.hasIndex("name") then what = obstacle.name
		print "Clearing: " + what
		if what == "Grass" or what == "Weeds" then
			me.statusColor = "#66FF66"
			me.select "Scythe"
		else if what == "Stone" or what == "HoeDirt" then
			me.statusColor = "#8888FF"
			me.select "Pickaxe"
		else
			me.statusColor = "#FF8800"
			me.select "Axe"
		end if
		for tries in range(19)
			if not me.ahead then return true
			me.useTool
			if me.energy <= 0 then return false
			wait 0.2
		end for
		return false	// couldn't clear the obstacle
	end function

	// Attempt to clear any obstacle ahead, and then move forward, for the
	// given total distance.  Returns 1 (true) on success, 0 (false) on failure.
	me.clearAndMove = function(dist=1)
		for i in range(dist-1)
			pos = me.position
			while me.position == pos
				if not me.clearAhead then return false
				me.forward
			end while
		end for
		return true
	end function
	
end if

// Stuff that applies both to bots and to the home computer
me.screenColor = "#333399"

_saveGlobals

print
_printMark "Enter `help` for help."
print

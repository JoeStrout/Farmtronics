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

usr = function()
	_printMark "Remember, you `must` use quotation marks  around file paths.", "#FFFF00"
	_printMark "   Wrong: `cd /usr`", "#FF0000"
	_printMark "   Right: `cd ""/usr""`"
end function
sys = function()
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

// ToDo: functions below, once supporting APIs are in place
//color.lerp = function(colorA, colorB, t=0.5)
//	return _lerpColor(colorA, colorB, t)
//end function
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
		if markup then _printMark line[:40] else _printOneLine line[:40]
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
	if path == "/" then
		for p in ["/usr", "/usr2", "/sys"]
			if file.info(p) == null then continue
			print (p+spaces)[:24] + "DIR"
		end for
		return
	end if
	
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
	if s.len + prefix.len + indent < 65 then
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

edit = function()
	import "editor"
	if not globals.hasIndex("_source") or not _source then globals._source = [""]
	editor.editText _source
end function

clear = function()
	// Clear and reset all displays.
	text.clear
	text.row = 19
end function

_saveGlobals = function()
	globals._savedGlobals = {} + globals
end function

_restoreGlobals = function()
	for key in globals.indexes
		if key[0] != "_" then globals.remove(key)
	end for
	for key in _savedGlobals.indexes
		if key[0] != "_" then globals[key] = _savedGlobals[key]
	end for
end function

reset = function()
	//Sound.stopAll
	globals._source = []
	globals._sourceFile = null
	_restoreGlobals
	env.importPaths = [".", "/usr/lib", "/sys/lib"]
	print("Program source cleared and reset")
	return null
end function

tip = function()
	tips = file.readLines("/sys/tips.txt")
	return tips[rnd * tips.len].replace("\n", char(13))
end function

_saveGlobals

toDo = function()
	import "toDo"
	toDo.present
end function
todo = @toDo	// (allow mistyping just this one time, because newbies)

if bot then
	// Additional bot functions
	here = function()
		return bot.position.area
	end function
	
	scan = function()
		here = bot.position.area
		for x in range(0, here.width-1)
			for y in range(0, here.height-1)
				t = here.tile(x,y)
				if not t then continue
				print x+","+y + ": " + t
			end for
		end for
	end function
	
	bot.ahead = function()
		f = bot.facing
		pos = bot.position
		if f == 0 then return here.tile(pos.x, pos.y-1)
		if f == 1 then return here.tile(pos.x+1, pos.y)
		if f == 2 then return here.tile(pos.x, pos.y+1)
		if f == 3 then return here.tile(pos.x-1, pos.y)
	end function
	
	bot.use = function(toolNameOrIndex)
		if toolNameOrIndex isa number then
			bot.currentToolIndex = toolNameOrIndex
			print "Using " + inv[toolNameOrIndex].name + " (index " + toolNameOrIndex + ")"
			return
		end if
		toolName = str(toolNameOrIndex).lower
		inv = bot.inventory
		for i in inv.indexes
			if inv[i] and inv[i].name.lower == toolName then
				bot.currentToolIndex = i
				print "Using " + inv[i].name + " (index " + i + ")"
				return
			end if
		end for
		print "No tool found matching: " + toolNameOrIndex
	end function
	
	bot.clearAhead = function()
		obstacle = bot.ahead
		if not obstacle then return
		what = obstacle.type
		if obstacle.hasIndex("name") then what = obstacle.name
		print "Clearing: " + what
		if what == "Grass" or what == "Weeds" then
			bot.statusColor = "#66FF66"
			bot.use "Scythe"
		else if what == "Stone" then
			bot.statusColor = "#8888FF"
			bot.use "Pickaxe"
		else
			bot.statusColor = "#FF8800"
			bot.use "Axe"
		end if
		while bot.ahead
			bot.useTool
		end while
	end function
	
	bot.clearAndMove = function(dist=1)
		for i in range(dist-1)
			pos = bot.position
			while bot.position == pos
				bot.clearAhead
				bot.forward
			end while
		end for
	end function
	
	bot.clearGrid = function(width, height)
		for h in range(0, height-1)
			bot.clearAndMove width
			if h % 2 then
				bot.right; bot.clearAndMove; bot.right
			else
				bot.left; bot.clearAndMove; bot.left
			end if
		end for
	end function
	
	bot.till = function()
		bot.use "Hoe"
		bot.useTool
	end function
	
	bot.water = function()
		bot.use "Watering Can"
		bot.useTool
	end function
	
	bot.plant = function(seedName)
		bot.use seedName
		bot.placeItem
	end function
	
end if

// Stuff that applies both to bots and to the home computer
screenColor = "#333399"

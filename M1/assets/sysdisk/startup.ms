_printMark = function(s, altColor="#00FF00")
	colors = [text.color, altColor]
	which = 0
	oldDelim = text.delimiter
	text.delimiter = ""
	while len(s)
		pos = s.indexOf("`")
		if pos == null then
			print s
			break
		end if
		print s[:pos]
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
	lines = []  // ToDo:  _fileReadLines("/sys/help/" + topic + ".txt")
	if not lines then
		_printMark "No help available for `""" + topic + """`."
		print "Help topics:"
		text.delimiter = ""
		for topic in file.children("/sys/help").sort
			if topic[0] == "_" then continue
			topic = topic - ".txt"
			if text.column > 44 then print char(13)
			print """" + topic + """" + " " * (20 - topic.len)
		end for
		text.delimiter = char(13)
		print
	else
		for line in lines
			_printMark line
		end for
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

pageThrough = function(lines)
	text.delimiter = ""
	printed = 0
	for i in range(0, lines.len-1)
		line = "" + lines[i]
		print line[:39]
		if line.len > 39 then text.setCell 39,text.row, line[39]
		print char(13)
		printed = printed + 1
		if printed > 24 and i+1 < lines.len then
			text.inverse = true
			print "[" + (lines.len - i - 1) + " more]"
			text.inverse = false
			done = false
			while not done
				k = input  // ToDo: should be key.get
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


if bot then
	// Additional bot functions
	here = function()
		return bot.position.area
	end function

	scan = function()
		here = position.area
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

	bot.equip = function(toolName)
		inv = bot.inventory
		toolName = toolName.lower
		for i in inv.indexes
			if inv[i] and inv[i].name.lower == toolName then
				bot.currentToolIndex = i
				print "Equipped " + inv[i].name + " (index " + i + ")"
				return
			end if
		end for
		print "No tool found matching: " + toolName
	end function

	bot.clearAhead = function()
		obstacle = bot.ahead
		if not obstacle then return
		what = obstacle.type
		if obstacle.hasIndex("name") then what = obstacle.name
		if what == "HoeDirt" then return
		print "Clearing: " + what
		if what == "Grass" or what == "Weeds" then
			bot.statusColor = "#66FF66"
			bot.equip "Scythe"
		else if what == "Stone" then
			bot.statusColor = "#8888FF"
			bot.equip "Pickaxe"
		else
			bot.statusColor = "#FF8800"
			bot.equip "Axe"
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

	bot.listInventory = function()
		inv = bot.inventory
		for i in inv.indexes
			print inv[i].name + " (index " + i + ")"
	end function

	bot.till = function()
		bot.equip("Hoe")
		bot.useTool
	end function

	bot.tillGrid = function(width, height)
		for h in range(0, height-1)
			for j in range(0,width-1)
				bot.clearAhead
				bot.till
				bot.forward
			end for
			if h % 2 then
				bot.right; bot.clearAhead; bot.till; bot.forward; bot.right
			else
				bot.left; bot.clearAhead; bot.till; bot.forward; bot.left
			end if
		end for
	end function

	bot.water = function()
		bot.equip("Watering Can")
		bot.useTool
	end function

	bot.waterGrid = function(height, width)
		for h in range(0, height-1)
			for j in range(0,width-1)
				bot.water
				bot.forward
			if h % 2 then
				bot.right; bot.water; bot.forward; bot.right
			else
				bot.left; bot.water; bot.forward; bot.left
			end if
		end for
	end function

	bot.plantParsnip = function()
		print "Planting Parsnip"
		bot.equip "Parsnip Seeds"
		bot.plantCrop
	end function

	bot.plant = function()
		for i in range(0,3)
			bot.plantParsnip
			bot.right
		end for
		bot.plantParsnip
	end function

	bot.plantGrid = function(heigh, width)
		for h in range(0, height-1)
			for j in range(0,width-1)
				bot.forward
				bot.plant
			if h % 2 then
				bot.right; bot.forward; bot.plant; bot.right
			else
				bot.left; bot.forward; bot.plant; bot.left
			end if
		end for
	end function

	bot.clearAndPlantLine = function(distance)
		for h in range(0, distance-1)
			bot.plant
			bot.clearAhead
			bot.till
			bot.water
			bot.forward
			bot.plant
		end for
	end function

	bot.clearAndPlantGrid = function(width, height)
		bot.clearAndPlantLine(width)
		for h in range(0, height-2)
			if h % 2 then
				bot.right; bot.plant; bot.clearAhead; bot.till; bot.water; bot.forward; bot.right; bot.plant
			else
				bot.left; bot.plant; bot.clearAhead; bot.till; bot.water; bot.forward; bot.left; bot.plant
			end if
			bot.clearAndPlantLine(width-2)
		end for
	end function

end if

// Stuff that applies both to bots and to the home computer
screenColor = "#333399"

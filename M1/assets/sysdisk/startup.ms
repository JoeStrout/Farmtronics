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
		print "Clearing: " + what
		if what == "Grass" or what == "Weed" then
			bot.statusColor = "#66FF66"
			bot.equip "Scythe"
		else if what == "Stone" then
			bot.statusColor = "#8888FF"
			bot.equip "Pickaxe"
		else
			bot.statusColor = "#FF8800"
			bot.equip "Axe"
		end if
		useTool
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
end if

// Stuff that applies both to bots and to the home computer
screenColor = "#333399"

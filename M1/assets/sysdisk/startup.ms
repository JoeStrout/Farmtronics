here = function()
	return position.area
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

ahead = function()
	f = facing
	pos = position
	if f == 0 then return here.tile(pos.x, pos.y-1)
	if f == 1 then return here.tile(pos.x+1, pos.y)
	if f == 2 then return here.tile(pos.x, pos.y+1)
	if f == 3 then return here.tile(pos.x-1, pos.y)
end function

equip = function(toolName)
	inv = inventory
	toolName = toolName.lower
	for i in inv.indexes
		if inv[i] and inv[i].name.lower == toolName then
			globals.currentToolIndex = i
			print "Equipped " + inv[i].name + " (index " + i + ")"
			return
		end if
	end for
	print "No tool found matching: " + toolName
end function

clearAhead = function()
	obstacle = ahead
	if not obstacle then return
	what = obstacle.type
	if obstacle.hasIndex("name") then what = obstacle.name
	print "Clearing: " + what
	if what == "Grass" or what == "Weed" then
		globals.statusColor = "#66FF66"
		equip "Scythe"
	else if what == "Stone" then
		globals.statusColor = "#8888FF"
		equip "Pickaxe"
	else
		globals.statusColor = "#FF8800"
		equip "Axe"
	end if
	useTool
end function

clearAndMove = function(dist=1)
	for i in range(dist-1)
		pos = position
		while position == pos
			clearAhead
			forward
		end while
	end for
end function

clearGrid = function(width, height)
	for h in range(0, height-1)
		clearAndMove width
		if h % 2 then
			right; clearAndMove; right
		else
			left; clearAndMove; left
		end if
	end for
end function

screenColor = "#333399"

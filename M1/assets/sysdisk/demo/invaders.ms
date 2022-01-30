// ASCII Invaders

import "listUtil"
import "textUtil"

//----------------------------------------
// Test/debug stuff
clear
text.color = color.gray
if version.hostName == "Mini Micro" then
	for row in range(0,26)
		for col in range(0, 68)
			text.setCellColor col, row, text.color
			if row >= 20 or col >= 40 then text.setCell col, row, "X"
		end for
	end for
end if

debugMouse = function
	if not mouse.button then return
	col = textUtil.textCol(mouse.x)
	row = textUtil.textRow(mouse.y)
	text.setCell col, row, "X"
	err = aliens.removeAlienAt(col, row)
	if err then
		text.row = 22; text.column=0; print err
	end if
end function

//----------------------------------------
// Constants
//
EOL = char(13)

alien1a = ["/•\", "/""\"]
alien1b = ["/•\", "\""/"]

alien2a = ["°π°", "/~\"]
alien2b = ["°π°", "|~|"]

alien3a = ["(☐)", "v^v"]
alien3b = ["(☐)", "^v^"]

alienColors = [color.lime, color.aqua, color.fuchsia]

playerShotChar = "¡"
alienShotChar = "!"

//----------------------------------------
// General utility functions
//
leftTrimmable = function(s)
	for i in s.indexes
		if s[i] != " " then return i
	end for
	return s.len
end function

rightTrimmable = function(s)
	for i in range(s.len-1)
		if s[i] != " " then return s.len-i-1
	end for
	return s.len
end function

min = function(a,b)
	if a < b then return a else return b
end function

max = function(a,b)
	if a > b then return a else return b
end function


//----------------------------------------
// Aliens
//
lineColors = []
alienLinesA = []
alienLinesA.push (alien1a[0] + "  ") * 6; lineColors.push alienColors[0]
alienLinesA.push (alien1a[1] + "  ") * 6; lineColors.push alienColors[0]
alienLinesA.push ""; lineColors.push color.white
alienLinesA.push (alien2a[0] + "  ") * 6; lineColors.push alienColors[1]
alienLinesA.push (alien2a[1] + "  ") * 6; lineColors.push alienColors[1]
alienLinesA.push ""; lineColors.push color.white
alienLinesA.push (alien3a[0] + "  ") * 6; lineColors.push alienColors[2]
alienLinesA.push (alien3a[1] + "  ") * 6; lineColors.push alienColors[2]
alienLinesB = []
alienLinesB.push (alien1b[0] + "  ") * 6
alienLinesB.push (alien1b[1] + "  ") * 6
alienLinesB.push ""
alienLinesB.push (alien2b[0] + "  ") * 6
alienLinesB.push (alien2b[1] + "  ") * 6
alienLinesB.push ""
alienLinesB.push (alien3b[0] + "  ") * 6
alienLinesB.push (alien3b[1] + "  ") * 6

aliens = {}
aliens.lines = [alienLinesA[:], alienLinesB[:]]
aliens.width = alienLinesA[0].len
aliens.count = 24
aliens.x = 20 - floor(aliens.width/2)
aliens.y = 16
aliens.modeB = 0
aliens.nextTickTime = 0
aliens.timePerTick = 0.5
aliens.xdir = -1

aliens.draw = function()
	text.delimiter = ""
	lines = self.lines[self.modeB]
	for i in lines.indexes
		text.color = lineColors[i]
		text.row = self.y - i; text.column = 0
		print " " * self.x
		print lines[i]
		print " " * (40 - text.column)
	end for
	text.delimiter = EOL
end function

aliens.update = function()
	if time < self.nextTickTime then return
	self.nextTickTime = time + self.timePerTick
	self.modeB = not self.modeB
	if self.modeB then
		if (self.x == 0 and self.xdir < 0) or
		  (self.x + self.width == 40 and self.xdir > 0) then
			// reverse direction and move down
			text.row = self.y; text.column = 0; print " "*40
			self.y = self.y - 1
			self.xdir = -self.xdir
		else
			self.x = self.x + self.xdir			
		end if
	end if
	self.draw
end function

aliens.trim = function
	ltrim = 999
	rtrim = 999
	for line in self.lines[0]
		if not line then continue
		ltrim = min(ltrim, leftTrimmable(line))
		rtrim = min(rtrim, rightTrimmable(line))
	end for
	if not ltrim and not rtrim then return
	text.row=20; text.column=0; print "Trimming " + ltrim + " and " + rtrim
	for mode in [0,1]
		lines = self.lines[mode]
		for i in lines.indexes
			if rtrim then
				lines[i] = lines[i][ltrim : -rtrim]
			else
				lines[i] = lines[i][ltrim:]
			end if
		end for
	end for
	self.width = self.width - ltrim - rtrim
	self.x = self.x + ltrim
end function

aliens.removeAlienAt = function(col, row, explode=true)
	// figure out which row in our lines is affected
	i = self.y - row
	if i < 0 or i >= self.lines[0].len then return
	if i % 3 == 2 then return  // between alien rows
	i0 = floor(i/3) * 3
	// and, which alien column is affected
	j = col - self.x
	if j < 0 or j >= self.lines[0][0].len then return
	if j % 5 > 2 then return	// between columns
	j0 = floor(j / 5) * 5
	j1 = j0 + 3
	// confirm that we still have something there
	if self.lines[0][i0][j0:j1] == "   " then return
	// replace that alien with spaces
	for mode in [0,1]
		for i in [i0, i0+1]
			self.lines[mode][i] = self.lines[mode][i][:j0] +
			  "   " + self.lines[mode][i][j1:]
		end for
	end for
	if explode then
		exp = new Explosion
		exp.init self.x + j0, self.y - i0
	end if
	self.trim
	self.count = self.count - 1
end function

//----------------------------------------
// Player Ship
//
ship = {}
ship.x = 20
ship.ascii = ["  " + char(134) + "^" + char(135) + "  ", 
char(134)+char(57856) + "/=\" + char(57857)+char(135)]
ship.shotWasPressed = true

ship.draw = function()
	text.color = color.yellow
	text.delimiter = ""
	text.row = 1; text.column = self.x-2; print self.ascii[0]
	text.row = 0; text.column = self.x-3; print " "
	text.column = self.x-2; print self.ascii[1]
	if text.column < 40 then print " "
	text.row=20; text.delimiter=EOL
end function

ship.update = function
	dx = 0
	if key.pressed("left") or key.pressed("a") then
		dx = -1
	end if
	if key.pressed("right") or key.pressed("d") then
		dx = dx + 1
	end if
	if dx then
		self.x = self.x + dx
		if self.x < 1 then self.x = 1
		if self.x > 37 then self.x = 37
		self.draw
	end if
	
	shotPressed = key.pressed("space") or
	 key.pressed("left shift") or key.pressed("right shift")
	if shotPressed and not self.shotWasPressed then
		shot = new PlayerShot
		shot.init
	end if
	self.shotWasPressed = shotPressed
end function

//----------------------------------------
// Explosion (when an alien is destroyed)
//
Explosion = {}
Explosion.instances = []
Explosion.init = function(left, top)
	self.left = left
	self.top = top
	self.stage = 0
	self.draw
	Explosion.instances.push self
end function

Explosion.draw = function
	if self.stage < 3 then
		art = ["\*/", "/*\"]
	else if self.stage < 5 then
		art = ["***", "***"]
	else if self.stage < 8 then
		art = [".:.", ".:."]
	else if self.stage < 10 then
		art = ["` '", ", ."]
	else
		art = ["   ", "   "]
	end if
	text.row = self.top; text.column = self.left
	text.color = color.lerp(color.yellow, color.red, self.stage/10)
	text.print art[0]
	text.column = self.left
	text.print art[1]
end function
	
Explosion.update = function
	self.stage = self.stage + 1
	self.draw
	if self.stage >= 10 then
		Explosion.instances.removeVal self
	end if
end function

Explosion.updateAll = function
	for exp in Explosion.instances
		exp.update
	end for
end function


//----------------------------------------
// Player Shot
//
PlayerShot = {}
PlayerShot.hasHit = false
PlayerShot.hitFrames = 0
PlayerShot.instances = []

PlayerShot.init = function
	self.x = ship.x
	self.y = 2
	self.draw
	PlayerShot.instances.push self
end function

PlayerShot.draw = function
	text.setCell self.x, self.y, playerShotChar
	text.setCellColor self.x, self.y, color.yellow
end function

PlayerShot.handleHit = function
	c = text.cell(self.x, self.y)
	text.setCell self.x, self.y, "*"
	text.setCellColor self.x, self.y, color.orange
	self.hasHit = true
	if c == "o" then  // hit shield
		return
	else  // hit alien
		aliens.removeAlienAt self.x, self.y
	end if
end function

PlayerShot.update = function
	if self.hasHit then
		self.hitFrames = self.hitFrames + 1
		if self.hitFrames == 4 then
			text.setCell self.x, self.y, " "
			PlayerShot.instances.removeVal self
		end if
	else
		text.setCell self.x, self.y, " "
		self.y = self.y + 1
		if self.y >= 20 then
			PlayerShot.instances.removeVal self
			return
		end if
		if text.cell(self.x, self.y) != " " then
			self.handleHit
		else
			self.draw
		end if
	end if
end function

PlayerShot.updateAll = function
	for shot in PlayerShot.instances
		shot.update
	end for
end function

//----------------------------------------
// Level set-up
//

aliens.trim
aliens.draw
ship.draw

// draw shields
text.color = color.red
text.row = 5; text.column = 0
print " "*3 + "  ooo   " * 4
print " "*3 + " ooooo  " * 4
print " "*3 + " oo oo  " * 4

//----------------------------------------
// Main Loop
//
while not key.pressed("escape")
	yield
	aliens.update
	ship.update
	PlayerShot.updateAll
	Explosion.updateAll
	debugMouse
end while
key.clear
text.row = 0
text.color = color.orange


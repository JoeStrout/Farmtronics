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

drawScore = function
	s = ("00000" + score)[-5:]
	text.row = 19; text.column = 34; text.color = color.yellow
	print s
end function

drawLives = function
	s = char(9829) * (lives - 1) + "   "
	text.row = 19; text.column = 2; text.color = color.red
	print s
end function

addToScore = function(points)
	globals.score = score + points
	drawScore
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

aliens.init = function()
	self.lines = [alienLinesA[:], alienLinesB[:]]
	self.width = alienLinesA[0].len
	self.count = 18
	self.x = 20 - floor(self.width/2)
	self.y = 16
	self.modeB = 0
	self.nextTickTime = 0
	self.timePerTick = 0.5
	self.nextFireTime = 1
	self.timePerShot = 0.7
	self.xdir = -1
	self.trim
end function


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
	if self.count < 1 then return
	if time > self.nextFireTime then
		self.fire
		self.nextFireTime = time + self.timePerShot
	end if
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
	if ltrim or rtrim then
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
	end if
	for i in range(self.lines[0].len - 1)
		s = self.lines[0][i]
		if not s or s == " " * s.len then
			self.lines[0].remove i
			self.lines[1].remove i
		else
			break
		end if
	end for
end function

aliens.removeAlienAt = function(col, row, explode=true)
	// figure out which row in our lines is affected
	i = self.y - row
	if i < 0 or i >= self.lines[0].len then return false
	if i % 3 == 2 then return false // between alien rows
	i0 = floor(i/3) * 3
	// and, which alien column is affected
	j = col - self.x
	if j < 0 or j >= self.lines[0][0].len then return false
	if j % 5 > 2 then return false	// between columns
	j0 = floor(j / 5) * 5
	j1 = j0 + 3
	// confirm that we still have something there
	if self.lines[0][i0][j0:j1] == "   " then return false
	// replace that alien with spaces
	for mode in [0,1]
		for i in [i0, i0+1]
			self.lines[mode][i] = self.lines[mode][i][:j0] +
			  "   " + self.lines[mode][i][j1:]
		end for
	end for
	if explode then
		exp = new Explosion
		type = alienColors.indexOf(lineColors[i0])
		points = [500, 250, 100][type]
		exp.init self.x + j0, self.y - i0, points
		addToScore points
	end if
	self.trim
	self.count = self.count - 1
	self.timePerTick = 0.025 * self.count
	return true
end function

aliens.fire = function
	// pick a random row and column in our block...
	i = floor(self.lines[0].len * rnd / 3) * 3
	line = self.lines[0][i]
	j = floor(rnd*line.len/5) * 5
	// check if we actually have an alien there
	if not line or line[j] == " " then return // ...nope!
	// spawn the shot
	shot = new Shot
	shot.initFromAlien self.x+j+1, self.y-i-2, lineColors[i]
end function

//----------------------------------------
// Player Ship
//
ship = {}
ship.x = 20
ship.dead = false
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
	if self.dead then return
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
		shot = new Shot
		shot.initFromPlayer
	end if
	self.shotWasPressed = shotPressed
end function

ship.die = function
	exp = new Explosion
	exp.init self.x - 2, 1
	exp = new Explosion
	exp.init self.x, 1
	self.dead = true
	self.resetTime = time + 3
end function

//----------------------------------------
// Explosion (when an alien is destroyed)
//
Explosion = {}
Explosion.instances = []
Explosion.init = function(left, top, score)
	self.left = left
	self.top = top
	self.stage = 0
	self.score = score
	self.draw
	Explosion.instances.push self
end function

Explosion.draw = function
	if self.stage < 6 then
		art = ["\*/", "/*\"]
	else if self.stage < 10 then
		art = ["***", "***"]
	else if self.stage < 16 then
		art = [".:.", ".:."]
	else if self.stage < 20 then
		art = ["` '", ", ."]
	else if self.score > 0 then
		art = [str(self.score), "   "]
	else
		art = ["   ", "   "]
	end if
	if self.stage < 20 then
		text.color = color.lerp(color.yellow, color.red, self.stage/20)
	else
		text.color = color.lerp(color.yellow, color.black, self.stage/20-1)
	end if
	text.delimiter = ""
	text.row = self.top; text.column = self.left
	text.print art[0]
	text.row = self.top - 1; text.column = self.left
	text.print art[1]
	text.delimiter = EOL
end function
	
Explosion.update = function
	self.stage = self.stage + 1
	self.draw
	if self.stage >= 40 then
		Explosion.instances.removeVal self
	end if
end function

Explosion.updateAll = function
	for exp in Explosion.instances
		exp.update
	end for
end function


//----------------------------------------
// Player and Alien Shots
//
Shot = {}
Shot.hasHit = false
Shot.instances = []
Shot.framesPerMove = 3
Shot.frames = 0

Shot.initFromPlayer = function
	self.x = ship.x
	self.y = 2
	self.dy = 1
	self.char = playerShotChar
	self.color = color.yellow
	self.draw
	Shot.instances.push self
end function

Shot.initFromAlien = function(x,y, color)
	self.x = x; self.y = y
	self.dy = -1
	self.char = alienShotChar
	self.color = color
	self.draw
	Shot.instances.push self
end function

Shot.draw = function
	text.setCell self.x, self.y, self.char
	text.setCellColor self.x, self.y, self.color
end function

Shot.handleHit = function
	c = text.cell(self.x, self.y)
	if self.dy < 0 then
		// alien shots collide only with shield or player
		if self.y > 1 and c != "o" then return
	end if
	text.setCell self.x, self.y, "*"
	text.setCellColor self.x, self.y, color.orange
	self.hasHit = true
	self.frames = 0
	if c == "o" then  // hit shield
		return
	else if self.y < 2 then  // hit player
		ship.die
	else  // hit alien (maybe)
		if not aliens.removeAlienAt(self.x, self.y) then
			self.hasHit = false
			self.draw
		end if
	end if
end function

Shot.update = function
	self.frames = self.frames + 1
	if self.hasHit then
		if self.frames == 4 then
			text.setCell self.x, self.y, " "
			Shot.instances.removeVal self
		end if
	else if self.frames >= self.framesPerMove then
		self.frames = 0
		c = text.cell(self.x, self.y)
		if c == self.char or c == "*" then
			text.setCell self.x, self.y, " "
		end if
		self.y = self.y + self.dy
		if self.y >= 20 or self.y < 0 then
			Shot.instances.removeVal self
			return
		end if
		if text.cell(self.x, self.y) != " " then
			self.handleHit
		else
			self.draw
		end if
	end if
end function

Shot.updateAll = function
	for shot in Shot.instances
		shot.update
	end for
end function

//----------------------------------------
// Game/Level set-up methods
//

startLevel = function
	text.clear
	drawScore
	drawLives
	aliens.init
	aliens.draw
	ship.dead = false
	ship.draw

	// draw shields
	text.color = color.red
	text.row = 5; text.column = 0
	print "  " + "   ooo   " * 4
	print "  " + "  ooooo  " * 4
	print "  " + "  oo oo  " * 4
end function

startGame = function
	globals.score = 0
	globals.lives = 3
	startLevel
end function

endGame = function
	text.row = 12; text.column = 15
	text.color = color.yellow
	print "GAME OVER"
	wait 2
	text.row = 1
	exit
end function

//----------------------------------------
// Main Loop
//
startGame
while not key.pressed("escape")
	yield
	aliens.update
	ship.update
	Shot.updateAll
	Explosion.updateAll
	if ship.dead and time > ship.resetTime then
		globals.lives = globals.lives - 1
		if globals.lives < 1 then
			endGame
		else
			startLevel
		end if
	end if
end while
key.clear
text.row = 0
text.color = color.orange


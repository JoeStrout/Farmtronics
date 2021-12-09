// Pig the Dice Game for two or more players.

if version.hostName != "Mini Micro" then
	// This code can work even in plain vanilla MiniScript,
	// if we just define a couple of functions:
	clear = null
	key = {}
	key.get = function()
		s = input("")
		if s == "" then s = char(13)
		return s[0]
	end function
	text = {}
end if

// Print title and instructions
print "Pig Dice"
print "   a dice game for 2 or more players"
print
print "Each player takes turns rolling a 6-sided die 1 or more times."
print "On your turn, you accumulate points equal to the sum of your rolls."
print "But if you roll a 1, you lose all the points accumulated on that"
print "turn.  So you have to ask yourself:"
print
print "   Do you feel lucky?"
print
print "First player to 100 points wins!"
print

// Define the Player class
Player = {}
Player.score = 0
Player.doTurn = function()
	rolls = 0
	pot = 0
	print self.name + "'s Turn!"
	while true
		if self.score + pot >= goal then
			print "   " + self.name.upper + " WINS WITH " + (self.score + pot) + "!"
			exit
		else
			text.delimiter = ""
			if rolls == 0 then
				print self.name + " ready!  Press R to roll! "
			else
				print self.name + ", you have " + pot + " in the pot.  [R]oll or [H]old? "
			end if
			inp = key.get
			text.delimiter = char(13)
		end if
		if inp == "" or inp[0].upper == "R" then
			print "Roll"
			die = ceil(rnd*6)
			// The Mini Micro font includes the Unicode symbols for 6-sided dice.
			// They are char(9856) through char(9861).  So we can get the right
			// symbol for the die we rolled as:
			dieFace = char(9855 + die)
			if die == 1 then
				print "   You roll a " + dieFace + ".  Busted!"
				return
			else
				pot = pot + die
				rolls = rolls + 1
				print "   You roll a " + dieFace + ", for a total of " + pot + "."
			end if
		else
			print "Hold"
			self.score = self.score + pot
			return
		end if
	end while
end function

// Create the players
playerCount = val(input("Enter number of players (2 or more): "))
if playerCount < 2 then
	print "OK, come back when you have 2 or more players!"
	exit
end if
players = []
for i in range(0, playerCount-1)
	p = new Player
	p.name = "Player " + (i+1)
	s = input("Enter " + p.name + " name (or hit Return for " + p.name + "): ")
	if s != "" then p.name = s
	players.push p
end for

// Set the goal (adjust this for a shorter or longer game)
goal = 100

// Main loop
while true
	for player in players
		print
		scores = []
		for p in players
			scores.push p.name + ": " + p.score
		end for
		print scores.join("  |  ")
		
		player.doTurn
		if player.score >= goal then break
	end for
end while

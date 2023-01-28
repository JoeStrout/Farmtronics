// This module contains names for all the special characters
// supported by Farmtronics.
//
// To use:
//	1. add this line near the top of your program:
//     import "chars"
//  2. in your code, just write something like:
//     print chars.checkedBox


// These display as arrows, and are also the characters you would get
// from key.get if the user presses the arrow keys on the keyboard:
left = char(17)
right = char(18)
up = char(19)
down = char(20)

// These are the official Unicode code points for the arrows:
leftArrow = char(8592)
upArrow = char(8593)
rightArrow = char(8594)
downArrow = char(8595)

// Upside-down question mark:
unprintable = char(21)

// Button caps: turn on inverse mode, and print these on either side
// of a caption, and it looks like a button:
buttonCapLeft = char(57856)
buttonCapRight = char(57857)

// A stick figure in various states of emotion:
figureStanding = char(57872)
figureWaitingToeDown = char(57873)
figureWaitingToeUp = char(57874)
figureAkimbo = char(57875)

// Some misc. glyphs:
mu = char(181)
invertedBang = char(161)
copyright = char(169)
lineEndSymbol = char(172)
registered = char(174)
degrees = char(176)
invertedQuestion = char(191)
multSign = char(215)
divSign = char(247)
pi = char(960)
tau = char(964)
bullet = char(8226)
tree = char(57888)
ellipsis = char(8230)

// Checkboxes:
emptyBox = char(9744)
checkedBox = char(9745)
boxWithX = char(9746)

// Card suits:
spade = char(9824)
club = char(9827)
heart = char(9829)
diamond = char(9830)

// Faces of a 6-sided die:
dieFace = {}
dieFace[1] = char(9856)
dieFace[2] = char(9857)
dieFace[3] = char(9858)
dieFace[4] = char(9859)
dieFace[5] = char(9860)
dieFace[6] = char(9861)

// Invisible characters that, when printed, make something happen:
inverseOn = char(134)
inverseOff = char(135)
bell = char(7)

// Other miscellaneous control characters:
backup = char(8)
tab = char(9)
esc = char(27)
returnKey = char(10)
enterKey = char(3)

// Collections:
printable = [leftArrow, upArrow, rightArrow, downArrow, unprintable,
	buttonCapLeft, buttonCapRight, 
	figureStanding, figureWaitingToeDown, figureWaitingToeUp, figureAkimbo,
	mu, invertedBang, copyright, lineEndSymbol, registered, degrees, 
	invertedQuestion, multSign, divSign, pi, tau, bullet, tree, ellipsis,
	emptyBox, checkedBox, boxWithX,
	spade, club, heart, diamond] + dieFace.values

allPrintable = []
for code in range(32,126) + [161, 165, 169, 172, 174, 176] + range(191,255)
	allPrintable.push char(code)
end for
allPrintable = allPrintable + printable

if locals == globals then
	text.clear
	text.row = 20
	print "Special characters:"
	printAlign = function(label, text)
		print " " * (25 - label.len) + label + ": " + text
	end function
	printAlign "left or leftArrow", leftArrow
	printAlign "up or upArrow", upArrow
	printAlign "right or rightArrow", rightArrow
	printAlign "down or rightArrow", downArrow
	printAlign "unprintable", unprintable
	printAlign "buttonCapLeft", buttonCapLeft
	printAlign "buttonCapRight", buttonCapRight
	printAlign "figureStanding", figureStanding
	printAlign "figureWaitingToeDown", figureWaitingToeDown
	printAlign "figureWaitingToeUp", figureWaitingToeUp
	printAlign "figureAkimbo", figureAkimbo
	printAlign "mu, pi, tau", mu + " " + pi + " " + tau
	printAlign "bullet, ellipsis, degrees", [bullet, ellipsis, degrees].join
	printAlign "tree", tree
	printAlign "emptyBox", emptyBox
	printAlign "checkedBox", checkedBox
	printAlign "boxWithX", boxWithX
	printAlign "spade club heart diamond", [spade, club, heart, diamond].join
	printAlign "dieFace[1] - dieFace[6]", [dieFace[1], dieFace[2], dieFace[3],
			 dieFace[4], dieFace[5], dieFace[6]].join
	
	// Since this code has been run in the global scope, it's hidden the
	// intrinsic `pi` function, which will cause weird bugs in other code. So:
	globals.remove "pi"
end if

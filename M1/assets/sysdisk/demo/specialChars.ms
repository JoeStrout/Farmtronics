// A simple program to display all the special characters
// in the built-in Mini Micro font.

codes = []
codes.push [7, "(Beep)"]
codes.push [9, "(Tab)"]
codes.push [13, "(Line Break)"]
codes.push [17, "Left Arrow"]
codes.push [18, "Right Arrow"]
codes.push [19, "Up Arrow"]
codes.push [20, "Down Arrow"]
codes.push [21, "Unprintable char"]
codes.push [134, "(Inverse mode on)"]
codes.push [135, "(Inverse mode off)"]
codes.push [960, "Pi"]
codes.push [964, "Tau"]
codes.push [57856, "Button Cap Left"]
codes.push [57857, "Button Cap Right"]
codes.push [57872, "Figure Standing"]
codes.push [57873, "Figure Waiting"]
codes.push [57874, "Figure Tap Foot"]
codes.push [57875, "Figure Akimbo"]
codes.push [57888, "Tree"]
codes.push [8592, "Left Arrow"]
codes.push [8593, "Up Arrow"]
codes.push [8594, "Right Arrow"]
codes.push [8595, "Down Arrow"]
codes.push [9744, "Empty Box"]
codes.push [9745, "Checked Box"]
codes.push [9746, "Box with X"]
codes.push [9824, "Spade"]
codes.push [9827, "Club"]
codes.push [9829, "Heart"]
codes.push [9830, "Diamond"]
codes.push [9856, "Die Face 1"]
codes.push [9857, "Die Face 2"]
codes.push [9858, "Die Face 3"]
codes.push [9859, "Die Face 4"]
codes.push [9860, "Die Face 5"]
codes.push [9861, "Die Face 6"]

clear
row = 25
column = 0
printMore = function(s)
	text.row = row
	text.column = column
	print s
	globals.row = row - 1
	if 25 - row >= codes.len/2 then
		globals.row = 25
		globals.column = 36
	end if
end function

for pair in codes
	if pair[1][0] == "(" then c = " " else c = char(pair[0])
	printMore ("char(" + pair[0] + ")       ")[:12] + c + "  " + pair[1]
end for

print
print

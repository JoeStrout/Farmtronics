import "chars"

Task = {}
Task.key = ""      // short unique key
Task.name = ""     // display name
Task.details = []  // lines of text to print
Task.done = false

tasks = []

addTask = function(key, name, details)
	t = new Task
	t.key = key
	t.name = name
	t.details = details
	tasks.push t
end function

addTask "hello", "print ""Hello world!""", [
"Simply use the `print` command to",
"print the message ""Hello world!""",
"to the screen."]

addTask "cd", "cd to another directory", [
"Change to another directory by using",
"the `cd` command.  Remember to use",
"quotation marks around the path!",
"",
"Example: `cd ""/sys/demo"""]

addTask "demo", "run a demo", [
"`load` and then `run` any program you",
"find in the ""/sys/demo"" directory.",
"Example:",
"  `cd ""/sys/demo""`",
"  `dir`",
"  `load ""dogYears""",
"  `run`"]

addTask "edit", "edit a program", [
"Use the `edit` command to view and",
"edit the MiniScript code for any",
"program.",
"",
"(Load a program with `load`, or to",
"start a new program, use `reset`.)"]

addTask "save", "save a program", [
"Save the current program to your",
"/usr disk.  Example: `save ""foo""`",
"",
"(You can use `save` by itself with a",
"program previously loaded or saved",
"on the user disk.  But you can't",
"save anything to the /sys disk.)"]

addTask "for", "for-loop from 1 to 100", [
"Write a small program that prints the",
"numbers 1 through 100 using a `for` loop.",
"",
"For programming help, go to:",
"   `https://miniscript.org`"]

addTask "fizzbuzz", "implement FizzBuzz", [
"Write a program that prints the numbers",
"1 through 100, but prints ""Fizz"" instead",
"for any number divisible by 3, ""Buzz""",
"for any number divisible by 5, and for",
"numbers divisible by both 3 AND 5,",
"prints ""FizzBuzz""."]


selectedIdx = 0
for i in tasks.indexes
	if not tasks[i].done then
		selectedIdx = i
		break
	end if
end for

btn = function(c)
	return chars.inverseOn + chars.buttonCapLeft + c +
	  chars.buttonCapRight + chars.inverseOff
end function

btnUp = btn(chars.upArrow)
btnDown = btn(chars.downArrow)
btnEsc = btn("Esc")

draw = function()
	prevColor = text.color
	text.clear
	text.row = 19; text.column = 0
	text.color = color.silver
	print "To-Do List:"
	print
	for i in tasks.indexes
		t = tasks[i]
		if i == selectedIdx then
			text.color = color.yellow
		else
			text.color = color.silver
		end if
		if t.done then box = chars.checkedBox else box = chars.emptyBox
		print " " + box + " " + t.name
	end for
	print
	t = tasks[selectedIdx]
	text.color = color.yellow
	for line in t.details
		_printMark line, color.orange
	end for
	
	text.row = 0
	text.color = color.silver
	text.delimiter = ""
	_printOneLine btnUp + "/" + btnDown + 
	  ":view details; " + btnEsc + ": exit"
	text.delimiter = char(13)
	text.color = prevColor
end function

present = function()
	clear
	while true
		draw
		k = key.get
		if k == chars.up then
			outer.selectedIdx = (selectedIdx + tasks.len - 1) % tasks.len
		else if k == chars.down then
			outer.selectedIdx = (selectedIdx + 1) % tasks.len
		else if k == char(27) or k == char(10) then
			break
		end if
	end while
end function

if locals == globals then present

// Utilities related to paths, i.e., a series of locations in 2D space.
// Paths are represented as a list of [x,y] sub-lists.  Here we have
// methods for drawing such a path, creating such a path with the mouse,
// and moving a point along a path (both forwards and backwards).

import "mathUtil"

// drawPath
//	Draw a path, defined as a list of [x,y] points.  (This is similar
//	to PixelDisplay.drawPoly, but does not assume a closed figure.)
// Parameters:
//	gfx: PixelDisplay to draw to
//	path: list of [x,y] path points
//	color: optional color to draw in (defaults to gfx.color)
//	penSize: optional line thickness (defaults to 1)
drawPath = function(gfx, path, color, penSize)
	hw = penSize / 2
	fw = penSize - 1
	for i in range(0, path.len-2)
		gfx.line path[i][0], path[i][1], path[i+1][0], path[i+1][1], color, penSize
		if penSize > 2 then		
			gfx.fillEllipse path[i][0]-hw, path[i][1]-hw, fw, fw, color
		end if
	end for
	if penSize > 2 then
	gfx.fillEllipse path[-1][0]-hw, path[-1][1]-hw, fw, fw, color
end function

// nearestPointOnPath
//	Find the closest point on the path to the given point.
nearestPointOnPath = function(point, path)
	best = null
	bestDist = 0
	for i in range(0, path.len-2)
		pt = mathUtil.nearestPointOnLineSegment(path[i], path[i+1], point)
		dist = mathUtil.distance(pt, point)
		if best == null or dist < bestDist then
			best = pt
			bestDist = dist
		end if
	end for
	return best
end function

// distanceToPath
//	Return the distance from the given point to the closest
//	point on the given path.  (Useful for hit-testing.)
distanceToPath = function(point, path)
	best = null
	bestDist = 0
	for i in range(0, path.len-2)
		pt = mathUtil.nearestPointOnLineSegment(path[i], path[i+1], point)
		dist = mathUtil.distance(pt, point)
		if best == null or dist < bestDist then
			best = pt
			bestDist = dist
		end if
	end for
	return bestDist
end function

// makePathWithMouse
//	This is a little utility function to help you define a path, by clicking
//	a series of points on the screen with the mouse.
makePathWithMouse = function()
	import "textUtil"  // needed for printWrap
	clear
	text.row = 25
	while key.pressed("return"); end while
	print "Click points in the path.  Press Return when done."
	path = []
	btnDown = mouse.button
	while not key.pressed("return") and not key.pressed("enter")
		if mouse.button and not btnDown then
			path.push [mouse.x, mouse.y]
			if path.len == 1 then
				gfx.fillEllipse path[0][0]-2, path[0][1]-2, 4, 4
			else
				gfx.line path[-2][0], path[-2][1], path[-1][0], path[-1][1]
			end if
			text.row = 24
			textUtil.printWrap str(path)
		end if
		btnDown = mouse.button
		yield
	end while
	key.clear
	print
	print "OK, now you can copy that path above and paste into your code!"
end function
	

// PathPoint class
//	A PathPoint is a little object that represents a point along a path.
//	The path is assumed to not change while one of these is attached.
//	It allows you to efficiently move forward and backward along the path,
//	without having to start all over from the beginning on each step.
PathPoint = {}
PathPoint.path = null		// list of [x,y] points
PathPoint.position = [0,0]	// our current [x,y] position
PathPoint.curIndex = -1		// index of the point we are at or just past
PathPoint.distAtIndex = 0	// distance of that indexed point from the beginning of the path
PathPoint.distToNext = 0	// distance to next point (may be 0 if we're on the last point)
PathPoint.t = 0				// how far we are between this point and the next one

// PathPoint.make: a function to return a new PathPoint,
// positioned at a certain distance along the given path.
//	path: a list of xy points
//	initialDistance: how far along that path we begin
PathPoint.make = function(path, initialDistance=0)
	pp = new PathPoint
	pp.path = path
	distToI = 0
	for i in range(0, path.len-2)
		ptA = path[i]
		ptB = path[i+1]
		d = mathUtil.distance(ptA, ptB)
		if distToI + d > initialDistance then
			pp.curIndex = i
			pp.distAtIndex = distToI
			pp.distToNext = d
			pp.t = (initialDistance - distToI) / d
			pp.updatePosition
			return pp
		end if
		distToI = distToI + d
	end for
	pp.distToNext = 0
	pp.t = 0
	pp.updatePosition
	return pp
end function

// PathPoint.nearPoint: a function to return a new PathPoint,
// initialized to the closest point on the path to some other
// point.
PathPoint.nearPoint = function(path, nearestToPoint)
	pp = new PathPoint
	pp.path = path
	destDist = 0
	distToI = 0
	for i in range(0, path.len-2)
		ptA = path[i]
		ptB = path[i+1]
		d = mathUtil.distance(ptA, ptB)
		t = mathUtil.proportionAlongLine(ptA, ptB, nearestToPoint)
		if t <= 0 then
			p = ptA
		else if t >= 1 then
			p = ptB
		else
			p = mathUtil.lerp2d(ptA, ptB, t)
		end if
		dist = mathUtil.distance(p, nearestToPoint)
		if pp.curIndex < 0 or dist < bestDist then
			pp.curIndex = i
			pp.distAtIndex = distToI
			pp.distToNext = d
			pp.t = t
			bestDist = dist
		end if
		distToI = distToI + d
	end for
	pp.updatePosition
	return pp
end function

// PathPoint.distance: return our total distance from the start of the path.
PathPoint.distance = function()
	return self.distAtIndex + self.distToNext * t
end function

// PathPoint.isAtEnd: returns true when we have reached
//	the end of our path.
PathPoint.isAtEnd = function()
	return self.curIndex == self.path.len-1
end function

// PathPoint.updatePosition: recalculate our position [x,y] from
//	our other data (curIndex and t).
PathPoint.updatePosition = function()
	if self.curIndex < 0 then return [0,0]	// (happens on empty paths)
	self.position = self.path[self.curIndex]
	if self.distToNext > 0 and self.curIndex+1 < self.path.len then
		self.position = mathUtil.lerp2d(self.position, self.path[self.curIndex+1], self.t)
	end if
end function

// PathPoint.forwardAngle: get the angle, in degrees, that points forward along
//	the path at our current position.   This could be assigned to the .rotation
//	of a sprite to make it face forward as it travels along the path.
PathPoint.forwardAngle = function()
	if self.curIndex < 0 then return 0   // (happens on empty paths)
	i = self.curIndex
	if i == self.path.len - 1 then i = i - 1
	return atan(self.path[i+1][1] - self.path[i][1], 
	     self.path[i+1][0] - self.path[i][0]) * 180/pi
end function

// PathPoint.advance: move forward along the path by the given distance,
//	stopping if we reach the end.
PathPoint.advance = function(distance)
	if distance == 0 then return
	if distance < 0 then return self.retreat(-distance)
	maxIndex = self.path.len - 1
	if self.curIndex == maxIndex then return	// already at end
	while self.curIndex <= maxIndex and distance > 0
		newt = self.t + distance / self.distToNext
		if newt < 1 then
			// we're just advancing a little bit within the current segment
			self.t = newt
			self.updatePosition
			return
		end if
		// Advance to the next segment; if we hit the end, bail out
		self.curIndex = self.curIndex + 1
		self.distAtIndex = self.curIndex + self.distToNext
		distance = distance - self.distToNext * (1 - self.t)
		self.t = 0
		if self.curIndex == maxIndex then
			self.distToNext = 0
			self.updatePosition
			return
		end if
		self.distToNext = mathUtil.distance(
		self.path[self.curIndex], self.path[self.curIndex+1])
	end while
end function

PathPoint.retreat = function(distance)
	if distance == 0 then return
	if distance < 0 then return self.advance(-distance)
	while distance > 0
		newt = self.t - distance / self.distToNext
		if newt >= 0 then
			self.t = newt
			self.updatePosition
			return
		end if
		if self.curIndex == 0 then
			self.t = 0
			self.updatePosition
			return
		end if
		distance = distance - self.distToNext * self.t
		self.curIndex = self.curIndex - 1
		self.distToNext =  mathUtil.distance(
		self.path[self.curIndex], self.path[self.curIndex+1])
		self.distAtIndex = self.distAtIndex - self.distToNext
		self.t = 1
	end while	
end function

runUnitTests = function()
	print "Unit testing: pathUtil"
	import "listUtil"  // needed for list.apply
	
	errorCount = 0
	assertEqual = function(actual, expected, note)
		// for the sake of unit testing, let's round all numbers to 1 decimal place.
		if actual isa list then actual.apply1 @round, 1
		if expected isa list then expected.apply1 @round, 1
		if actual != expected then
			print "Unit test failure (" + note + "): expected " + expected + ", got " + actual
			outer.errorCount = errorCount + 1
		end if
	end function
	
	path = [ [30,10], [20,10], [10,10], [10,0], [0,0] ]
	pp = PathPoint.make(path, 13)
	assertEqual pp.position, [17,10], "PathPoint.make"
	pp = PathPoint.nearPoint(path, [17,50])
	assertEqual pp.position, [17,10], "PathPoint.nearPoint"
	assertEqual round(pp.forwardAngle), 180, "PathPoint.forwardAngle"
	
	pp.advance 5
	assertEqual pp.position, [12,10], "PathPoint.advance"
	assertEqual round(pp.forwardAngle), 180, "PathPoint.forwardAngle"
	
	pp.advance 5
	assertEqual pp.position, [10,7], "PathPoint.advance"
	assertEqual round(pp.forwardAngle), -90, "PathPoint.forwardAngle"
	
	pp.retreat -13
	assertEqual pp.position, [4,0], "PathPoint.retreat"
	assertEqual round(pp.forwardAngle), 180, "PathPoint.forwardAngle"
	assertEqual pp.isAtEnd, false, "PathPoint.isAtEnd"
	
	pp.advance 13
	assertEqual pp.position, [0,0], "PathPoint.advance"
	assertEqual round(pp.forwardAngle), 180, "PathPoint.forwardAngle"
	assertEqual pp.isAtEnd, true, "PathPoint.isAtEnd"
	
	pp.advance -23
	assertEqual pp.position, [13,10], "PathPoint.advance"
	assertEqual round(pp.forwardAngle), 180, "PathPoint.forwardAngle"
	assertEqual pp.isAtEnd, false, "PathPoint.isAtEnd"
	
	
	if errorCount == 0 then
		print "All tests passed.  Pop pop pop!"
	else
		print errorCount + " error" + "s" * (errorCount!=1) + " found."
	end if	
end function

if globals == locals then runUnitTests

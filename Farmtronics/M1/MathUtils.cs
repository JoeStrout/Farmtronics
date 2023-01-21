using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

public static class MathUtils {

	public const float Deg2Rad = 0.0174532925f;
	public const float Rad2Deg = 57.2957795131f;

	static System.Random rnd = new System.Random();

	public static float sqrMagnitude(this Vector2 v) {
		return v.X*v.X + v.Y*v.Y;
	}

	//public class Transform2D {
	//	public Vector2 center;
	//	public float rotation;	// rotation angle in RADIANS
	//	public Vector2 scale;
		
	//	public Transform2D(Vector2 center=default(Vector2), float rotation=0) {
	//		this.center = center;
	//		this.rotation = rotation;
	//		this.scale = Vector2.one;
	//	}
		
	//	public Transform2D(Vector2 center, float rotation, Vector2 scale) {
	//		this.center = center;
	//		this.rotation = rotation;
	//		this.scale = scale;
	//	}

	//	public Transform2D(Transform2D other) {
	//		this.center = other.center;
	//		this.rotation = other.rotation;
	//		this.scale = other.scale;
	//	}
		
	//	public override string ToString() {
	//		return string.Format("Transform2D({0}, {1}, {2})", center, rotation, scale);
	//	}

	//	public bool Matches(Transform2D other) {
	//		if (this == other) return true;
	//		return this.center == other.center
	//			&& this.rotation == other.rotation
	//			&& this.scale == other.scale;
	//	}

	//	public Matrix4x4 ToMatrix() {
	//		return Matrix4x4.TRS(center, Quaternion.Euler(0, 0, rotation * Mathf.Rad2Deg), scale);
	//	}
		
	//	public void TransformBounds(BoundingBox localBB, BoundingBox worldBB) {
	//		if (localBB.center == Vector2.zero) {
	//			worldBB.center = center;
	//		} else {
	//			worldBB.center = TransformPoint(localBB.center);
	//		}
	//		worldBB.halfSize.X = localBB.halfSize.X * Mathf.Abs(scale.X);
	//		worldBB.halfSize.Y = localBB.halfSize.Y * Mathf.Abs(scale.Y);
	//		worldBB.rotation = localBB.rotation + rotation;
	//		worldBB.dirty = true;
	//	}
		
	//	public Vector2 TransformPoint(Vector2 localPoint) {
	//		// convert from local to world coordinates:
	//		// scale, then rotate, then translate.
	//		float sinAng = Mathf.Sin(rotation);
	//		float cosAng = Mathf.Cos(rotation);
	//		Vector2 v = new Vector2(localPoint.X * scale.X, localPoint.Y * scale.Y);
	//		float worldX = center.X + v.X * cosAng - v.Y * sinAng;
	//		float worldY = center.Y + v.Y * cosAng + v.X * sinAng;
	//		return new Vector2(worldX, worldY);
	//	}
		
	//	public Vector2 InverseTransformPoint(Vector2 worldPoint) {
	//		// convert from world to local coordinates:
	//		// untranslate, then unrotate, then unscale
	//		Vector2 v = worldPoint - center;
			
	//		float sinAng = Mathf.Sin(-rotation);
	//		float cosAng = Mathf.Cos(-rotation);
	//		float newx = v.X * cosAng - v.Y * sinAng;
	//		v.Y = v.Y * cosAng + v.X * sinAng;
	//		v.X = newx;
			
	//		v.X /= scale.X;
	//		v.Y /= scale.Y;
			
	//		return v;
	//	}
	//}

	//public class BoundingBox {
	//	public Vector2 center;
	//	public Vector2 halfSize;
	//	public float rotation;	// rotation angle in RADIANS

	//	public int changeCount;	// change counter
	//	public bool dirty;		// true if corner, axis, and origin need recalculated
		
	//	Vector2[] corner;		// Corners of the box, where 0 is the lower left
	//	Vector2[] axis;			// two edges of the box extended away from corner[0]
	//	double[] origin;		// origin[a] = corner[0].dot(axis[a]);
		
	//	public BoundingBox(Vector3 center, Vector2 halfSize, float rotation=0) {
	//		this.center = center;
	//		this.halfSize = halfSize;
	//		this.rotation = rotation;
	//		dirty = true;
	//		//Debug.Log("Created new BoundingBox");
	//	}

	//	public override string ToString() {
	//		return string.Format("Bounds({0}, {1}, {2})", center, rotation, halfSize);
	//	}


	//	void Recompute() {
	//		if (corner == null) {
	//			corner = new Vector2[4];
	//			axis = new Vector2[2];
	//			origin = new double[2];
	//		}
	//		float cosAng = Mathf.Cos(rotation);
	//		float sinAng = Mathf.Sin(rotation);
	//		float vx = halfSize.X;
	//		float vy = halfSize.Y;
			
	//		corner[0] = new Vector2(center.X - vx * cosAng + vy * sinAng, center.Y - vy * cosAng - vx * sinAng);
	//		corner[1] = new Vector2(center.X - vx * cosAng - vy * sinAng, center.Y + vy * cosAng - vx * sinAng);
	//		corner[2] = new Vector2(center.X + vx * cosAng - vy * sinAng, center.Y + vy * cosAng + vx * sinAng);
	//		corner[3] = new Vector2(center.X + vx * cosAng + vy * sinAng, center.Y - vy * cosAng + vx * sinAng);
			
	//		axis[0] = corner[1] - corner[0]; 
	//		axis[1] = corner[3] - corner[0]; 

	//		// Make the length of each axis 1/edge length so we know any
	//		// dot product must be less than 1 to fall within the edge.
	//		for (int a = 0; a < 2; ++a) {
	//			axis[a] /= axis[a].sqrMagnitude;
	//			origin[a] = Vector2.Dot(corner[0], axis[a]);
	//		}
			
	//		dirty = false;
	//	}
		
	//	public bool Intersects(BoundingBox other) {
	//		if (rotation == 0 && other.rotation == 0) {
	//			// Axis-aligned bounding boxes: special case we can test very quickly.
	//			if (center.X + halfSize.X < other.center.X - other.halfSize.X) return false;
	//			if (center.X - halfSize.X > other.center.X + other.halfSize.X) return false;
	//			if (center.Y + halfSize.Y < other.center.Y - other.halfSize.Y) return false;
	//			if (center.Y - halfSize.Y > other.center.Y + other.halfSize.Y) return false;
	//			return true;
	//		}

	//		// Non-axis-aligned bounding boxes, we need to do Separation Axis Test.
	//		// Reference: http://flipcode.com/archives/2D_OBB_Intersection.shtml	
	//		// (although that implementation fails to check all 4 required axes)
	//		if (dirty) Recompute();
	//		if (other.dirty) other.Recompute();
			
	//		for (int a = 0; a < 2; ++a) {

	//			// Find the extent of box 2 on axis a
	//			double t = Vector2.Dot(other.corner[0], axis[a]);
	//			double tMin = t, tMax = t;
	//			for (int c = 1; c < 4; ++c) {		// (starting at 1 because we handled 0 above)
	//				t = Vector2.Dot(other.corner[c], axis[a]);
	//				if (t < tMin) tMin = t;
	//				else if (t > tMax) tMax = t;
	//			}

	//			// We have to subtract off the origin.  Then,
	//			// see if [tMin, tMax] intersects [0, 1]
	//			if ((tMin > 1 + origin[a]) || (tMax < origin[a])) {
	//				// There was no intersection along this dimension;
	//				// the boxes cannot possibly overlap.
	//				//Debug.Log("Separation found on axis " + a + ": tMin=" + tMin + ", tMax=" + tMax + ", origin[a]=" + origin[a]);
	//				return false;
	//			}
	//			//Debug.Log("No separation on axis " + a + ": tMin=" + tMin + ", tMax=" + tMax + ", origin[a]=" + origin[a]);
	//		}
			
	//		// No overlap yet; but if the boxes have different rotations, then we
	//		// still have two more axes to check from the other box.
	//		if (rotation != other.rotation) {
	//			for (int a = 0; a < 2; ++a) {

	//				// Find the extent of box 2 on axis a
	//				double t = Vector2.Dot(corner[0], other.axis[a]);
	//				double tMin = t, tMax = t;
	//				for (int c = 1; c < 4; ++c) {
	//					t = Vector2.Dot(corner[c], other.axis[a]);
	//					if (t < tMin) tMin = t;
	//					else if (t > tMax) tMax = t;
	//				}

	//				// We have to subtract off the origin.  Then,
	//				// see if [tMin, tMax] intersects [0, 1]
	//				if ((tMin > 1 + other.origin[a]) || (tMax < other.origin[a])) {
	//					// There was no intersection along this dimension;
	//					// the boxes cannot possibly overlap.
	//					//Debug.Log("Separation found on axis " + (a+2) + ": tMin=" + tMin + ", tMax=" + tMax + ", origin[a]=" + other.origin[a]);
	//					return false;
	//				}
	//				//Debug.Log("No separation on axis " + (a+2) + ": tMin=" + tMin + ", tMax=" + tMax + ", origin[a]=" + other.origin[a]);
	//			}				
	//		}

	//		// There was no dimension along which there is no intersection.
	//		// Therefore the boxes overlap.
	//		return true;
	//	}
		
	//	public bool Contains(Vector2 point) {
	//		// Start by converting the point into box coordinates.
	//		point -= center;
	//		if (rotation != 0) {
	//			float sinAng = Mathf.Sin(-rotation);
	//			float cosAng = Mathf.Cos(-rotation);
	//			point = new Vector2(point.X * cosAng - point.Y * sinAng, point.Y * cosAng + point.X * sinAng);
	//		}
	//		// Then do a simple bounds check.
	//		return (point.X > -halfSize.X && point.X < halfSize.X && point.Y > -halfSize.Y && point.Y < halfSize.Y);
	//	}
		
	//	public Vector2[] Corners() {
	//		if (dirty) Recompute();
	//		return corner;
	//	}
	//}

	// Return a normally-distributed random number (mean 0, standard deviation 1).
	// Reference: https://stackoverflow.com/questions/5817490
	public static float RandomGaussian() {
		double u, v, S;

		do {
			u = 2.0 * rnd.NextDouble() - 1.0;
			v = 2.0 * rnd.NextDouble() - 1.0;
			S = u * u + v * v;
		} while (S >= 1.0);

		double fac = System.Math.Sqrt(-2.0 * System.Math.Log(S) / S);
		return (float)(u * fac);
	}
	
	// Project a vector onto a plane. (The output is not normalized.)
	//public static Vector3 ProjectVectorOnPlane(Vector3 planeNormal, Vector3 vector) {
	//	planeNormal.Normalize();
	//	return vector - (Vector3.Dot(vector, planeNormal) * planeNormal);
	//}

	//// Rotate a 2D vector by the given angle.
	//public static Vector2 Vector2Rotate(Vector2 v, float degrees) {
	//	float radians = degrees * Mathf.Deg2Rad;
	//	float sinAng = Mathf.Sin(radians);
	//	float cosAng = Mathf.Cos(radians);
	//	return new Vector2(v.X * cosAng - v.Y * sinAng, v.Y * cosAng + v.X * sinAng);
	//}
	
	//// Rotate a 3D vector about an axis.
	//public static Vector3 Vector3Rotate(Vector3 v, Vector3 axis, float degrees) {
	//	// https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
	//	axis.Normalize();
	//	float theta = degrees * Mathf.Deg2Rad;
	//	float cosTheta = Mathf.Cos(theta);
	//	Vector3 vrot = v * cosTheta 
	//		+ Vector3.Cross(axis, v) * Mathf.Sin(theta)
	//		+ axis * Vector3.Dot(axis, v) * (1 - cosTheta);
	//	return vrot;
	//}

	/// <summary>
	/// Sample a Gaussian curve that goes from 1 at distance=0
	/// to 0 at distance >= maxRadius.  Useful in painting blobs.
	/// </summary>
	/// <returns>A value from 0 to 1.</returns>
	/// <param name="distance">distance from the center of the curve/blob</param>
	/// <param name="maxRadius">radius at which the output goes to 0</param>
	public static float GaussFalloff(float distance, float maxRadius) {
		if (distance >= maxRadius) return 0f;
		if (distance <= 0) return 1f;
		float result = (float)Math.Pow(360f, -Math.Pow(distance / maxRadius, 2.5f) - 0.01f);
		if (result < 0) result = 0;
		if (result > 1) result = 1;
		return result;
	}
	
	/// <summary>
	/// Return the value of t such that endA + t * (endB-endA) is the closest
	/// point on the line to point p.  Fails if endA == endB.
	/// </summary>
	/// <param name="endA">One end of the line.</param>
	/// <param name="endB">Other end of the line.</param>
	/// <param name="p">Point of interest.</param>
	/// <returns>t value (which will be in range 0-1 if p is between endA and endB)</returns>
	public static float ProportionAlongLine(Vector2 endA, Vector2 endB, Vector2 p) {
		Vector2 a_p = p - endA;
		Vector2 a_b = endB - endA;
		float segLenSqr = a_b.sqrMagnitude();
		return Vector2.Dot(a_p, a_b) / segLenSqr;
	}
	
	/// <summary>
	/// Returns the nearest point on line (endA, endB) to point p.
	/// </summary>
	public static Vector2 NearestPointOnLine(Vector2 endA, Vector2 endB, Vector2 p) {
		Vector2 a_p = p - endA;
		Vector2 a_b = endB - endA;
		float segLenSqr = a_b.sqrMagnitude();
		if (segLenSqr < 1E-6) return endA;
		float t = Vector2.Dot(a_p, a_b) / segLenSqr;
		return endA + a_b * t;		
	}
	
	/// <summary>
	/// Returns the nearest point on line segment (endA, endB) to point p.
	/// </summary>
	public static Vector2 NearestPointOnLineSegment(Vector2 endA, Vector2 endB, Vector2 p) {
		Vector2 a_p = p - endA;
		Vector2 a_b = endB - endA;
		float segLenSqr = a_b.sqrMagnitude();
		if (segLenSqr < 1E-6) return endA;
		float t = Vector2.Dot(a_p, a_b) / segLenSqr;
		if (t < 0) t = 0; if (t > 1) t = 1;
		return endA + a_b * t;		
	}
	
	/// <summary>
	/// Find the proportion (u) along each line segment where two lines
	/// intersect.  If both uA and uB are in [0,1] then the intersection
	/// is within the range of both line segments.  If either is outside
	/// this range, then the intersection is outside one or both segments.
	/// The actual intersection point may be found as a0 + uA * (a1-a0).
	/// Reference: http://paulbourke.net/geometry/pointlineplane/
	/// </summary>
	/// <param name="a0">Line A endpoint 0.</param>
	/// <param name="a1">Line A endpoint 1.</param>
	/// <param name="b0">Line B endpoint 0.</param>
	/// <param name="b1">Line B endpoint 1.</param>
	/// <param name="uA">Intersection proportion from a0 to a1.</param>
	/// <param name="uB">Intersectino proportion from b0 to b1.</param>
	/// <returns>true if intersection found; false if lines are parallel</returns>
	public static bool IntersectionProportions(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, out float uA, out float uB) {
		// Denominator for ua and ub are the same, so store this calculation.
		// If denominator is 0, then lines are parallel.
		float d = (b1.Y - b0.Y) * (a1.X - a0.X) - (b1.X - b0.X) * (a1.Y - a0.Y);
		if (d == 0f) {
			uA = uB = 0;
			return false;
		}
		
		// Numerators n_a and n_b are calculated as follows...
		float n_a = (b1.X - b0.X) * (a0.Y - b0.Y) - (b1.Y - b0.Y) * (a0.X - b0.X);
		float n_b = (a1.X - a0.X) * (a0.Y - b0.Y) - (a1.Y - a0.Y) * (a0.X - b0.X);
		
		// Calculate the intermediate fractional point at which the lines intersect.
		uA = n_a / d;
		uB = n_b / d;
		return true;		
	}

	public static float LineSegIntersectFraction(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
		// Look for an intersection between line p1-p2 and line p3-p4.
		// Return the fraction of the way from p1 to p2 where this
		// intersection occurs.  If the two lines are parallel, and
		// there is no intersection, then this returns float.NaN.
		// Reference: http://paulbourke.net/geometry/lineline2d/
		float num = (p4.X - p3.X) * (p1.Y - p3.Y) - (p4.Y - p3.Y) * (p1.X - p3.X);
		float denom=(p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);
		if (denom == 0f) return float.NaN;
		return num / denom;
	}
	
	public static bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
		// Return whether the line segment p1-p2 intersects segment p3-p4.
		float ua = LineSegIntersectFraction(p1, p2, p3, p4);
		if (float.IsNaN(ua)) return false;  // the line segments are parallel
		if (ua < 0.0f || ua > 1.0f) return false;  // intersection out of bounds
		float ub = LineSegIntersectFraction(p3, p4, p1, p2);
		if (ub < 0.0f || ub > 1.0f) return false;  // intersection out of bounds
		return true;
	}

	public static bool LineLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 result) {
		// Find the intersection of line p1-p2 and line p3-p4.
		float ua = LineSegIntersectFraction(p1, p2, p3, p4);
		if (float.IsNaN(ua)) {  // the line segments are parallel
			result = new Vector2(0,0);
			return false;
		}
		result = p1 + (p2-p1) * ua;
		return true;
	}

	// Structure of precomputed data used for point-in-polygon tests.
	public struct PointInPolyPrecalc {
		public List<Vector2> polygon;
		public List<float> constants;
		public List<float> multiples;
	}

	public static PointInPolyPrecalc PrecalcPointInPoly(List<Vector2> polygon) {
		var result = new PointInPolyPrecalc();
		result.polygon = polygon;
		result.constants = new List<float>(polygon.Count);
		result.multiples = new List<float>(polygon.Count);

		int j = polygon.Count - 1;
		for (int i=0; i<polygon.Count; i++) {
			if (polygon[j].Y == polygon[i].Y) {
				result.constants.Add(polygon[i].X);
				result.multiples.Add(0);
			} else {
				result.constants.Add(polygon[i].X-(polygon[i].Y*polygon[j].X)/(polygon[j].Y-polygon[i].Y)+(polygon[i].Y*polygon[i].X)/(polygon[j].Y-polygon[i].Y));
				result.multiples.Add((polygon[j].X-polygon[i].X)/(polygon[j].Y-polygon[i].Y)); 
			}
			j=i;
		}
		
		return result;
	}

	public static bool PointInPoly(PointInPolyPrecalc precalc, Vector2 pt) {
		int polyCorners = precalc.polygon.Count;
		bool oddNodes = false;
		bool current = precalc.polygon[polyCorners-1].Y > pt.Y;
		for (int i=0; i < polyCorners; i++) {
			bool previous = current;
			current = precalc.polygon[i].Y > pt.Y; 
			if (current != previous) oddNodes ^= pt.Y * precalc.multiples[i] + precalc.constants[i] < pt.X; 
		}
		return oddNodes;
	}

	public static bool PointInPoly(Vector2 pt, List<Vector2> polygon) {
		var precalc = PrecalcPointInPoly(polygon);
		return PointInPoly(precalc, pt);
	}

	public static bool AnyPointInPoly(List<Vector2> pointsToCheck, List<Vector2> polygon) {
		var precalc = PrecalcPointInPoly(polygon);
		for (int i=0; i<pointsToCheck.Count; i++) {
			if (PointInPoly(precalc, pointsToCheck[i])) return true;
		}
		return false;
	}
	
	public static float PolyArea(List<Vector2> polygon) {
		// Note: works correctly only for polygons with no self-intersections.
		float area = 0;
		int lasti = polygon.Count - 1;
		for (int i=0; i<polygon.Count; i++) {
			area += (polygon[lasti].X + polygon[i].X) * (polygon[lasti].Y - polygon[i].Y);
			lasti = i;
		}
		if (area < 0) area = -area;
		return area * 0.5f;
	}
	
	public static List<Vector2> InsetPolygon(List<Vector2> polyPoints, float amount) {
		int count = polyPoints.Count;
		var result = new List<Vector2>(count);
		Vector2 lastPt = polyPoints[count-1];
		for (int i=0; i<count; i++) {
			int nexti = (i+1) % count;
			result.Add(InsetCorner(lastPt, polyPoints[i], polyPoints[nexti], amount));
			lastPt = polyPoints[i];
		}
		return result;
	}
	
	public static Vector2 InsetCorner(Vector2 p0, Vector2 p1, Vector2 p2, float insetDist) {
		// find the offset left and right lines
		float leftDist = Vector2.Distance(p1, p0);
		if (leftDist < 1E-6f) return p1;
		Vector2 leftOffset = new Vector2(p1.Y-p0.Y, p0.X-p1.X);
		leftOffset = leftOffset * insetDist / leftDist;

		float rightDist = Vector2.Distance(p2, p1);
		if (rightDist < 1E-6f) return p1;
		Vector2 rightOffset = new Vector2(p2.Y-p1.Y, p1.X-p2.X);
		rightOffset = rightOffset * insetDist / rightDist;
		
		// return the intersection point of the two inset segments
		Vector2 result;
		if (LineLineIntersection(p0+leftOffset, p1+leftOffset, p1+rightOffset, p2+rightOffset, out result)) {
			return result;
		} else {
			return p1;
		}
	}
	
}

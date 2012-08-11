using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AW2.Helpers.Geometric
{
    /// <summary>
    /// Contains helper methods for geometric problems.
    /// </summary>
    public class Geometry
    {
        #region Intersection methods

        /// <summary>
        /// Returns true iff the point is contained in the geometric primitive.
        /// </summary>
        public static bool Intersect(Vector2 point, IGeomPrimitive prim)
        {
            if (prim is Circle) return Intersect(point, (Circle)prim);
            if (prim is Rectangle) return Intersect(point, (Rectangle)prim);
            if (prim is Triangle) return Intersect(point, (Triangle)prim);
            if (prim is Polygon) return Intersect(point, (Polygon)prim);
            throw new NotImplementedException("Geometry.Intersect not implemented for Vector2 and " + prim.GetType().Name);
        }

        /// <summary>
        /// Returns true iff the point is in the solid circle.
        /// </summary>
        public static bool Intersect(Vector2 point, Circle circle)
        {
            return Vector2.DistanceSquared(point, circle.Center) <= circle.Radius * circle.Radius;
        }

        /// <summary>
        /// Returns if a point and a solid rectangle intersect.
        /// </summary>
        public static bool Intersect(Vector2 point, Rectangle rectangle)
        {
            return
                rectangle.Min.X <= point.X && point.X <= rectangle.Max.X &&
                rectangle.Min.Y <= point.Y && point.Y <= rectangle.Max.Y;
        }

        /// <summary>
        /// Returns if a point lies inside a triangle.
        /// </summary>
        public static bool Intersect(Vector2 point, Triangle triangle)
        {
            // Adapted from C code by Eric Haines, from http://www.graphicsgems.org/.

            // Shoot a test ray along +X axis.  The strategy is to compare vertex Y values
            // to the testing point's Y and quickly discard edges which are entirely to one
            // side of the test ray.
            Vector2 p1 = triangle.P1;
            Vector2 p2 = triangle.P2;
            Vector2 p3 = triangle.P3;
            bool yflag1 = p1.Y >= point.Y;
            bool yflag2 = p2.Y >= point.Y;
            bool yflag3 = p3.Y >= point.Y;
            bool inside_flag = false;
            if (yflag1 != yflag2 &&
                yflag2 == ((p2.Y - point.Y) * (p1.X - p2.X) >= (p2.X - point.X) * (p1.Y - p2.Y)))
                inside_flag = !inside_flag;
            if (yflag2 != yflag3 &&
                yflag3 == ((p3.Y - point.Y) * (p2.X - p3.X) >= (p3.X - point.X) * (p2.Y - p3.Y)))
                inside_flag = !inside_flag;
            if (yflag3 != yflag1 &&
                yflag1 == ((p1.Y - point.Y) * (p3.X - p1.X) >= (p1.X - point.X) * (p3.Y - p1.Y)))
                inside_flag = !inside_flag;
            return inside_flag;
        }

        /// <summary>
        /// Returns true iff the two line segments ab and cd intersect.
        /// </summary>
        /// <param name="a">One end of the first line segment.</param>
        /// <param name="b">The other end of the first line segment.</param>
        /// <param name="c">One end of the second line segment.</param>
        /// <param name="d">The other end of the second line segment.</param>
        /// <param name="intersection">Where to store the point of intersection.</param>
        /// <returns>True iff the line segments intersect.</returns>
        /// The point of intersection will be stored to 'intersection' only if the segments
        /// intersect at a single point. If the segments don't intersect, 'intersect' is unmodified.
        /// If the segments intersect at a whole line segment, 'intersection' will be set to null.
        public static bool Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d, ref Vector2? intersection)
        {
            // This algorithm is adapted from C code by Franklin Antonio 
            // at http://www.graphicsgems.org
            float x1lo, x1hi, y1lo, y1hi;
            float Ax = b.X - a.X;
            float Bx = c.X - d.X;

            // X bound box test
            if (Ax < 0)
            {
                x1lo = b.X;
                x1hi = a.X;
            }
            else
            {
                x1hi = b.X;
                x1lo = a.X;
            }
            if (Bx > 0)
            {
                if (x1hi < d.X || c.X < x1lo) return false;
            }
            else
            {
                if (x1hi < c.X || d.X < x1lo) return false;
            }

            float Ay = b.Y - a.Y;
            float By = c.Y - d.Y;

            // Y bound box test
            if (Ay < 0)
            {
                y1lo = b.Y;
                y1hi = a.Y;
            }
            else
            {
                y1hi = b.Y;
                y1lo = a.Y;
            }
            if (By > 0)
            {
                if (y1hi < d.Y || c.Y < y1lo) return false;
            }
            else
            {
                if (y1hi < c.Y || d.Y < y1lo) return false;
            }

            float Cx = a.X - c.X;
            float Cy = a.Y - c.Y;
            float val1 = By * Cx - Bx * Cy;
            float val3 = Ay * Bx - Ax * By;
            if (val3 > 0)
            {
                if (val1 < 0 || val1 > val3) return false;
            }
            else
            {
                if (val1 > 0 || val1 < val3) return false;
            }
            float val2 = Ax * Cy - Ay * Cx;
            if (val3 > 0)
            {
                if (val2 < 0 || val2 > val3) return false;
            }
            else
            {
                if (val2 > 0 || val2 < val3) return false;
            }

            if (val3 == 0)
            {
                // both segments on the same line
                intersection = null;
                return true;
            }

            Vector2 inters = new Vector2();
            float num = val1 * Ax;	               // numerator
            inters.X = a.X + num / val3;           // intersection x

            num = val1 * Ay;
            inters.Y = a.Y + num / val3;           // intersection y
            intersection = new Vector2?(inters);
            return true; // intersecting segments
        }

        /// <summary>
        /// Returns true iff the point is inside the polygon.
        /// </summary>
        public static bool Intersect(Vector2 point, Polygon polygon)
        {
            // Adapted from C code by Eric Haines, from http://www.graphicsgems.org/.

            // This version is usually somewhat faster than the original published in
            // Graphics Gems IV; by turning the division for testing the X axis crossing
            // into a tricky multiplication test this part of the test became faster,
            // which had the additional effect of making the test for "both to left or
            // both to right" a bit slower for triangles than simply computing the
            // intersection each time.  The main increase is in triangle testing speed,
            // which was about 15% faster; all other polygon complexities were pretty much
            // the same as before.  On machines where division is very expensive (not the
            // case on the HP 9000 series on which I tested) this test should be much
            // faster overall than the old code.  Your mileage may (in fact, will) vary,
            // depending on the machine and the test data, but in general I believe this
            // code is both shorter and faster.  This test was inspired by unpublished
            // Graphics Gems submitted by Joseph Samosky and Mark Haigh-Hutchinson.
            // Related work by Samosky is in:
            //
            // Samosky, Joseph, "SectionView: A system for interactively specifying and
            // visualizing sections through three-dimensional medical image data",
            // M.S. Thesis, Department of Electrical Engineering and Computer Science,
            // Massachusetts Institute of Technology, 1993.

            // Shoot a test ray along +X axis.  The strategy is to compare vertex Y values
            // to the testing point's Y and quickly discard edges which are entirely to one
            // side of the test ray. 
            Vector2 vPrev = polygon.Vertices[polygon.Vertices.Length - 1];

            // Get test bit for above/below X axis.
            bool yflag0 = vPrev.Y >= point.Y;

            bool inside_flag = false;
            foreach (Vector2 v in polygon.Vertices)
            {
                bool yflag1 = v.Y >= point.Y;

                // Check if endpoints straddle (are on opposite sides) of X axis
                // (i.e. the Y's differ); if so, +X ray could intersect this edge.
                // The old test also checked whether the endpoints are both to the
                // right or to the left of the test point.  However, given the faster
                // intersection point computation used below, this test was found to
                // be a break-even proposition for most polygons and a loser for
                // triangles (where 50% or more of the edges which survive this test
                // will cross quadrants and so have to have the X intersection computed
                // anyway).  I credit Joseph Samosky with inspiring me to try dropping
                // the "both left or both right" part of my code.
                if (yflag0 != yflag1)
                {
                    /* Check intersection of pgon segment with +X ray.
                     * Note if >= point's X; if so, the ray hits it.
                     * The division operation is avoided for the ">=" test by checking
                     * the sign of the first vertex wrto the test point; idea inspired
                     * by Joseph Samosky's and Mark Haigh-Hutchinson's different
                     * polygon inclusion tests.
                     */
                    if (((v.Y - point.Y) * (vPrev.X - v.X) >=
                        (v.X - point.X) * (vPrev.Y - v.Y)) == yflag1)
                    {
                        inside_flag = !inside_flag;
                    }
                }

                /* Move to the next pair of vertices, retaining info as possible. */
                yflag0 = yflag1;
                vPrev = v;
            }
            return inside_flag;
        }

        /// <summary>
        /// Crops a line segment to a rectangle, returning the new end point.
        /// The start point must be strictly inside the rectangle.
        /// </summary>
        public static Vector2 CropLineSegment(Vector2 start, Vector2 end, Vector2 min, Vector2 max)
        {
            if (!IsPointInsideRectangle(start, min, max)) throw new ArgumentException("Start must be inside the rectangle");
            var p11 = min;
            var p21 = new Vector2(max.X, min.Y);
            var p22 = max;
            var p12 = new Vector2(min.X, max.Y);
            Vector2? intersection = null;
            if (Intersect(p11, p21, start, end, ref intersection) && intersection.HasValue) return intersection.Value;
            if (Intersect(p21, p22, start, end, ref intersection) && intersection.HasValue) return intersection.Value;
            if (Intersect(p22, p12, start, end, ref intersection) && intersection.HasValue) return intersection.Value;
            if (Intersect(p12, p11, start, end, ref intersection) && intersection.HasValue) return intersection.Value;
            return end;
        }

        #endregion Intersection methods

        #region Location and distance query methods

        /// <summary>
        /// Returns true if <paramref name="p"/> is strictly inside the axis-aligned rectangle
        /// defined by <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        public static bool IsPointInsideRectangle(Vector2 p, Vector2 min, Vector2 max)
        {
            if (min.X > max.X || min.Y > max.Y) throw new ArgumentException("Min must not be greater than max");
            return p.X > min.X && p.X < max.X && p.Y > min.Y && p.Y < max.Y;
        }

        /// <summary>
        /// Returns the length of the shortest line segment that connects
        /// the geometric primitive to the point.
        /// </summary>
        public static float Distance(Vector2 point, IGeomPrimitive prim)
        {
            if (prim is Circle) return Distance(point, (Circle)prim);
            if (prim is Rectangle) return Distance(point, (Rectangle)prim);
            if (prim is Triangle) return Distance(point, (Triangle)prim);
            if (prim is Polygon) return Distance(point, (Polygon)prim);
            throw new NotImplementedException("Geometry.Distance() not implemented for Vector2 and " + prim.GetType().Name);
        }

        /// <summary>
        /// Returns the squared distance between 'point' and the line segment ab.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <returns>The squared distance between 'point' and the line segment ab.</returns>
        public static float DistanceSquared(Vector2 point, Vector2 a, Vector2 b)
        {
            // There's three cases to consider, depending on the point's projection
            // on the line that a and b define.
            Vector2 ab = b - a;
            Vector2 ap = point - a;
            Vector2 bp = point - b;
            float dot1 = Vector2.Dot(ab, ap);
            float dot2 = Vector2.Dot(ab, bp);

            // Case 1: Point's projection is outside the line segment, closer to a than b.
            if (dot1 <= 0)
                return ap.LengthSquared();

            // Case 2: Point's projection is outside the line segment, closer to b than a.
            if (dot2 >= 0)
                return bp.LengthSquared();

            // Case 3: Point's projection is on the line segment.
            return ap.LengthSquared() - dot1 * dot1 / ab.LengthSquared();
        }

        /// <summary>
        /// Returns a point on the line segment that is maximally close to the given point.
        /// </summary>
        /// There may not be a unique closest point. In such a case the exact return
        /// value is undefined but will still meet the definition of the return value.
        /// If the given point is on the line segment, the same point is returned.
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <param name="point">The point.</param>
        /// <returns>A point on the line segment that is maximally close to the given point.</returns>
        public static Vector2 GetClosestPoint(Vector2 a, Vector2 b, Vector2 point)
        {
            float distance;
            return GetClosestPoint(a, b, point, out distance);
        }

        /// <summary>
        /// Returns a point on the line segment that is maximally close to the given point.
        /// Also computes the distance from the given point to the returned point.
        /// </summary>
        /// There may not be a unique closest point. In such a case the exact return
        /// value is undefined but will still meet the definition of the return value.
        /// If the given point is on the line segment, the same point is returned.
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <param name="point">The point.</param>
        /// <param name="distance">Where to store the distance between the given point and the returned point.</param>
        /// <returns>A point on the line segment that is maximally close to the given point.</returns>
        public static Vector2 GetClosestPoint(Vector2 a, Vector2 b, Vector2 point, out float distance)
        {
            // There's three cases to consider, depending on the point's projection
            // on the line that a and b define.
            Vector2 ab = b - a;
            Vector2 ap = point - a;
            Vector2 bp = point - b;
            float dot1 = Vector2.Dot(ab, ap);
            float dot2 = Vector2.Dot(ab, bp);
            Vector2 closestPoint;

            // Case 1: Point's projection is outside the line segment, closer to a than b.
            if (dot1 <= 0)
                closestPoint = a;

            // Case 2: Point's projection is outside the line segment, closer to b than a.
            else if (dot2 >= 0)
                closestPoint = b;

            // Case 3: Point's projection is on the line segment.
            else
            {
                Vector2 apProjectedToAb = ab * Vector2.Dot(ap, ab) / ab.LengthSquared();
                closestPoint = a + apProjectedToAb;
            }

            distance = (point - closestPoint).Length();
            return closestPoint;
        }

        /// <summary>
        /// Returns the distance from a point to a circle.
        /// If the point is inside the circle, the distance is zero.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="circle">The circle.</param>
        /// <returns>The distance from the point to the circle.</returns>
        public static float Distance(Vector2 point, Circle circle)
        {
            return Math.Max(0, Vector2.Distance(point, circle.Center) - circle.Radius);
        }

        /// <summary>
        /// Returns the distance from a point to a triangle.
        /// If the point is inside the triangle, the distance is zero.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="triangle">The triangle.</param>
        /// <returns>The distance from the point to the triangle.</returns>
        public static float Distance(Vector2 point, Triangle triangle)
        {
            // First test corners for being the closest points on the triangle.
            // This should cover the most probable cases, assuming the triangle
            // is small relative to the distance 'p' is from 't' on average
            // over several calls to this method.
            // The second most probable case is that the closest point lies
            // on an edge of 't'.
            // The least probable case is that 'p' lies inside 't'.

            Vector2 p1 = triangle.P1;
            Vector2 p2 = triangle.P2;
            Vector2 p3 = triangle.P3;

            // Is 't.P1' the closest point?
            Vector2 p1p = point - p1;
            Vector2 e12 = p2 - p1;
            Vector2 e13 = p3 - p1;
            float halfplane12 = Vector2.Dot(p1p, e12);
            float halfplane13 = Vector2.Dot(p1p, e13);
            if (halfplane12 <= 0 && halfplane13 <= 0)
                return Vector2.Distance(point, p1);

            // Is 't.P2' the closest point?
            Vector2 p2p = point - p2;
            Vector2 e23 = p3 - p2;
            float halfplane21 = -Vector2.Dot(p2p, e12);
            float halfplane23 = Vector2.Dot(p2p, e23);
            if (halfplane21 <= 0 && halfplane23 <= 0)
                return Vector2.Distance(point, p2);

            // Is 't.P3' the closest point?
            Vector2 p3p = point - p3;
            float halfplane31 = -Vector2.Dot(p3p, e13);
            float halfplane32 = -Vector2.Dot(p3p, e23);
            if (halfplane31 <= 0 && halfplane32 <= 0)
                return Vector2.Distance(point, p3);

            // Is the closest point on the edge between 't.P2' and 't.P3'?
            float distance23 = Vector2.Dot(triangle.Normal23, p2p);
            if (distance23 >= 0 && halfplane23 >= 0 && halfplane32 >= 0)
                return distance23;

            // Is the closest point on the edge between 't.P1' and 't.P3'?
            float distance13 = Vector2.Dot(triangle.Normal13, p1p);
            if (distance13 >= 0 && halfplane13 >= 0 && halfplane31 >= 0)
                return distance13;

            // Is the closest point on the edge between 't.P1' and 't.P2'?
            float distance12 = Vector2.Dot(triangle.Normal12, p1p);
            if (distance12 >= 0 && halfplane12 >= 0 && halfplane21 >= 0)
                return distance12;

            // Otherwise 'p' lies inside 't'.
            return 0;
        }

        /// <summary>
        /// Returns the squared distance from a point to a triangle.
        /// If the point is inside the triangle, the distance is zero.
        /// </summary>
        public static float DistanceSquared(Vector2 point, Triangle triangle)
        {
            // First, test corners for being the closest points on the triangle.
            // This should cover the most probable cases, assuming the triangle
            // is small relative to the distance 'p' is from 't' on average
            // over several calls to this method.
            // The second most probable case is that the closest point lies
            // on an edge of 't'.
            // The least probable case is that 'p' lies inside 't'.

            Vector2 p1 = triangle.P1;
            Vector2 p2 = triangle.P2;
            Vector2 p3 = triangle.P3;

            // Is 'p1' the closest point?
            Vector2 p1p = point - p1;
            Vector2 e12 = p2 - p1;
            Vector2 e13 = p3 - p1;
            float halfplane12 = Vector2.Dot(p1p, e12);
            float halfplane13 = Vector2.Dot(p1p, e13);
            if (halfplane12 <= 0 && halfplane13 <= 0)
                return Vector2.DistanceSquared(point, p1);

            // Is 'p2' the closest point?
            Vector2 p2p = point - p2;
            Vector2 e23 = p3 - p2;
            float halfplane21 = -Vector2.Dot(p2p, e12);
            float halfplane23 = Vector2.Dot(p2p, e23);
            if (halfplane21 <= 0 && halfplane23 <= 0)
                return Vector2.DistanceSquared(point, p2);

            // Is 'p3' the closest point?
            Vector2 p3p = point - p3;
            float halfplane31 = -Vector2.Dot(p3p, e13);
            float halfplane32 = -Vector2.Dot(p3p, e23);
            if (halfplane31 <= 0 && halfplane32 <= 0)
                return Vector2.DistanceSquared(point, p3);

            // Is the closest point on the edge between 'p2' and 'p3'?
            float distance23 = Vector2.Dot(triangle.Normal23, p2p);
            if (distance23 >= 0 && halfplane23 >= 0 && halfplane32 >= 0)
                return distance23 * distance23;

            // Is the closest point on the edge between 'p1' and 'p3'?
            float distance13 = Vector2.Dot(triangle.Normal13, p1p);
            if (distance13 >= 0 && halfplane13 >= 0 && halfplane31 >= 0)
                return distance13 * distance13;

            // Is the closest point on the edge between 'p1' and 'p2'?
            float distance12 = Vector2.Dot(triangle.Normal12, p1p);
            if (distance12 >= 0 && halfplane12 >= 0 && halfplane21 >= 0)
                return distance12 * distance12;

            // Otherwise 'p' lies inside 't'.
            return 0;
        }

        /// <summary>
        /// Returns the distance between a point and a rectangle.
        /// </summary>
        /// The distance is the least distance between the point and any
        /// point that lies in the rectangle. In particular, if the point itself
        /// lies inside the rectangle, zero will be returned.
        public static float Distance(Vector2 point, Rectangle rectangle)
        {
            bool left = point.X < rectangle.Min.X;
            bool right = rectangle.Max.X < point.X;
            bool under = point.Y < rectangle.Min.Y;
            bool over = rectangle.Max.Y < point.Y;

            // Is the shortest distance measured from a face?
            if (!left && !right)
            {
                if (under)
                    return rectangle.Min.Y - point.Y;
                if (over)
                    return point.Y - rectangle.Max.Y;
                return 0;
            }
            if (!under && !over)
            {
                if (left)
                    return rectangle.Min.X - point.X;
                if (right)
                    return point.X - rectangle.Max.X;
                return 0;
            }

            // Shortest distance is measured from a corner.
            if (left)
            {
                if (under)
                    return (float)Math.Sqrt(
                        (rectangle.Min.X - point.X) * (rectangle.Min.X - point.X) +
                        (rectangle.Min.Y - point.Y) * (rectangle.Min.Y - point.Y));
                else // over
                    return (float)Math.Sqrt(
                        (rectangle.Min.X - point.X) * (rectangle.Min.X - point.X) +
                        (point.Y - rectangle.Max.Y) * (point.Y - rectangle.Max.Y));
            }
            else // right
            {
                if (under)
                    return (float)Math.Sqrt(
                        (point.X - rectangle.Max.X) * (point.X - rectangle.Max.X) +
                        (rectangle.Min.Y - point.Y) * (rectangle.Min.Y - point.Y));
                else // over
                    return (float)Math.Sqrt(
                        (point.X - rectangle.Max.X) * (point.X - rectangle.Max.X) +
                        (point.Y - rectangle.Max.Y) * (point.Y - rectangle.Max.Y));
            }
        }

        /// <summary>
        /// Returns the squared distance between a point and a rectangle.
        /// </summary>
        /// The distance is the least distance between the point and any
        /// point that lies in the rectangle. In particular, if the point itself
        /// lies inside the rectangle, zero will be returned.
        /// <param name="point">The point.</param>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>The squared distance between the point and the rectangle.</returns>
        public static float DistanceSquared(Vector2 point, Rectangle rectangle)
        {
            bool left = point.X < rectangle.Min.X;
            bool right = rectangle.Max.X < point.X;
            bool under = point.Y < rectangle.Min.Y;
            bool over = rectangle.Max.Y < point.Y;

            // Is the shortest distance measured from a face?
            if (!left && !right)
            {
                if (under)
                    return (rectangle.Min.Y - point.Y) * (rectangle.Min.Y - point.Y);
                if (over)
                    return (point.Y - rectangle.Max.Y) * (point.Y - rectangle.Max.Y);
                return 0;
            }
            if (!under && !over)
            {
                if (left)
                    return (rectangle.Min.X - point.X) * (rectangle.Min.X - point.X);
                if (right)
                    return (point.X - rectangle.Max.X) * (point.X - rectangle.Max.X);
                return 0;
            }

            // Shortest distance is measured from a corner.
            if (left)
            {
                if (under)
                    return
                        (rectangle.Min.X - point.X) * (rectangle.Min.X - point.X) +
                        (rectangle.Min.Y - point.Y) * (rectangle.Min.Y - point.Y);
                else // over
                    return
                        (rectangle.Min.X - point.X) * (rectangle.Min.X - point.X) +
                        (point.Y - rectangle.Max.Y) * (point.Y - rectangle.Max.Y);
            }
            else // right
            {
                if (under)
                    return
                        (point.X - rectangle.Max.X) * (point.X - rectangle.Max.X) +
                        (rectangle.Min.Y - point.Y) * (rectangle.Min.Y - point.Y);
                else // over
                    return
                        (point.X - rectangle.Max.X) * (point.X - rectangle.Max.X) +
                        (point.Y - rectangle.Max.Y) * (point.Y - rectangle.Max.Y);
            }
        }

        /// <summary>
        /// Returns the distance between the given point and polygon.
        /// </summary>
        /// The returned distance is the least distance between the point and any
        /// point that lies in the polygon. In particular, if the point itself
        /// lies inside the polygon, zero will be returned.
        /// <param name="point">The point.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns>The distance between the given point and polygon.</returns>
        public static float Distance(Vector2 point, Polygon polygon)
        {
            return (float)Math.Sqrt(DistanceSquared(point, polygon));
        }

        /// <summary>
        /// Returns the squared distance between the given point and polygon.
        /// </summary>
        /// The distance is the least distance between the point and any
        /// point that lies in the polygon. In particular, if the point itself
        /// lies inside the polygon, zero will be returned.
        /// <param name="point">The point.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns>The squared distance between the given point and polygon.</returns>
        public static float DistanceSquared(Vector2 point, Polygon polygon)
        {
            if (Intersect(point, polygon)) return 0;
            Vector2[] vertices = polygon.Vertices;
            if (polygon.FaceStrips != null)
            {
                float bestDistanceSquared = Single.MaxValue;
                float bestStripDistanceSquared = Single.MaxValue;
                int bestStripI = -1;

                // The distance (from the point) to each face strip is at most
                // the least distance to the farthest corner of a face of the
                // bounding box of the face strip, i.e. the second-shortest distance
                // to a corner of the strip's bounding box. This is so because the
                // bounding box is tight and thus there is at least one vertex
                // of the face strip lying on each face of the bounding box.
                // If the point is in the bounding box, the strip must be checked.
                // From bounding boxes not containing the point one must check
                // the closest strip and all strips whose closest corner is closer
                // than the second-closest corner of the closest strip.
                // For this, the strips will be ordered in 'stripIs' by distance 
                // to their bounding box. Strips that contain the query point
                // we mark with distance zero, thus always making them to be checked.
                // 'stripIs' and 'stripDistances' have the same indexing.
                int[] stripIs = new int[polygon.FaceStrips.Length];
                for (int i = 0; i < stripIs.Length; ++i) stripIs[i] = i;
                float[] stripDistancesSquared = new float[polygon.FaceStrips.Length];
                for (int stripI = 0; stripI < polygon.FaceStrips.Length; ++stripI)
                {
                    Polygon.FaceStrip strip = polygon.FaceStrips[stripI];
                    if (Geometry.Intersect(point, strip.BoundingBox))
                    {
                        stripDistancesSquared[stripI] = 0;
                    }
                    else
                    {
                        // Seek out the closest face strip of those that don't contain the query point.
                        float[] cornerDistsSquared = new float[4]
                        { 
                            Vector2.DistanceSquared(point, new Vector2(strip.BoundingBox.Min.X, strip.BoundingBox.Min.Y)),
                            Vector2.DistanceSquared(point, new Vector2(strip.BoundingBox.Min.X, strip.BoundingBox.Max.Y)),
                            Vector2.DistanceSquared(point, new Vector2(strip.BoundingBox.Max.X, strip.BoundingBox.Min.Y)),
                            Vector2.DistanceSquared(point, new Vector2(strip.BoundingBox.Max.X, strip.BoundingBox.Max.Y))
                        };
                        Array.Sort(cornerDistsSquared);
                        stripDistancesSquared[stripI] = DistanceSquared(point, strip.BoundingBox);
                        float stripDistanceSquared = cornerDistsSquared[1];
                        if (stripDistanceSquared < bestStripDistanceSquared)
                        {
                            bestStripDistanceSquared = stripDistanceSquared;
                            bestStripI = stripI;
                        }
                    }
                }

                Array.Sort(stripDistancesSquared, stripIs);
                for (int stripI = 0; stripI < stripDistancesSquared.Length && stripDistancesSquared[stripI] <= bestStripDistanceSquared; ++stripI)
                {
                    Polygon.FaceStrip strip = polygon.FaceStrips[stripIs[stripI]];
                    int oldI = strip.StartIndex;
                    for (int vertI = strip.StartIndex + 1; vertI <= strip.EndIndex; ++vertI)
                    {
                        int realI = vertI % vertices.Length;
                        bestDistanceSquared = MathHelper.Min(bestDistanceSquared,
                            DistanceSquared(point, vertices[oldI], vertices[realI]));
                        oldI = realI;
                    }
                }

                return bestDistanceSquared;
            }
            else
            {
                float bestDistanceSquared = Single.MaxValue;
                Vector2 oldV = polygon.Vertices[vertices.Length - 1];
                foreach (Vector2 v in vertices)
                {
                    bestDistanceSquared = MathHelper.Min(bestDistanceSquared, DistanceSquared(point, oldV, v));
                    oldV = v;
                }
                return bestDistanceSquared;
            }
        }

        #endregion Location and distance query methods

        #region Random location methods

        /// <summary>
        /// Returns a random location inside an area.
        /// </summary>
        /// <param name="prim">The area.</param>
        /// <returns>A random location inside the area.</returns>
        public static Vector2 GetRandomLocation(IGeomPrimitive prim)
        {
            var type = prim.GetType();
            if (type == typeof(Circle)) return GetRandomLocation((Circle)prim);
            if (type == typeof(Rectangle))
            {
                var rectangle = (Rectangle)prim;
                return RandomHelper.GetRandomVector2(rectangle.Min, rectangle.Max);
            }

            // For any other geometric primitive, randomise points in its bounding box
            // until we hit inside the primitive. This is generic but can be increasingly
            // inefficient with certain primitives.
            var boundingBox = prim.BoundingBox;
            for (int i = 0; i < 1000000; ++i)
            {
                var pos = RandomHelper.GetRandomVector2(boundingBox.Min, boundingBox.Max);
                if (Intersect(pos, prim)) return pos;
            }
            throw new NotImplementedException("GetRandomLocation not properly implemented for " + type.Name);
        }

        /// <summary>
        /// Returns a random location inside a circle. Returned locations will be
        /// uniformly distributed.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <returns>A random location inside the circle.</returns>
        public static Vector2 GetRandomLocation(Circle circle)
        {
            Vector2 pos, dirUnit;
            float dirAngle;
            RandomHelper.GetRandomCirclePoint(circle.Radius, out pos, out dirUnit, out dirAngle);
            return pos + circle.Center;
        }

        #endregion Random location methods

        #region Other methods

        /// <summary>
        /// Translates cartesian coordinates into normalised barycentric 
        /// coordinates relative to a triangle.
        /// </summary>
        /// The triangle is given as the three vectors, v1, v2 and v3, and the
        /// coordinates to translate as the vector p. The resulting barycentric
        /// coordinates are (A,B,C), of which B and C are stored to amount2 and
        /// amount3, and A is 1-amount2-amount3.
        /// If the triangle is reduced, the return value is undefined.
        /// <param name="v1">First vertex of the triangle.</param>
        /// <param name="v2">Second vertex of the triangle.</param>
        /// <param name="v3">Third vertex of the triangle.</param>
        /// <param name="p">Coordinates to translate.</param>
        /// <param name="amount2">Resulting barycentric coordinate B.</param>
        /// <param name="amount3">Resulting barycentric coordinate C.</param>
        public static void CartesianToBarycentric(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 p,
            out float amount2, out float amount3)
        {
            float denom = (v2.X - v1.X) * (v3.Y - v1.Y) + (v1.X - v3.X) * (v2.Y - v1.Y);
            if (denom == 0)
            {
                // Triangle's faces are all parallel.
                amount2 = 0;
                amount3 = 0;
            }
            else
            {
                amount2 = ((v1.X - p.X) * (v1.Y - v3.Y) + (v3.X - v1.X) * (v1.Y - p.Y)) / denom;
                amount3 = ((v2.X - v1.X) * (p.Y - v1.Y) + (p.X - v1.X) * (v1.Y - v2.Y)) / denom;
            }
        }

        /// <summary>
        /// Translates cartesian coordinates into normalised barycentric 
        /// coordinates relative to a triangle.
        /// </summary>
        /// The triangle is given as the three vectors, v1, v2 and v3, and the
        /// coordinates to translate as the vector p. The resulting barycentric
        /// coordinates are (A,B,C), of which B and C are stored to amount2 and
        /// amount3, and A is 1-amount2-amount3.
        /// If the triangle is reduced, the return value is undefined.
        /// <param name="v1">First vertex of the triangle.</param>
        /// <param name="v2">Second vertex of the triangle.</param>
        /// <param name="v3">Third vertex of the triangle.</param>
        /// <param name="pX">X coordinate to translate.</param>
        /// <param name="pY">Y coordinate to translate.</param>
        /// <param name="amount2">Resulting barycentric coordinate B.</param>
        /// <param name="amount3">Resulting barycentric coordinate C.</param>
        public static void CartesianToBarycentric(Vector2 v1, Vector2 v2, Vector2 v3,
            double pX, double pY,
            out double amount2, out double amount3)
        {
            double denom = ((double)v2.X - (double)v1.X) * ((double)v3.Y - (double)v1.Y)
                + ((double)v1.X - (double)v3.X) * ((double)v2.Y - (double)v1.Y);
            if (denom == 0)
            {
                // Triangle's faces are all parallel.
                amount2 = 0;
                amount3 = 0;
            }
            else
            {
                amount2 = (((double)v1.X - pX) * ((double)v1.Y - (double)v3.Y)
                    + ((double)v3.X - (double)v1.X) * ((double)v1.Y - pY)) / denom;
                amount3 = (((double)v2.X - (double)v1.X) * ((double)pY - (double)v1.Y)
                    + (pX - (double)v1.X) * ((double)v1.Y - (double)v2.Y)) / denom;
            }
        }

        /// <summary>
        /// Translates normalised barycentric coordinates relative to a triangle 
        /// into Cartesian coordinates.
        /// </summary>
        /// The weight of the triangle's first vertex is 1 - amount2 - amount3.
        /// <param name="v1">First vertex of the triangle.</param>
        /// <param name="v2">Second vertex of the triangle.</param>
        /// <param name="v3">Third vertex of the triangle.</param>
        /// <param name="amount2">Barycentric coordinate 2; weight of triangle's second vertex.</param>
        /// <param name="amount3">Barycentric coordinate 3; weight of triangle's third vertex.</param>
        public static Vector2 BarycentricToCartesian(Vector2 v1, Vector2 v2, Vector2 v3,
            double amount2, double amount3)
        {
            double amount1 = 1 - amount2 - amount3;
            return new Vector2((float)(v1.X * amount1 + v2.X * amount2 + v3.X * amount3),
                (float)(v1.Y * amount1 + v2.Y * amount2 + v3.Y * amount3));
        }

        /// <summary>
        /// Translates normalised barycentric coordinates relative to a triangle 
        /// into Cartesian coordinates.
        /// </summary>
        /// The weight of the triangle's first vertex is 1 - amount2 - amount3.
        /// <param name="v1">First vertex of the triangle.</param>
        /// <param name="v2">Second vertex of the triangle.</param>
        /// <param name="v3">Third vertex of the triangle.</param>
        /// <param name="amount2">Barycentric coordinate 2; weight of triangle's second vertex.</param>
        /// <param name="amount3">Barycentric coordinate 3; weight of triangle's third vertex.</param>
        public static Vector3 BarycentricToCartesian(Vector3 v1, Vector3 v2, Vector3 v3,
            double amount2, double amount3)
        {
            double amount1 = 1 - amount2 - amount3;
            return new Vector3((float)(v1.X * amount1 + v2.X * amount2 + v3.X * amount3),
                (float)(v1.Y * amount1 + v2.Y * amount2 + v3.Y * amount3),
                (float)(v1.Z * amount1 + v2.Z * amount2 + v3.Z * amount3));
        }

        #endregion Other methods
    }
}

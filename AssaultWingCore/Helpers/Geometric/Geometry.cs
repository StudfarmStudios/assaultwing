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
        #region Type definitions

        /// <summary>
        /// Kinds of intersection volumes that two lines can have.
        /// </summary>
        public enum LineIntersectionType
        {
            /// <summary>
            /// The lines don't intersect.
            /// </summary>
            None,
            /// <summary>
            /// The lines intersect at one point.
            /// </summary>
            Point,
            /// <summary>
            /// The lines intersect at a line interval (infinitely many points).
            /// </summary>
            Segment,
        }

        /// <summary>
        /// Type of stand of a point relative to the directed edge of a geometric object.
        /// </summary>
        public enum StandType
        {
            /// <summary>
            /// The point stands on the left hand side of the directed edge.
            /// </summary>
            Left,

            /// <summary>
            /// The point stands on the right hand side of the directed edge.
            /// </summary>
            Right,

            /// <summary>
            /// The point stands on the edge.
            /// </summary>
            Edge,
        }

        #endregion Type definitions

        #region Intersection methods

        /// <summary>
        /// Returns true iff the two geometric primitives intersect.
        /// </summary>
        /// <param name="prim1">One primitive.</param>
        /// <param name="prim2">The other primitive</param>
        /// <returns>True iff the two geometric primitives intersect.</returns>
        public static bool Intersect(IGeomPrimitive prim1, IGeomPrimitive prim2)
        {
            // Check fast cases.
            if (prim1 is Point)
            {
                Point point1 = (Point)prim1;
                if (prim2 is Point) return point1.Location.Equals(((Point)prim2).Location);
                if (prim2 is Circle) return Intersect(point1, (Circle)prim2);
                if (prim2 is Rectangle) return Intersect(point1, (Rectangle)prim2);
            }
            if (prim1 is Circle)
            {
                Circle circle1 = (Circle)prim1;
                if (prim2 is Point) return Intersect((Point)prim2, circle1);
                if (prim2 is Circle) return Intersect(circle1, (Circle)prim2);
                if (prim2 is Rectangle) return Intersect(circle1, (Rectangle)prim2);
            }
            if (prim1 is Rectangle)
            {
                Rectangle rectangle1 = (Rectangle)prim1;
                if (prim2 is Point) return Intersect((Point)prim2, rectangle1);
                if (prim2 is Circle) return Intersect((Circle)prim2, rectangle1);
                if (prim2 is Rectangle) return Intersect((Rectangle)prim2, rectangle1);
            }

            // Prune further checks by bounding boxes.
            if (!Intersect(prim1.BoundingBox, prim2.BoundingBox)) return false;

            // Check remaining, slow cases.
            if (prim1 is Point)
            {
                Point point1 = (Point)prim1;
                if (prim2 is Triangle) return Intersect(point1, (Triangle)prim2);
                if (prim2 is Polygon) return Intersect(point1, (Polygon)prim2);
            }
            if (prim1 is Circle)
            {
                Circle circle1 = (Circle)prim1;
                if (prim2 is Triangle) return Intersect(circle1, (Triangle)prim2);
                if (prim2 is Polygon) return Intersect(circle1, (Polygon)prim2);
            }
            if (prim1 is Triangle)
            {
                Triangle triangle1 = (Triangle)prim1;
                if (prim2 is Point) return Intersect((Point)prim2, triangle1);
                if (prim2 is Circle) return Intersect((Circle)prim2, triangle1);
                if (prim2 is Rectangle) return Intersect((Rectangle)prim2, triangle1);
                if (prim2 is Triangle) return Intersect(triangle1, (Triangle)prim2);
                if (prim2 is Polygon) return Intersect(triangle1, (Polygon)prim2);
            }
            if (prim1 is Polygon)
            {
                Polygon polygon1 = (Polygon)prim1;
                if (prim2 is Point) return Intersect((Point)prim2, polygon1);
                if (prim2 is Circle) return Intersect((Circle)prim2, polygon1);
                if (prim2 is Rectangle) return Intersect((Rectangle)prim2, polygon1);
                if (prim2 is Triangle) return Intersect((Triangle)prim2, polygon1);
                if (prim2 is Polygon) return Intersect(polygon1, (Polygon)prim2);
            }
            throw new Exception("Unknown geometric primitives in Geometry.Intersect(): " +
                prim1.GetType().Name + " " + prim2.GetType().Name);
        }

        /// <summary>
        /// Returns true iff the point is in the circle.
        /// </summary>
        /// The circle is thought of as a solid disc that contains also its edge.
        /// <param name="point">The point.</param>
        /// <param name="circle">The circle.</param>
        /// <returns>True iff the point is in the circle.</returns>
        public static bool Intersect(Point point, Circle circle)
        {
            return Vector2.DistanceSquared(point.Location, circle.Center) <= circle.Radius * circle.Radius;
        }

        /// <summary>
        /// Returns true iff the two circles intersect.
        /// </summary>
        /// The circles are thought of as solid discs that contain also their edges.
        /// <param name="circle1">One circle.</param>
        /// <param name="circle2">The other circle.</param>
        /// <returns>True iff the circles intersect.</returns>
        public static bool Intersect(Circle circle1, Circle circle2)
        {
            // The circles intersect iff their centers are at most the sum of
            // their radii apart. We can just as well compare the squares of the distances.
            float radiiSum = circle1.Radius + circle2.Radius;
            return Vector2.DistanceSquared(circle1.Center, circle2.Center) <= radiiSum * radiiSum;
        }

        /// <summary>
        /// Returns if a point and a rectangle intersect.
        /// </summary>
        /// The rectangle is considered solid.
        /// <param name="point">The point.</param>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns><c>true</c> if the point and the rectangle intersect,
        /// <c>false</c> otherwise.</returns>
        public static bool Intersect(Point point, Rectangle rectangle)
        {
            return
                rectangle.Min.X <= point.Location.X && point.Location.X <= rectangle.Max.X &&
                rectangle.Min.Y <= point.Location.Y && point.Location.Y <= rectangle.Max.Y;
        }

        /// <summary>
        /// Returns if a circle and a rectangle intersect.
        /// </summary>
        /// The primitives are considered solid.
        /// <param name="circle">The circle.</param>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns><c>true</c> if the circle and the rectangle intersect,
        /// <c>false</c> otherwise.</returns>
        public static bool Intersect(Circle circle, Rectangle rectangle)
        {
            // Adapted from the C function
            // Fast Circle-Rectangle Intersection Checking
            // by Clifford A. Shaffer
            // from http://tog.acm.org/GraphicsGems/gems/CircleRect.c
            // on 2008-06-19, implemented after the article in
            // "Graphics Gems", Academic Press, 1990.

            // Rectangle corners relative to the circle's center.
            Vector2 relativeMin = rectangle.Min - circle.Center;
            Vector2 relativeMax = rectangle.Max - circle.Center;

            float radius2 = circle.Radius * circle.Radius;
            if (relativeMax.X < 0)          /* R to left of circle center */
                if (relativeMax.Y < 0)      /* R in lower left corner */
                    return (relativeMax.X * relativeMax.X + relativeMax.Y * relativeMax.Y) <= radius2;
                else if (relativeMin.Y > 0) /* R in upper left corner */
                    return (relativeMax.X * relativeMax.X + relativeMin.Y * relativeMin.Y) <= radius2;
                else                        /* R due West of circle */
                    return -relativeMax.X <= circle.Radius;
            else if (relativeMin.X > 0)     /* R to right of circle center */
                if (relativeMax.Y < 0)      /* R in lower right corner */
                    return (relativeMin.X * relativeMin.X + relativeMax.Y * relativeMax.Y) <= radius2;
                else if (relativeMin.Y > 0) /* R in upper right corner */
                    return (relativeMin.X * relativeMin.X + relativeMin.Y * relativeMin.Y) <= radius2;
                else                        /* R due East of circle */
                    return relativeMin.X <= circle.Radius;
            else                            /* R on circle vertical centerline */
                if (relativeMax.Y < 0)      /* R due South of circle */
                    return -relativeMax.Y <= circle.Radius;
                else if (relativeMin.Y > 0) /* R due North of circle */
                    return relativeMin.Y <= circle.Radius;
                else                        /* R contains circle centerpoint */
                    return true;
        }

        /// <summary>
        /// Returns if two rectangles intersect.
        /// </summary>
        /// The rectangles are considered solid.
        /// <param name="rectangle1">One rectangle.</param>
        /// <param name="rectangle2">The other rectangle.</param>
        /// <returns><c>true</c> if the rectangles intersect,
        /// <c>false</c> otherwise.</returns>
        public static bool Intersect(Rectangle rectangle1, Rectangle rectangle2)
        {
            return !(
                rectangle1.Max.X < rectangle2.Min.X ||
                rectangle1.Max.Y < rectangle2.Min.Y ||
                rectangle2.Max.X < rectangle1.Min.X ||
                rectangle2.Max.Y < rectangle1.Min.Y);
        }

        /// <summary>
        /// Returns if a point lies inside a triangle.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="triangle">The triangle.</param>
        /// <returns><b>true</b>if the point lies inside the triangle, 
        /// <b>false</b> otherwise.</returns>
        public static bool Intersect(Point point, Triangle triangle)
        {
            // Adapted from C code by Eric Haines, from http://www.graphicsgems.org/.

            // Shoot a test ray along +X axis.  The strategy is to compare vertex Y values
            // to the testing point's Y and quickly discard edges which are entirely to one
            // side of the test ray.
            Vector2 p1 = triangle.P1;
            Vector2 p2 = triangle.P2;
            Vector2 p3 = triangle.P3;
            bool yflag1 = p1.Y >= point.Location.Y;
            bool yflag2 = p2.Y >= point.Location.Y;
            bool yflag3 = p3.Y >= point.Location.Y;
            bool inside_flag = false;
            if (yflag1 != yflag2 &&
                yflag2 == ((p2.Y - point.Location.Y) * (p1.X - p2.X) >= (p2.X - point.Location.X) * (p1.Y - p2.Y)))
                inside_flag = !inside_flag;
            if (yflag2 != yflag3 &&
                yflag3 == ((p3.Y - point.Location.Y) * (p2.X - p3.X) >= (p3.X - point.Location.X) * (p2.Y - p3.Y)))
                inside_flag = !inside_flag;
            if (yflag3 != yflag1 &&
                yflag1 == ((p1.Y - point.Location.Y) * (p3.X - p1.X) >= (p1.X - point.Location.X) * (p3.Y - p1.Y)))
                inside_flag = !inside_flag;
            return inside_flag;
        }

        /// <summary>
        /// Returns if a circle intersects a triangle.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <param name="triangle">The triangle.</param>
        /// <returns><b>true</b>if the circle intersects the triangle, 
        /// <b>false</b> otherwise.</returns>
        public static bool Intersect(Circle circle, Triangle triangle)
        {
            float radius = circle.Radius;
            return DistanceSquared(new Point(circle.Center), triangle) <= radius * radius;
        }

        /// <summary>
        /// Returns if a rectangle and a triangle intersect.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <param name="triangle">The triangle.</param>
        /// <returns><b>true</b>if the rectangle and the triangle intersect, 
        /// <b>false</b> otherwise.</returns>
        public static bool Intersect(Rectangle rectangle, Triangle triangle)
        {
            throw new NotImplementedException("Method not implemented: Intersect(Rectangle, Triangle)");
        }

        /// <summary>
        /// Returns if two triangles intersect.
        /// </summary>
        /// <param name="triangle1">One triangle.</param>
        /// <param name="triangle2">Another triangle.</param>
        /// <returns><b>true</b>if the two triangles intersect, 
        /// <b>false</b> otherwise.</returns>
        public static bool Intersect(Triangle triangle1, Triangle triangle2)
        {
            throw new NotImplementedException("Method not implemented: Intersect(Triangle, Triangle)");
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
        /// Returns the kind of intersection of the two line segments ab and cd.
        /// </summary>
        /// <param name="a">One end of the first line segment.</param>
        /// <param name="b">The other end of the first line segment.</param>
        /// <param name="c">One end of the second line segment.</param>
        /// <param name="d">The other end of the second line segment.</param>
        /// <returns>How the two line segments intersect, if at all.</returns>
        public static LineIntersectionType Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
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
                if (x1hi < d.X || c.X < x1lo) return LineIntersectionType.None;
            }
            else
            {
                if (x1hi < c.X || d.X < x1lo) return LineIntersectionType.None;
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
                if (y1hi < d.Y || c.Y < y1lo) return LineIntersectionType.None;
            }
            else
            {
                if (y1hi < c.Y || d.Y < y1lo) return LineIntersectionType.None;
            }

            float Cx = a.X - c.X;
            float Cy = a.Y - c.Y;
            float val1 = By * Cx - Bx * Cy;
            float val3 = Ay * Bx - Ax * By;
            if (val3 > 0)
            {
                if (val1 < 0 || val1 > val3) return LineIntersectionType.None;
            }
            else
            {
                if (val1 > 0 || val1 < val3) return LineIntersectionType.None;
            }
            float val2 = Ax * Cy - Ay * Cx;
            if (val3 > 0)
            {
                if (val2 < 0 || val2 > val3) return LineIntersectionType.None;
            }
            else
            {
                if (val2 > 0 || val2 < val3) return LineIntersectionType.None;
            }

            if (val3 == 0) return LineIntersectionType.Segment; // TODO: Fix case of point intersection
            return LineIntersectionType.Point;
        }

        /// <summary>
        /// Returns true iff the point is in the area defined by the polygon.
        /// </summary>
        /// Edges are considered to belong to the polygon.
        /// <param name="point">The point to check.</param>
        /// <param name="polygon">The polygon to check.</param>
        /// <returns>True iff the point is in the polygon.</returns>
        public static bool Intersect(Point point, Polygon polygon)
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
            bool yflag0 = vPrev.Y >= point.Location.Y;

            bool inside_flag = false;
            foreach (Vector2 v in polygon.Vertices)
            {
                bool yflag1 = v.Y >= point.Location.Y;

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
                    if (((v.Y - point.Location.Y) * (vPrev.X - v.X) >=
                        (v.X - point.Location.X) * (vPrev.Y - v.Y)) == yflag1)
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
        /// Returns true iff the circle intersects the polygon.
        /// </summary>
        /// The circle and the polygon are considered to contain their respective edges.
        /// <param name="circle">The circle to check.</param>
        /// <param name="polygon">The polygon to check.</param>
        /// <returns>True iff the circle intersects the polygon.</returns>
        public static bool Intersect(Circle circle, Polygon polygon)
        {
            return DistanceSquared(new Point(circle.Center), polygon) < circle.Radius * circle.Radius;
        }

        /// <summary>
        /// Returns if a rectangle and a polygon intersect.
        /// </summary>
        /// <param name="rectangle">The rectangle.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns><b>true</b>if the rectangle and the polygon intersect, 
        /// <b>false</b> otherwise.</returns>
        public static bool Intersect(Rectangle rectangle, Polygon polygon)
        {
            throw new NotImplementedException("Method not implemented: Intersect(Rectangle, Polygon)");
        }

        /// <summary>
        /// Returns if a triangle intersects a polygon.
        /// </summary>
        /// <param name="triangle">The triangle.</param>
        /// <param name="polygon">The polygon.</param>
        /// <returns><b>true</b>if the circle intersects the polygon, 
        /// <b>false</b> otherwise.</returns>
        public static bool Intersect(Triangle triangle, Polygon polygon)
        {
            throw new NotImplementedException("Method not implemented: Intersect(Triangle, Polygon)");
        }

        /// <summary>
        /// Returns true iff the two polygons intersect.
        /// </summary>
        /// The polygons are considered to contain their respective edges.
        /// <param name="polygon1">One polygon.</param>
        /// <param name="polygon2">The other polygon.</param>
        /// <returns>True iff the two polygons intersect.</returns>
        public static bool Intersect(Polygon polygon1, Polygon polygon2)
        {
            throw new NotImplementedException("Method not implemented: Intersect(Polygon, Polygon)");
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
        /// Returns the distance between two geometric primitives.
        /// </summary>
        /// Distance is the length of the shortest line segment that connects
        /// the geometric primitives.
        /// <param name="prim1">One primitive.</param>
        /// <param name="prim2">The other primitive</param>
        /// <returns>The distance between the two geometric primitives.</returns>
        public static float Distance(IGeomPrimitive prim1, IGeomPrimitive prim2)
        {
            if (prim1 is Point)
            {
                Point point1 = (Point)prim1;
                if (prim2 is Point) return (point1.Location - ((Point)prim2).Location).Length();
                if (prim2 is Circle) return Distance(point1, (Circle)prim2);
                if (prim2 is Rectangle) return Distance(point1, (Rectangle)prim2);
                if (prim2 is Triangle) return Distance(point1, (Triangle)prim2);
                if (prim2 is Polygon) return Distance(point1, (Polygon)prim2);
            }
            if (prim1 is Circle)
            {
                Circle circle1 = (Circle)prim1;
                if (prim2 is Point) return Distance((Point)prim2, circle1);
                if (prim2 is Circle) return Distance(circle1, (Circle)prim2);
                if (prim2 is Rectangle) return Distance(circle1, (Rectangle)prim2);
                if (prim2 is Triangle) return Distance(circle1, (Triangle)prim2);
            }
            if (prim1 is Rectangle)
            {
                Rectangle rectangle1 = (Rectangle)prim1;
                if (prim2 is Point) return Distance((Point)prim2, rectangle1);
                if (prim2 is Circle) return Distance((Circle)prim2, rectangle1);
            }
            if (prim1 is Triangle)
            {
                Triangle triangle1 = (Triangle)prim1;
                if (prim2 is Point) return Distance((Point)prim2, triangle1);
                if (prim2 is Circle) return Distance((Circle)prim2, triangle1);
            }
            if (prim1 is Polygon)
            {
                Polygon polygon1 = (Polygon)prim1;
                if (prim2 is Point) return Distance((Point)prim2, polygon1);
                if (prim2 is Circle) return Distance((Circle)prim2, polygon1);
            }
            throw new NotImplementedException("Geometry.Distance() not implemented for " +
                prim1.GetType().Name + " and " + prim2.GetType().Name);
        }

        /// <summary>
        /// Returns the distance between 'point' and the line segment ab.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <returns>The distance between 'point' and the line segment ab.</returns>
        public static float Distance(Point point, Vector2 a, Vector2 b)
        {
            return (float)Math.Sqrt(DistanceSquared(point, a, b));
        }

        /// <summary>
        /// Returns the squared distance between 'point' and the line segment ab.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="a">One end of the line segment.</param>
        /// <param name="b">The other end of the line segment.</param>
        /// <returns>The squared distance between 'point' and the line segment ab.</returns>
        public static float DistanceSquared(Point point, Vector2 a, Vector2 b)
        {
            // There's three cases to consider, depending on the point's projection
            // on the line that a and b define.
            Vector2 ab = b - a;
            Vector2 ap = point.Location - a;
            Vector2 bp = point.Location - b;
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
        public static Point GetClosestPoint(Vector2 a, Vector2 b, Point point)
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
        public static Point GetClosestPoint(Vector2 a, Vector2 b, Point point, out float distance)
        {
            // There's three cases to consider, depending on the point's projection
            // on the line that a and b define.
            Vector2 ab = b - a;
            Vector2 ap = point.Location - a;
            Vector2 bp = point.Location - b;
            float dot1 = Vector2.Dot(ab, ap);
            float dot2 = Vector2.Dot(ab, bp);
            Point closestPoint;

            // Case 1: Point's projection is outside the line segment, closer to a than b.
            if (dot1 <= 0)
                closestPoint = new Point(a);

            // Case 2: Point's projection is outside the line segment, closer to b than a.
            else if (dot2 >= 0)
                closestPoint = new Point(b);

            // Case 3: Point's projection is on the line segment.
            else
            {
                Vector2 apProjectedToAb = ab * Vector2.Dot(ap, ab) / ab.LengthSquared();
                closestPoint = new Point(a + apProjectedToAb);
            }

            distance = (point.Location - closestPoint.Location).Length();
            return closestPoint;
        }

        /// <summary>
        /// Returns the distance from a point to a circle.
        /// If the point is inside the circle, the distance is zero.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="circle">The circle.</param>
        /// <returns>The distance from the point to the circle.</returns>
        public static float Distance(Point point, Circle circle)
        {
            return Math.Max(0, (point.Location - circle.Center).Length() - circle.Radius);
        }

        /// <summary>
        /// Returns the distance from a point to a triangle.
        /// If the point is inside the triangle, the distance is zero.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="triangle">The triangle.</param>
        /// <returns>The distance from the point to the triangle.</returns>
        public static float Distance(Point point, Triangle triangle)
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
            Vector2 p1p = point.Location - p1;
            Vector2 e12 = p2 - p1;
            Vector2 e13 = p3 - p1;
            float halfplane12 = Vector2.Dot(p1p, e12);
            float halfplane13 = Vector2.Dot(p1p, e13);
            if (halfplane12 <= 0 && halfplane13 <= 0)
                return Vector2.Distance(point.Location, p1);

            // Is 't.P2' the closest point?
            Vector2 p2p = point.Location - p2;
            Vector2 e23 = p3 - p2;
            float halfplane21 = -Vector2.Dot(p2p, e12);
            float halfplane23 = Vector2.Dot(p2p, e23);
            if (halfplane21 <= 0 && halfplane23 <= 0)
                return Vector2.Distance(point.Location, p2);

            // Is 't.P3' the closest point?
            Vector2 p3p = point.Location - p3;
            float halfplane31 = -Vector2.Dot(p3p, e13);
            float halfplane32 = -Vector2.Dot(p3p, e23);
            if (halfplane31 <= 0 && halfplane32 <= 0)
                return Vector2.Distance(point.Location, p3);

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
        /// <param name="point">The point.</param>
        /// <param name="triangle">The triangle.</param>
        /// <returns>The squared distance from the point to the triangle.</returns>
        public static float DistanceSquared(Point point, Triangle triangle)
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
            Vector2 p1p = point.Location - p1;
            Vector2 e12 = p2 - p1;
            Vector2 e13 = p3 - p1;
            float halfplane12 = Vector2.Dot(p1p, e12);
            float halfplane13 = Vector2.Dot(p1p, e13);
            if (halfplane12 <= 0 && halfplane13 <= 0)
                return Vector2.DistanceSquared(point.Location, p1);

            // Is 'p2' the closest point?
            Vector2 p2p = point.Location - p2;
            Vector2 e23 = p3 - p2;
            float halfplane21 = -Vector2.Dot(p2p, e12);
            float halfplane23 = Vector2.Dot(p2p, e23);
            if (halfplane21 <= 0 && halfplane23 <= 0)
                return Vector2.DistanceSquared(point.Location, p2);

            // Is 'p3' the closest point?
            Vector2 p3p = point.Location - p3;
            float halfplane31 = -Vector2.Dot(p3p, e13);
            float halfplane32 = -Vector2.Dot(p3p, e23);
            if (halfplane31 <= 0 && halfplane32 <= 0)
                return Vector2.DistanceSquared(point.Location, p3);

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
        /// <param name="point">The point.</param>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>The distance between the point and the rectangle.</returns>
        public static float Distance(Point point, Rectangle rectangle)
        {
            bool left = point.Location.X < rectangle.Min.X;
            bool right = rectangle.Max.X < point.Location.X;
            bool under = point.Location.Y < rectangle.Min.Y;
            bool over = rectangle.Max.Y < point.Location.Y;

            // Is the shortest distance measured from a face?
            if (!left && !right)
            {
                if (under)
                    return rectangle.Min.Y - point.Location.Y;
                if (over)
                    return point.Location.Y - rectangle.Max.Y;
                return 0;
            }
            if (!under && !over)
            {
                if (left)
                    return rectangle.Min.X - point.Location.X;
                if (right)
                    return point.Location.X - rectangle.Max.X;
                return 0;
            }

            // Shortest distance is measured from a corner.
            if (left)
            {
                if (under)
                    return (float)Math.Sqrt(
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y));
                else // over
                    return (float)Math.Sqrt(
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y));
            }
            else // right
            {
                if (under)
                    return (float)Math.Sqrt(
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y));
                else // over
                    return (float)Math.Sqrt(
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y));
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
        public static float DistanceSquared(Point point, Rectangle rectangle)
        {
            bool left = point.Location.X < rectangle.Min.X;
            bool right = rectangle.Max.X < point.Location.X;
            bool under = point.Location.Y < rectangle.Min.Y;
            bool over = rectangle.Max.Y < point.Location.Y;

            // Is the shortest distance measured from a face?
            if (!left && !right)
            {
                if (under)
                    return (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y);
                if (over)
                    return (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y);
                return 0;
            }
            if (!under && !over)
            {
                if (left)
                    return (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X);
                if (right)
                    return (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X);
                return 0;
            }

            // Shortest distance is measured from a corner.
            if (left)
            {
                if (under)
                    return
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y);
                else // over
                    return
                        (rectangle.Min.X - point.Location.X) * (rectangle.Min.X - point.Location.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y);
            }
            else // right
            {
                if (under)
                    return
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (rectangle.Min.Y - point.Location.Y) * (rectangle.Min.Y - point.Location.Y);
                else // over
                    return
                        (point.Location.X - rectangle.Max.X) * (point.Location.X - rectangle.Max.X) +
                        (point.Location.Y - rectangle.Max.Y) * (point.Location.Y - rectangle.Max.Y);
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
        public static float Distance(Point point, Polygon polygon)
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
        public static float DistanceSquared(Point point, Polygon polygon)
        {
            if (Intersect(point, polygon))
                return 0;
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
                        float[] cornerDistsSquared = new float[4] { 
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.BoundingBox.Min.X, strip.BoundingBox.Min.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.BoundingBox.Min.X, strip.BoundingBox.Max.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.BoundingBox.Max.X, strip.BoundingBox.Min.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.BoundingBox.Max.X, strip.BoundingBox.Max.Y))
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

        /// <summary>
        /// Returns the distance between two circles.
        /// If the circles intersect, the distance is zero.
        /// </summary>
        /// <param name="circle1">One circle.</param>
        /// <param name="circle2">The other circle.</param>
        /// <returns>The distance between the circles.</returns>
        public static float Distance(Circle circle1, Circle circle2)
        {
            return Math.Max(0, (circle1.Center - circle2.Center).Length() - circle1.Radius - circle2.Radius);
        }

        /// <summary>
        /// Returns the distance between a circle and a rectangle.
        /// If the circle and rectangle intersect, the distance is zero.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <param name="rectangle">The rectangle.</param>
        /// <returns>The distance between the circle and the rectangle.</returns>
        public static float Distance(Circle circle, Rectangle rectangle)
        {
            return Math.Max(0, Distance(new Point(circle.Center), rectangle) - circle.Radius);
        }

        /// <summary>
        /// Returns the distance between a circle and a triangle.
        /// If the circle and triangle intersect, the distance is zero.
        /// </summary>
        /// <param name="circle">The circle.</param>
        /// <param name="triangle">The triangle.</param>
        /// <returns>The distance between the circle and the triangle.</returns>
        public static float Distance(Circle circle, Triangle triangle)
        {
            return Math.Max(0, Distance(new Point(circle.Center), triangle) - circle.Radius);
        }

        /// <summary>
        /// Returns a point in the polygon that is maximally close to the given point.
        /// </summary>
        /// There may not be a unique closest point. In such a case the exact return
        /// value is undefined but will still meet the definition of the return value.
        /// If the given point is inside the polygon, the same point is returned.
        /// <param name="polygon">The polygon.</param>
        /// <param name="point">The point.</param>
        /// <returns>A point in the polygon that is maximally close to the given point.</returns>
        public static Point GetClosestPoint(Polygon polygon, Point point)
        {
            float distance;
            return GetClosestPoint(polygon, point, out distance);
        }

        /// <summary>
        /// Returns a point in the polygon that is maximally close to the given point.
        /// Also computes the distance from the polygon to the returned point.
        /// </summary>
        /// There may not be a unique closest point. In such a case the exact return
        /// value is undefined but will still meet the definition of the return value.
        /// If the given point is inside the polygon, the same point is returned.
        /// <param name="polygon">The polygon.</param>
        /// <param name="point">The point.</param>
        /// <param name="distance">Where to store the distance between the polygon and the returned point.</param>
        /// <returns>A point in the polygon that is maximally close to the given point.</returns>
        public static Point GetClosestPoint(Polygon polygon, Point point, out float distance)
        {
            if (Intersect(point, polygon))
            {
                distance = 0;
                return point;
            }
            float bestDistance = Single.MaxValue;
            Point bestPoint = point;
            int oldI = polygon.Vertices.Length - 1;
            for (int i = 0; i < polygon.Vertices.Length; oldI = i++)
            {
                float currentDistance;
                Point closestPoint = GetClosestPoint(polygon.Vertices[oldI], polygon.Vertices[i], point, out currentDistance);
                if (currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    bestPoint = closestPoint;
                }
            }
            distance = bestDistance;
            return bestPoint;
        }

        /// <summary>
        /// Returns a normalised vector pointing from an area toward another,
        /// or the zero vector if there's no candidate for the normal.
        /// </summary>
        /// <param name="prim1">The area to point from.</param>
        /// <param name="prim2">The area to point to.</param>
        /// <returns>A normalised vector pointing from an the first area toward the other,
        /// or the zero vector in difficult cases.</returns>
        public static Vector2 GetNormal(IGeomPrimitive prim1, IGeomPrimitive prim2)
        {
            if (prim1 is Point)
            {
                Point point1 = (Point)prim1;
                if (prim2 is Point)
                {
                    Vector2 difference = ((Point)prim2).Location - point1.Location;
                    return difference == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(difference);
                }
                if (prim2 is Circle)
                {
                    Vector2 difference = ((Circle)prim2).Center - point1.Location;
                    return difference == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(difference);
                }
                if (prim2 is Rectangle) throw new Exception("Geometry.GetNormal(Point, Rectangle) not implemented");
                if (prim2 is Triangle) return -GetNormal((Triangle)prim2, point1);
                if (prim2 is Polygon) return -GetNormal((Polygon)prim2, point1);
            }
            if (prim1 is Circle)
            {
                Circle circle1 = (Circle)prim1;
                if (prim2 is Point)
                {
                    Vector2 difference = ((Point)prim2).Location - circle1.Center;
                    return difference == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(difference);
                }
                if (prim2 is Circle)
                {
                    Vector2 difference = ((Circle)prim2).Center - circle1.Center;
                    return difference == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(difference);
                }
                if (prim2 is Rectangle) throw new Exception("Geometry.GetNormal(Circle, Rectangle) not implemented");
                if (prim2 is Triangle) return -GetNormal((Triangle)prim2, new Point(circle1.Center));
                if (prim2 is Polygon) return -GetNormal((Polygon)prim2, new Point(circle1.Center));
            }
            if (prim1 is Triangle)
            {
                Triangle triangle1 = (Triangle)prim1;
                if (prim2 is Point) return GetNormal(triangle1, (Point)prim2);
                if (prim2 is Circle) return GetNormal(triangle1, new Point(((Circle)prim2).Center));
                if (prim2 is Rectangle) throw new Exception("Geometry.GetNormal(Rectangle, Triangle) not implemented");
                if (prim2 is Triangle) throw new Exception("Geometry.GetNormal(Triangle, Triangle) not implemented");
                if (prim2 is Polygon) throw new Exception("Geometry.GetNormal(Triangle, Polygon) not implemented");
            }
            if (prim1 is Polygon)
            {
                Polygon polygon1 = (Polygon)prim1;
                if (prim2 is Point) return GetNormal(polygon1, (Point)prim2);
                if (prim2 is Circle) return GetNormal(polygon1, new Point(((Circle)prim2).Center));
                if (prim2 is Rectangle) throw new Exception("Geometry.GetNormal(Rectangle, Polygon) not implemented");
                if (prim2 is Triangle) throw new Exception("Geometry.GetNormal(Polygon, Triangle) not implemented");
                if (prim2 is Polygon) throw new Exception("Geometry.GetNormal(Polygon, Polygon) not implemented");
            }
            throw new Exception("Unknown geometric primitives in Geometry.GetNormal(): " +
                      prim1.GetType().Name + " " + prim2.GetType().Name);
        }

        /// <summary>
        /// Returns a unit normal vector from a triangle pointing towards a point.
        /// </summary>
        /// The returned vector will be normalised, it will be parallel to a shortest
        /// line segment that connects the triangle and the point, and it will
        /// point from the triangle towards the point. If the point lies inside
        /// the triangle, the zero vector will be returned.
        /// <param name="triangle">The triangle.</param>
        /// <param name="point">The point for the normal to point to.</param>
        /// <returns>A unit normal pointing to the given location.</returns>
        public static Vector2 GetNormal(Triangle triangle, Point point)
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
            Vector2 p1p = point.Location - p1;
            Vector2 e12 = p2 - p1;
            Vector2 e13 = p3 - p1;
            float halfplane12 = Vector2.Dot(p1p, e12);
            float halfplane13 = Vector2.Dot(p1p, e13);
            if (halfplane12 <= 0 && halfplane13 <= 0)
                return Vector2.Normalize(point.Location - p1);

            // Is 't.P2' the closest point?
            Vector2 p2p = point.Location - p2;
            Vector2 e23 = p3 - p2;
            float halfplane21 = -Vector2.Dot(p2p, e12);
            float halfplane23 = Vector2.Dot(p2p, e23);
            if (halfplane21 <= 0 && halfplane23 <= 0)
                return Vector2.Normalize(point.Location - p2);

            // Is 't.P3' the closest point?
            Vector2 p3p = point.Location - p3;
            float halfplane31 = -Vector2.Dot(p3p, e13);
            float halfplane32 = -Vector2.Dot(p3p, e23);
            if (halfplane31 <= 0 && halfplane32 <= 0)
                return Vector2.Normalize(point.Location - p3);

            // Is the closest point on the edge between 't.P2' and 't.P3'?
            float distance23 = Vector2.Dot(triangle.Normal23, p2p);
            if (distance23 >= 0 && halfplane23 >= 0 && halfplane32 >= 0)
                return triangle.Normal23;

            // Is the closest point on the edge between 't.P1' and 't.P3'?
            float distance13 = Vector2.Dot(triangle.Normal13, p1p);
            if (distance13 >= 0 && halfplane13 >= 0 && halfplane31 >= 0)
                return triangle.Normal13;

            // Is the closest point on the edge between 't.P1' and 't.P2'?
            float distance12 = Vector2.Dot(triangle.Normal12, p1p);
            if (distance12 >= 0 && halfplane12 >= 0 && halfplane21 >= 0)
                return triangle.Normal12;

            // Otherwise 'p' lies inside 't'.
            return Vector2.Zero;
        }

        /// <summary>
        /// Returns a unit normal vector from a polygon pointing towards a point.
        /// </summary>
        /// The returned vector will be normalised, it will be parallel to a shortest
        /// line segment that connects the polygon and the point, and it will
        /// point from the polygon towards the point. If the point lies inside
        /// the polygon, the zero vector will be returned.
        /// Note that the normal is not unique in all cases. In ambiguous cases the exact
        /// result is undefined but will obey the specified return conditions.
        /// <param name="polygon">The polygon.</param>
        /// <param name="point">The point for the normal to point to.</param>
        /// <returns>A unit normal pointing to the given location.</returns>
        public static Vector2 GetNormal(Polygon polygon, Point point)
        {
            Point closestPoint = GetClosestPoint(polygon, point);
            Vector2 difference = point.Location - closestPoint.Location;
            if (difference == Vector2.Zero)
                return Vector2.Zero;
            return Vector2.Normalize(difference);
        }

        /// <summary>
        /// Returns a unit normal vector from a set of polygons pointing towards a point.
        /// </summary>
        /// The returned vector will be normalised, it will be parallel to a shortest
        /// line segment that connects the polygons and the point, and it will
        /// point from the closest polygon towards the point. If the point lies inside
        /// a polygon, the zero vector will be returned.
        /// Note that the normal is not unique in all cases. In ambiguous cases the exact
        /// result is undefined but will obey the specified return conditions.
        /// <param name="polygons">The polygons.</param>
        /// <param name="point">The point the normal will point to.</param>
        /// <returns>A unit normal pointing to the given location.</returns>
        public static Vector2 GetNormal(IEnumerable<Polygon> polygons, Point point)
        {
            float bestDistance = float.MaxValue;
            Point bestPoint = null;
            foreach (Polygon polygon in polygons)
            {
                float distance;
                Point closestPoint = GetClosestPoint(polygon, point, out distance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = closestPoint;
                }
            }
            return Vector2.Normalize(point.Location - bestPoint.Location);
        }

        /// <summary>
        /// Returns where point p stands relative to the 
        /// directed line defined by the vector from a to b.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="a">The tail of the vector.</param>
        /// <param name="b">The head of the vector.</param>
        /// <returns>The stand of p relative to the
        /// directed line defined by the vector from a to b.</returns>
        public static StandType Stand(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 dir = b - a;
            Vector2 leftNormal = new Vector2(-dir.Y, dir.X);
            float dot = Vector2.Dot(leftNormal, p - a);
            return dot < 0 ? StandType.Right
                : dot > 0 ? StandType.Left
                : StandType.Edge;
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
            Type type = prim.GetType();
            if (type == typeof(Point))
                return ((Point)prim).Location;
            if (type == typeof(Circle))
                return GetRandomLocation((Circle)prim);
            if (type == typeof(Rectangle))
            {
                Rectangle rectangle = (Rectangle)prim;
                return RandomHelper.GetRandomVector2(rectangle.Min, rectangle.Max);
            }

            // For any other geometric primitive, randomise points in its bounding box
            // until we hit inside the primitive. This is generic but can be increasingly
            // inefficient with certain primitives.
            Rectangle boundingBox = prim.BoundingBox;
            for (int i = 0; i < 1000000; ++i)
            {
                Vector2 pos = RandomHelper.GetRandomVector2(boundingBox.Min, boundingBox.Max);
                if (Intersect(new Point(pos), prim))
                    return pos;
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

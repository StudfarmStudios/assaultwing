// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Text;
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
            // Check trivial cases.
            if (prim1 is Everything || prim2 is Everything)
                return true;

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
            throw new Exception("Method not implemented: Intersect(Rectangle, Triangle)");
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
            throw new Exception("Method not implemented: Intersect(Triangle, Triangle)");
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
#if GEOMETRY_APPROXIMATE
            // This approximation is unsuitable for ship vs. wall collisions;
            // players get stuck very easily.
            Point right = new Point(circle.Center + new Vector2(circle.Radius, 0));
            Point top = new Point(circle.Center + new Vector2(0, circle.Radius));
            Point left = new Point(circle.Center + new Vector2(-circle.Radius, 0));
            Point bottom = new Point(circle.Center + new Vector2(0, -circle.Radius));
            return Intersect(right, polygon) || Intersect(top, polygon)
                || Intersect(left, polygon) || Intersect(bottom, polygon);
#else
            return DistanceSquared(new Point(circle.Center), polygon) < circle.Radius * circle.Radius;
#endif
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
            throw new Exception("Method not implemented: Intersect(Rectangle, Polygon)");
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
            throw new Exception("Method not implemented: Intersect(Triangle, Polygon)");
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
            throw new Exception("Method not implemented: Intersect(Polygon, Polygon)");
        }

        #endregion Intersection methods

        #region Location and distance query methods

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
            if (prim1 is Everything || prim2 is Everything)
                return 0;
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
            throw new Exception("Geometry.Distance() not implemented for " +
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
                    if (Geometry.Intersect(point, strip.boundingBox))
                    {
                        stripDistancesSquared[stripI] = 0;
                    }
                    else
                    {
                        // Seek out the closest face strip of those that don't contain the query point.
                        float[] cornerDistsSquared = new float[4] { 
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Min.X, strip.boundingBox.Min.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Min.X, strip.boundingBox.Max.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Max.X, strip.boundingBox.Min.Y)),
                            Vector2.DistanceSquared(point.Location, new Vector2(strip.boundingBox.Max.X, strip.boundingBox.Max.Y))
                        };
                        Array.Sort(cornerDistsSquared);
                        stripDistancesSquared[stripI] = DistanceSquared(point, strip.boundingBox);
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
                    int oldI = strip.startIndex;
                    for (int vertI = strip.startIndex + 1; vertI <= strip.endIndex; ++vertI)
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
            if (prim1 is Everything || prim2 is Everything)
                return Vector2.Zero;
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
            if (type == typeof(Everything))
                return RandomHelper.GetRandomVector2(float.MinValue, float.MaxValue);
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
            float angle = RandomHelper.GetRandomFloat(0, MathHelper.TwoPi);
            float distance = circle.Radius * (float)Math.Sqrt(RandomHelper.globalRandomGenerator.NextDouble());
            return circle.Center + distance * new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
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

        #region Unit tests
#if DEBUG
        /// <summary>
        /// Tests the Geometry class.
        /// </summary>
        [TestFixture]
        public class GeometryTest
        {
            /// <summary>
            /// Sets up the testing.
            /// </summary>
            [SetUp]
            public void SetUp()
            {
            }

            /// <summary>
            /// Tests the general intersection method.
            /// </summary>
            [Test]
            public void TestGeneralIntersect()
            {
                Point p1 = new Point(new Vector2(10f, 10f));
                Point p2 = new Point(new Vector2(30f, 90f));
                Circle c1 = new Circle(new Vector2(20f, 10f), 20f);
                Circle c2 = new Circle(new Vector2(45f, 20f), 10f);
                Circle c3 = new Circle(new Vector2(90f, 90f), 20f);
                Vector2 q1 = new Vector2(0f, 0f);
                Vector2 q2 = new Vector2(30f, 10f);
                Vector2 q3 = new Vector2(20f, 90f);
                Vector2 q4 = new Vector2(10f, 90f);
                Vector2 q5 = new Vector2(-10f, 20f);
                Vector2 q6 = new Vector2(20f, 20f);
                Vector2 q7 = new Vector2(30f, 20f);
                Vector2 q8 = new Vector2(25f, 30f);
                Vector2 q9 = new Vector2(20f, 120f);
                Vector2 q10 = new Vector2(30f, 120f);
                Vector2 q11 = new Vector2(25f, 130f);
                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4, q5 });
                Polygon poly2 = new Polygon(new Vector2[] { q6, q7, q8 });
                Polygon poly3 = new Polygon(new Vector2[] { q9, q10, q11 });
                Everything e1 = new Everything();

                // Everything vs. anything
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)e1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)p1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p2, (IGeomPrimitive)e1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)c1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c2, (IGeomPrimitive)e1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)e1, (IGeomPrimitive)poly1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly2, (IGeomPrimitive)e1));

                // Point-point
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)p1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)new Point(new Vector2(10f, 10f))));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)p2));

                // Point-circle
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)c1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)p1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)p2, (IGeomPrimitive)c1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)p2));

                // Point-polygon
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)p1, (IGeomPrimitive)poly1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)p1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)p2, (IGeomPrimitive)poly1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)p2));

                // Circle-circle
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)c2));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c2, (IGeomPrimitive)c1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)c3));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c3, (IGeomPrimitive)c1));

                // Circle-polygon
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)c1, (IGeomPrimitive)poly1));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)c1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)c2, (IGeomPrimitive)poly2));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly2, (IGeomPrimitive)c2));

                // Polygon-polygon
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)poly2));
                Assert.IsTrue(Geometry.Intersect((IGeomPrimitive)poly2, (IGeomPrimitive)poly1));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly1, (IGeomPrimitive)poly3));
                Assert.IsFalse(Geometry.Intersect((IGeomPrimitive)poly3, (IGeomPrimitive)poly1));
            }

            /// <summary>
            /// Tests circle-circle intersections.
            /// </summary>
            [Test]
            public void TestIntersectCircleCircle()
            {
                Vector2 p1 = new Vector2(10f, 10f);
                Vector2 p2 = new Vector2(10f, 30f);
                Vector2 p3 = new Vector2(50f, 50f);

                Circle c1_0 = new Circle(p1, 0);
                Circle c2_0 = new Circle(p2, 0);
                Circle c1_10 = new Circle(p1, 10f);
                Circle c2_10 = new Circle(p2, 10f);
                Circle c2_20 = new Circle(p2, 20f);
                Circle c2_50 = new Circle(p2, 50f);
                Circle c2_10000 = new Circle(p2, 10000f);
                Circle c3_10 = new Circle(p3, 10f);

                // Dot vs. dot
                Assert.IsTrue(Geometry.Intersect(c1_0, c1_0));  // same dot
                Assert.IsFalse(Geometry.Intersect(c1_0, c2_0)); // different dot

                // Dot vs. circle
                Assert.IsFalse(Geometry.Intersect(c1_0, c3_10));   // dot outside circle
                Assert.IsTrue(Geometry.Intersect(c1_0, c2_20));    // dot on circle edge
                Assert.IsTrue(Geometry.Intersect(c1_0, c2_10000)); // dot inside circle

                // Circle vs. circle
                Assert.IsFalse(Geometry.Intersect(c1_10, c3_10));  // disjoint circles
                Assert.IsTrue(Geometry.Intersect(c1_10, c2_10));   // only edges intersect
                Assert.IsTrue(Geometry.Intersect(c2_50, c3_10));   // circle interiors intersect only partly
                Assert.IsTrue(Geometry.Intersect(c2_10, c2_50));   // one circle is strictly inside the other
                Assert.IsTrue(Geometry.Intersect(c2_20, c2_20));   // the circles are the same
            }

            /// <summary>
            /// Tests intersection of circle and rectangle.
            /// </summary>
            [Test]
            public void TestIntersectCircleRectangle()
            {
                Rectangle rect = new Rectangle(-20, -20, 20, 20);

                // Circle at rectangle corners
                Assert.IsTrue(Intersect(new Circle(new Vector2(30, 30), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(30, 30), 14), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(-30, -30), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(-30, -30), 14), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(30, -30), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(30, -30), 14), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(-30, 30), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(-30, 30), 14), rect));

                // Circle inside rectangle
                Assert.IsTrue(Intersect(new Circle(new Vector2(10, 10), 0), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(10, 10), 5), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(10, 10), 4000), rect));

                // Circle at rectangle face
                Assert.IsTrue(Intersect(new Circle(new Vector2(0, 30), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(0, 30), 9), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(0, -30), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(0, -30), 9), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(30, 0), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(30, 0), 9), rect));
                Assert.IsTrue(Intersect(new Circle(new Vector2(-30, 0), 30), rect));
                Assert.IsFalse(Intersect(new Circle(new Vector2(-30, 0), 9), rect));
            }

            /// <summary>
            /// Test intersection of rectangles.
            /// </summary>
            [Test]
            public void TestIntersectRectangleRectangle()
            {
                Vector2 p22 = new Vector2(20, 20);
                Vector2 p44 = new Vector2(40, 40);
                Vector2 p11 = new Vector2(10, 10);
                Vector2 p13 = new Vector2(10, 30);
                Vector2 p15 = new Vector2(10, 50);
                Vector2 p31 = new Vector2(30, 10);
                Vector2 p33 = new Vector2(30, 30);
                Vector2 p35 = new Vector2(30, 50);
                Vector2 p51 = new Vector2(50, 10);
                Vector2 p53 = new Vector2(50, 30);
                Vector2 p55 = new Vector2(50, 50);
                Rectangle rect = new Rectangle(p22, p44);

                Assert.IsFalse(Intersect(rect, new Rectangle(p11, p11)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p11, p13)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p11, p15)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p11, p31)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p11, p33)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p11, p35)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p11, p51)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p11, p53)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p11, p55)));

                Assert.IsFalse(Intersect(rect, new Rectangle(p13, p13)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p13, p15)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p13, p33)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p13, p35)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p13, p53)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p13, p55)));

                Assert.IsFalse(Intersect(rect, new Rectangle(p15, p15)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p15, p35)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p15, p55)));

                Assert.IsFalse(Intersect(rect, new Rectangle(p31, p31)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p31, p33)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p31, p35)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p31, p51)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p31, p53)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p31, p55)));

                Assert.IsTrue(Intersect(rect, new Rectangle(p33, p33)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p33, p35)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p33, p53)));
                Assert.IsTrue(Intersect(rect, new Rectangle(p33, p55)));

                Assert.IsFalse(Intersect(rect, new Rectangle(p35, p35)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p35, p55)));

                Assert.IsFalse(Intersect(rect, new Rectangle(p51, p51)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p51, p53)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p51, p55)));

                Assert.IsFalse(Intersect(rect, new Rectangle(p53, p53)));
                Assert.IsFalse(Intersect(rect, new Rectangle(p53, p55)));

                Assert.IsFalse(Intersect(rect, new Rectangle(p35, p55)));
            }

            /// <summary>
            /// Tests intersection of line segments with returned intersection point.
            /// </summary>
            [Test]
            public void TestIntersectLinePoint()
            {
                Vector2 p1 = new Vector2(10f, 10f);
                Vector2 p2 = new Vector2(40f, 50f);
                Vector2 p3 = new Vector2(20f, 60f);
                Vector2 p4 = new Vector2(50f, -10f);
                Vector2 p5 = new Vector2(30f, 20f);
                Vector2 p6 = p1 + (p1 - p2);
                Vector2 p7 = p2 + (p2 - p1);
                Vector2 p8 = p2 + new Vector2(1000f, -1000f);
                Vector2 p9 = p1 + new Vector2(1000f, -1000f);
                Vector2 p10 = new Vector2(-10f, 0f);
                Vector2 p11 = new Vector2(10f, 0f);
                Vector2 p12 = new Vector2(0f, -10f);
                Vector2 p13 = new Vector2(0f, 10f);
                Vector2? cross = new Vector2?();

                // General cases
                cross = p3; // to reveal possible errors in next call
                Assert.IsTrue(Geometry.Intersect(p10, p11, p12, p13, ref cross)); // orthogonal crossing
                Assert.AreEqual(cross, Vector2.Zero);
                Assert.IsTrue(Geometry.Intersect(p1, p2, p3, p4, ref cross));     // general crossing
                Assert.IsFalse(Geometry.Intersect(p1, p4, p2, p3, ref cross));    // lines cross but segments don't
                Assert.IsFalse(Geometry.Intersect(p1, p5, p3, p4, ref cross));    // line crosses segment, segments don't

                // Special cases
                Assert.IsTrue(Geometry.Intersect(p1, p2, p2, p3, ref cross));  // Endpoints connect
                Assert.AreEqual(cross, p2);
                Assert.IsTrue(Geometry.Intersect(p1, p3, p2, p3, ref cross));  // Endpoints connect
                Assert.AreEqual(cross, p3);
                Assert.IsFalse(Geometry.Intersect(p1, p2, p8, p9, ref cross)); // parallel lines far away
                Assert.IsTrue(Geometry.Intersect(p1, p2, p6, p7, ref cross)); // one line contained in another
                Assert.IsNull(cross, "Got one intersection point instead of infinitely many");
                cross = p3; // to reveal possible errors in next call
                Assert.IsTrue(Geometry.Intersect(p1, p2, p1, p2, ref cross)); // the same line segment
                Assert.IsNull(cross, "Got one intersection point instead of infinitely many");
            }

            /// <summary>
            /// Tests intersection of line segments with returned intersection type.
            /// </summary>
            [Test]
            public void TestIntersectLineType()
            {
                Vector2 p1 = new Vector2(10f, 10f);
                Vector2 p2 = new Vector2(40f, 50f);
                Vector2 p3 = new Vector2(20f, 60f);
                Vector2 p4 = new Vector2(50f, -10f);
                Vector2 p5 = new Vector2(30f, 20f);
                Vector2 p6 = p1 + (p1 - p2);
                Vector2 p7 = p2 + (p2 - p1);
                Vector2 p8 = p2 + new Vector2(1000f, -1000f);
                Vector2 p9 = p1 + new Vector2(1000f, -1000f);
                Vector2 p10 = new Vector2(-10f, 0f);
                Vector2 p11 = new Vector2(10f, 0f);
                Vector2 p12 = new Vector2(0f, -10f);
                Vector2 p13 = new Vector2(0f, 10f);
                Vector2 p14 = new Vector2(70f, 10f);
                Vector2 p15 = new Vector2(70f, 20f);
                Vector2 p16 = new Vector2(70f, 30f);
                Vector2 p17 = new Vector2(70f, 40f);

                // General cases
                Assert.AreEqual(Geometry.LineIntersectionType.Point, Geometry.Intersect(p10, p11, p12, p13)); // orthogonal crossing
                Assert.AreEqual(Geometry.LineIntersectionType.Point, Geometry.Intersect(p1, p2, p3, p4));     // general crossing
                Assert.AreEqual(Geometry.LineIntersectionType.None, Geometry.Intersect(p1, p4, p2, p3));      // lines cross but segments don't
                Assert.AreEqual(Geometry.LineIntersectionType.None, Geometry.Intersect(p1, p5, p3, p4));      // line crosses segment, segments don't

                // Special cases
                Assert.AreEqual(Geometry.LineIntersectionType.Point, Geometry.Intersect(p1, p2, p2, p3));  // Endpoints connect
                Assert.AreEqual(Geometry.LineIntersectionType.Point, Geometry.Intersect(p1, p3, p2, p3));  // Endpoints connect
                Assert.AreEqual(Geometry.LineIntersectionType.None, Geometry.Intersect(p1, p2, p8, p9)); // parallel lines far away
                Assert.AreEqual(Geometry.LineIntersectionType.Segment, Geometry.Intersect(p1, p2, p6, p7)); // one line contained in another
                Assert.AreEqual(Geometry.LineIntersectionType.Segment, Geometry.Intersect(p1, p2, p1, p2)); // the same line
                Assert.AreEqual(Geometry.LineIntersectionType.None, Geometry.Intersect(p14, p15, p16, p17)); // segments on same line, not intersecting
                Assert.AreEqual(Geometry.LineIntersectionType.Point, Geometry.Intersect(p14, p15, p15, p16)); // segments on same line, intersecting at a point
                Assert.AreEqual(Geometry.LineIntersectionType.Segment, Geometry.Intersect(p14, p16, p15, p17)); // segments on same line, intersecting at a segment
            }

            /// <summary>
            /// Tests point-polygon intersections
            /// </summary>
            [Test]
            public void TestIntersectPointPolygon()
            {
                Point p1 = new Point(new Vector2(-10f, -10f));
                Point p2 = new Point(new Vector2(10f, 20f));
                Point p3 = new Point(new Vector2(50f, 20f));
                Point p4 = new Point(new Vector2(25f, 20f));
                Point p5 = new Point(new Vector2(10f, 200f));

                Vector2 q1 = new Vector2(10f, 10f);
                Vector2 q2 = new Vector2(100f, 10f);
                Vector2 q3 = new Vector2(100f, 100f);
                Vector2 q4 = new Vector2(10f, 100f);
                Vector2 q5 = new Vector2(25f, 70f);
                Vector2 q6 = new Vector2(50f, 10f);
                Vector2 q7 = new Vector2(75f, 70f);

                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, q5, q6, q7, q2, q3, q4 });

                // Point and square; out, on boundary, in
                Assert.IsFalse(Geometry.Intersect(p1, poly1));
                Assert.IsTrue(Geometry.Intersect(p2, poly1));
                Assert.IsTrue(Geometry.Intersect(p3, poly1));
                Assert.IsTrue(Geometry.Intersect(p4, poly1));
                Assert.IsFalse(Geometry.Intersect(p5, poly1));

                // Point and concave polygon
                Assert.IsFalse(Geometry.Intersect(p1, poly2));
                Assert.IsTrue(Geometry.Intersect(p2, poly2));
                Assert.IsTrue(Geometry.Intersect(p3, poly2));
                Assert.IsFalse(Geometry.Intersect(p4, poly2));
                Assert.IsFalse(Geometry.Intersect(p5, poly2));

            }

            /// <summary>
            /// Tests circle-polygon intersections
            /// </summary>
            [Test]
            public void TestIntersectCirclePolygon()
            {
                float delta = 0.0002f; // slight margin in favour of intersection
                Circle c1 = new Circle(new Vector2(30f, 10f), 20f);
                Circle c2 = new Circle(new Vector2(10f, 0f), 5f);
                Circle c3 = new Circle(new Vector2(-90f, -90f), 20f);
                Circle c4 = new Circle(new Vector2(20f, 50f), 5f);
                Circle c5 = new Circle(new Vector2(20f, 70f), 10f + delta);
                Circle c6 = new Circle(new Vector2(15f, 100f), 10f + delta);
                Circle c7 = new Circle(new Vector2(15f, 90f), 0f + delta);
                Circle c8 = new Circle(new Vector2(0f, 60f), 5f);
                Circle c9 = new Circle(new Vector2(10f, 85f), 10f);
                Vector2 q1 = new Vector2(0f, 0f);
                Vector2 q2 = new Vector2(30f, 10f);
                Vector2 q3 = new Vector2(20f, 90f);
                Vector2 q4 = new Vector2(10f, 90f);
                Vector2 q5 = new Vector2(0f, 20f);
                Vector2 q6 = new Vector2(-10f, 70f);
                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4, q5, q6 });

                Assert.IsTrue(Geometry.Intersect(c1, poly1)); // circle centered at a vertex 
                Assert.IsTrue(Geometry.Intersect(c2, poly1)); // circle centered out, intersects
                Assert.IsFalse(Geometry.Intersect(c3, poly1)); // circle totally out
                Assert.IsTrue(Geometry.Intersect(c4, poly1)); // circle totally in
                Assert.IsTrue(Geometry.Intersect(c5, poly1)); // circle centered out, only edge intersects a vertex
                Assert.IsTrue(Geometry.Intersect(c6, poly1)); // circle centered out, only edge intersects edge
                Assert.IsTrue(Geometry.Intersect(c7, poly1)); // circle centered on edge, zero size
                Assert.IsFalse(Geometry.Intersect(c8, poly1)); // circle totally out, in polygon's "armpit"
                Assert.IsTrue(Geometry.Intersect(c9, poly1)); // circle centered in, intersects also complement
            }

            /// <summary>
            /// Tests polygon-polygon intersections
            /// </summary>
            [Test]
            public void TestIntersectPolygonPolygon()
            {
                Vector2 q1 = new Vector2(0f, 0f);
                Vector2 q2 = new Vector2(30f, 10f);
                Vector2 q3 = new Vector2(20f, 90f);
                Vector2 q4 = new Vector2(10f, 90f);
                Vector2 q5 = new Vector2(0f, 20f);
                Vector2 q6 = new Vector2(-10f, 70f);
                Vector2 w1 = new Vector2(-10f, -10f);
                Vector2 w2 = new Vector2(-20f, 0f);
                Vector2 w3 = new Vector2(20f, 20f);
                Vector2 w4 = new Vector2(60f, 50f);
                Vector2 w5 = new Vector2(30f, -20f);
                Vector2 w6 = new Vector2(25f, 40f);
                Vector2 w7 = new Vector2(20f, 70f);
                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4, q5, q6 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, w1, w2 });
                Polygon poly3 = new Polygon(new Vector2[] { w3, w4, w5 });
                Polygon poly4 = new Polygon(new Vector2[] { w3, w6, w7 });
                Polygon poly5 = new Polygon(new Vector2[] { w4, w2, w5 });

                Assert.IsTrue(Geometry.Intersect(poly1, poly2)); // only one vertex intersects
                Assert.IsTrue(Geometry.Intersect(poly1, poly3)); // vertex in polygon
                Assert.IsFalse(Geometry.Intersect(poly2, poly3)); // no intersection
                Assert.IsTrue(Geometry.Intersect(poly1, poly4)); // polygon in polygon
                Assert.IsTrue(Geometry.Intersect(poly1, poly5)); // vertices out, edge intersects polygon
            }

            /// <summary>
            /// Tests point-triangle intersections
            /// </summary>
            [Test]
            public void TestIntersectPointTriangle()
            {
                Point p1 = new Point(new Vector2(0f, 0f));
                Point p2 = new Point(new Vector2(19.9999f, 49.9999f));
                Point p3 = new Point(new Vector2(10f, 0f));
                Point p4 = new Point(new Vector2(0f, -20f));
                Vector2 v1 = new Vector2(0f, -10f);
                Vector2 v2 = new Vector2(20f, 50f);
                Vector2 v3 = new Vector2(-50f, 70f);
                Triangle t1 = new Triangle(v1, v2, v3);
                Assert.IsTrue(Intersect(p1, t1));
                Assert.IsTrue(Intersect(p2, t1));
                Assert.IsFalse(Intersect(p3, t1));
                Assert.IsFalse(Intersect(p4, t1));
            }

            /// <summary>
            /// Tests circle-triangle intersections
            /// </summary>
            [Test]
            public void TestIntersectCircleTriangle()
            {
                Vector2 p1 = new Vector2(0f, 0f);
                Vector2 p2 = new Vector2(20f, 50f);
                Vector2 p3 = new Vector2(10f, 0f);
                Vector2 p4 = new Vector2(0f, -20f);
                Vector2 v1 = new Vector2(0f, -10f);
                Vector2 v2 = new Vector2(20f, 50f);
                Vector2 v3 = new Vector2(-50f, 70f);
                Triangle t1 = new Triangle(v1, v2, v3);
                Assert.IsTrue(Intersect(new Circle(p1, 1), t1)); // circle included
                Assert.IsTrue(Intersect(new Circle(p1, 20), t1)); // neither included
                Assert.IsTrue(Intersect(new Circle(p1, 2000), t1)); // triangle included
                Assert.IsTrue(Intersect(new Circle(p2, 0), t1));
                Assert.IsTrue(Intersect(new Circle(p2, 20), t1));
                Assert.IsFalse(Intersect(new Circle(p3, 5), t1));
                Assert.IsTrue(Intersect(new Circle(p3, 10), t1));
                Assert.IsFalse(Intersect(new Circle(p4, 9.9999f), t1)); // barely out
                Assert.IsTrue(Intersect(new Circle(p4, 10), t1)); // borders intersect
                Assert.IsTrue(Intersect(new Circle(p4, 10.0001f), t1)); // barely in
            }

            /// <summary>
            /// Tests the general distance interface.
            /// </summary>
            [Test]
            public void TestGeneralDistance()
            {
                IGeomPrimitive[] prims = {
                    new Everything(),
                    new Point(new Vector2(10, 20)),
                    new Circle(new Vector2(30, 40), 50),
                    new Rectangle(60, 70, 80, 90),
                    new Triangle(new Vector2(10, 60), new Vector2(-90, -10), new Vector2(-30, 20)),
                    new Polygon(new Vector2[] { new Vector2(15, 25), new Vector2(35, 45), new Vector2(55, 65) }),
                };
                foreach (IGeomPrimitive prim1 in prims)
                    foreach (IGeomPrimitive prim2 in prims)
                        Assert.LessOrEqual(0, Distance(prim1, prim2));
            }

            /// <summary>
            /// Tests point to line segment distance.
            /// </summary>
            [Test]
            public void TestDistancePointSegment()
            {
                Vector2 q1 = new Vector2(10, 10);
                Vector2 q2 = new Vector2(20, 30);
                Vector2 q3 = new Vector2(50, 30);
                Vector2 q4 = new Vector2(20, -30);
                Point p1 = new Point(new Vector2(10, 10));
                Point p2 = new Point(new Vector2(20, 30));
                Point p3 = new Point(new Vector2(15, 20));
                Point p4 = new Point(new Vector2(20, 10));
                Point p5 = new Point(new Vector2(10, 30));
                Point p6 = new Point(new Vector2(10, 40));
                Point p7 = new Point(new Vector2(0, 0));
                Point p8 = new Point(new Vector2(40, -10));
                Point p9 = new Point(new Vector2(20, 1000));
                Point p10 = new Point(new Vector2(1000, 30));
                float delta = 0.0001f; // amount of acceptable error

                // Points on line segment
                Assert.AreEqual(Distance(p1, q1, q2), 0f, delta); // endpoint
                Assert.AreEqual(Distance(p2, q1, q2), 0f, delta); // endpoint
                Assert.AreEqual(Distance(p3, q1, q2), 0f, delta); // middle point

                // Point out of line segment, projects inside line segment
                Assert.AreEqual(Distance(p4, q1, q2), 10 * 2 / Math.Sqrt(5), delta);
                Assert.AreEqual(Distance(p5, q1, q2), 10 * 2 / Math.Sqrt(5), delta);

                // Point out of line segment, projects out of line segment
                Assert.AreEqual(Distance(p6, q1, q2), 10 * Math.Sqrt(2), delta);
                Assert.AreEqual(Distance(p7, q1, q2), 10 * Math.Sqrt(2), delta);

                // Point out of line segment, projects on an endpoint
                Assert.AreEqual(Distance(p4, q2, q3), 20, delta);
                Assert.AreEqual(Distance(p9, q2, q3), 970, delta);
                Assert.AreEqual(Distance(p5, q2, q4), 10, delta);
                Assert.AreEqual(Distance(p10, q2, q4), 980, delta);

                // Vertical line segment, point out of line segment, projects to line segment
                Assert.AreEqual(Distance(p8, q2, q3), 40, delta);

                // Horizontal line segment, point out of line segment, projects to line segment
                Assert.AreEqual(Distance(p7, q2, q4), 20, delta);
            }

            /// <summary>
            /// Tests point to circle distance.
            /// </summary>
            [Test]
            public void TestDistancePointCircle()
            {
                Point p1 = new Point(new Vector2(-9, 10));
                Point p2 = new Point(new Vector2(10, 30));
                Point p3 = new Point(new Vector2(11, 30));
                Point p4 = new Point(new Vector2(-990, -2990));
                Circle c1 = new Circle(new Vector2(10, 10), 20);
                float delta = 0.0001f; // amount of acceptable error
                Assert.AreEqual(0, Distance(p1, c1), delta); // interior
                Assert.AreEqual(0, Distance(p2, c1), delta); // edge
                Assert.AreEqual(Math.Sqrt(1 * 1 + 20 * 20) - 20, Distance(p3, c1), delta); // exterior near
                Assert.AreEqual(Math.Sqrt(1000 * 1000 + 3000 * 3000) - 20, Distance(p4, c1), delta); // exterior far
            }

            /// <summary>
            /// Tests point to rectangle distance.
            /// </summary>
            [Test]
            public void TestDistancePointRectangle()
            {
                Point p1 = new Point(new Vector2(0, 0));
                Point p2 = new Point(new Vector2(-20, 20));
                Point p3 = new Point(new Vector2(30, -5));
                Point p4 = new Point(new Vector2(40, 30));
                Point p5 = new Point(new Vector2(0, -5001));
                Rectangle r1 = new Rectangle(-20, -10, 30, 20);
                float delta = 0.0001f; // amount of acceptable error
                Assert.AreEqual(0, Distance(p1, r1), delta); // interior
                Assert.AreEqual(0, Distance(p2, r1), delta); // vertex
                Assert.AreEqual(0, Distance(p3, r1), delta); // edge
                Assert.AreEqual(10 * Math.Sqrt(2), Distance(p4, r1), delta); // closest to vertex
                Assert.AreEqual(4991, Distance(p5, r1), delta); // closest to edge
            }

            /// <summary>
            /// Tests point to triangle distance.
            /// </summary>
            [Test]
            public void TestDistancePointTriangle()
            {
                Vector2 q1 = new Vector2(30, 30);
                Vector2 q2 = new Vector2(-20, 20);
                Vector2 q3 = new Vector2(10, -30);
                Point p1 = new Point(new Vector2(10, 10));
                Point p2 = new Point(new Vector2(30, 30));
                Point p3 = new Point(new Vector2(5, 25));
                Point p4 = new Point(new Vector2(50, 50));
                Point p5 = new Point(new Vector2(5 - 10, 25 + 50));
                Triangle t1 = new Triangle(q1, q2, q3);
                float delta = 0.0001f; // amount of acceptable error

                // Point in triangle
                Assert.AreEqual(0, Distance(p1, t1), delta); // interior
                Assert.AreEqual(0, Distance(p2, t1), delta); // vertex
                Assert.AreEqual(0, Distance(p3, t1), delta); // edge

                // Point outside triangle
                Assert.AreEqual(20 * Math.Sqrt(2), Distance(p4, t1), delta); // closest to vertex
                Assert.AreEqual(Math.Sqrt(10 * 10 + 50 * 50), Distance(p5, t1), delta); // closest to edge
            }

            /// <summary>
            /// Tests closest point of a line segment with respect to a point.
            /// </summary>
            [Test]
            public void TestGetClosestPointPointSegment()
            {
                Vector2 q1 = new Vector2(10, 10);
                Vector2 q2 = new Vector2(20, 30);
                Vector2 q3 = new Vector2(50, 30);
                Vector2 q4 = new Vector2(20, -30);
                Point p1 = new Point(new Vector2(10, 10));
                Point p2 = new Point(new Vector2(20, 30));
                Point p3 = new Point(new Vector2(15, 20));
                Point p4 = new Point(new Vector2(20, 10));
                Point p5 = new Point(new Vector2(10, 30));
                Point p6 = new Point(new Vector2(10, 40));
                Point p7 = new Point(new Vector2(0, 0));
                Point p8 = new Point(new Vector2(40, -10));
                Point p9 = new Point(new Vector2(20, 1000));
                Point p10 = new Point(new Vector2(1000, 30));
                Point r1 = new Point(new Vector2(12, 14));
                Point r2 = new Point(new Vector2(18, 26));
                Point r3 = new Point(new Vector2(40, 30));
                Point r4 = new Point(new Vector2(20, 0));

                // Points on line segment
                Assert.AreEqual(GetClosestPoint(q1, q2, p1), p1); // endpoint
                Assert.AreEqual(GetClosestPoint(q1, q2, p2), p2); // endpoint
                Assert.AreEqual(GetClosestPoint(q1, q2, p3), p3); // middle point

                // Point out of line segment, projects inside line segment
                Assert.AreEqual(GetClosestPoint(q1, q2, p4), r1);
                Assert.AreEqual(GetClosestPoint(q1, q2, p5), r2);

                // Point out of line segment, projects out of line segment
                Assert.AreEqual(GetClosestPoint(q1, q2, p6), p2);
                Assert.AreEqual(GetClosestPoint(q1, q2, p7), p1);

                // Point out of line segment, projects on an endpoint
                Assert.AreEqual(GetClosestPoint(q2, q3, p4), p2);
                Assert.AreEqual(GetClosestPoint(q2, q3, p9), p2);
                Assert.AreEqual(GetClosestPoint(q2, q4, p5), p2);
                Assert.AreEqual(GetClosestPoint(q2, q4, p10), p2);

                // Vertical line segment, point out of line segment, projects to line segment
                Assert.AreEqual(GetClosestPoint(q2, q3, p8), r3);

                // Horizontal line segment, point out of line segment, projects to line segment
                Assert.AreEqual(GetClosestPoint(q2, q4, p7), r4);
            }

            /// <summary>
            /// Tests point to polygon distance.
            /// </summary>
            [Test]
            public void TestDistancePointPolygon()
            {
                Vector2 q1 = new Vector2(10f, 10f);
                Vector2 q2 = new Vector2(100f, 10f);
                Vector2 q3 = new Vector2(100f, 100f);
                Vector2 q4 = new Vector2(10f, 100f);
                Vector2 q5 = new Vector2(35f, 35f);
                Vector2 q6 = new Vector2(60f, 10f);

                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, q5, q6, q2, q3, q4 });

                Point p1 = new Point(new Vector2(50, 50));
                Point p2 = new Point(new Vector2(100, 100));
                Point p3 = new Point(new Vector2(10, 20));
                Point p4 = new Point(new Vector2(0, 0));
                Point p5 = new Point(new Vector2(0, 120));
                Point p6 = new Point(new Vector2(130, 100));
                Point p7 = new Point(new Vector2(120, -20));
                Point p8 = new Point(new Vector2(70, 0));
                Point p9 = new Point(new Vector2(35, -15));
                Point p10 = new Point(new Vector2(35, 30));

                float delta = 0.0001f; // acceptable error margin

                // Point in polygon
                Assert.AreEqual(Distance(p1, poly1), 0, delta); // inside
                Assert.AreEqual(Distance(p2, poly1), 0, delta); // on vertex
                Assert.AreEqual(Distance(p3, poly1), 0, delta); // on edge

                // Point out of polygon, convex polygon
                Assert.AreEqual(Distance(p4, poly1), 10 * Math.Sqrt(2), delta); // closest point is a vertex
                Assert.AreEqual(Distance(p5, poly1), (new Vector2(-10, 20)).Length(), delta); // ditto
                Assert.AreEqual(Distance(p6, poly1), 30, delta); // ditto
                Assert.AreEqual(Distance(p7, poly1), (new Vector2(20, -30)).Length(), delta); // ditto
                Assert.AreEqual(Distance(p8, poly1), 10, delta); // closest point is on edge

                // Point out of polygon, concave polygon
                Assert.AreEqual(Distance(p9, poly2), 25 * Math.Sqrt(2), delta); // ambiguous normal from two vertices
                Assert.AreEqual(Distance(p10, poly2), 5.0 / 2.0 * Math.Sqrt(2), delta); // ambiguous normal from two edges
            }

            /// <summary>
            /// Tests distance between two circles.
            /// </summary>
            [Test]
            public void TestDistanceCircleCircle()
            {
                Circle c1 = new Circle(new Vector2(10, 10), 20);
                Circle c2 = new Circle(new Vector2(10, 10), 10);
                Circle c3 = new Circle(new Vector2(30, 10), 10);
                Circle c4 = new Circle(new Vector2(40, 10), 10);
                Circle c5 = new Circle(new Vector2(50, 10), 10);
                Circle c6 = new Circle(new Vector2(10, 5130), 100);
                float delta = 0.0001f; // amount of acceptable error
                Assert.AreEqual(0, Distance(c1, c1), delta); // same circle
                Assert.AreEqual(0, Distance(c1, c2), delta); // inside
                Assert.AreEqual(0, Distance(c1, c3), delta); // intersects
                Assert.AreEqual(0, Distance(c1, c4), delta); // edge intersects
                Assert.AreEqual(10, Distance(c1, c5), delta); // outside
                Assert.AreEqual(5000, Distance(c1, c6), delta); // far outside
            }

            /// <summary>
            /// Tests circle to rectangle distance.
            /// </summary>
            [Test]
            public void TestDistanceCircleRectangle()
            {
                Circle c1 = new Circle(new Vector2(0, 0), 10);
                Circle c2 = new Circle(new Vector2(-20, 20), 10);
                Circle c3 = new Circle(new Vector2(30, -5), 10);
                Circle c4 = new Circle(new Vector2(40, 30), 10);
                Circle c5 = new Circle(new Vector2(0, -5001), 10);
                Circle c6 = new Circle(new Vector2(0, 0), 1000);
                Rectangle r1 = new Rectangle(-20, -10, 30, 20);
                float delta = 0.0001f; // amount of acceptable error
                Assert.AreEqual(0, Distance(c1, r1), delta); // interior
                Assert.AreEqual(0, Distance(c2, r1), delta); // vertex
                Assert.AreEqual(0, Distance(c3, r1), delta); // edge
                Assert.AreEqual(10 * Math.Sqrt(2) - 10, Distance(c4, r1), delta); // closest to vertex
                Assert.AreEqual(4991 - 10, Distance(c5, r1), delta); // closest to edge
                Assert.AreEqual(0, Distance(c6, r1), delta); // rectangle in circle
            }

            /// <summary>
            /// Tests circle to triangle distance.
            /// </summary>
            [Test]
            public void TestDistanceCircleTriangle()
            {
                Vector2 q1 = new Vector2(30, 30);
                Vector2 q2 = new Vector2(-20, 20);
                Vector2 q3 = new Vector2(10, -30);
                Circle c1 = new Circle(new Vector2(10, 10), 10);
                Circle c2 = new Circle(new Vector2(30, 30), 10);
                Circle c3 = new Circle(new Vector2(5, 25), 10);
                Circle c4 = new Circle(new Vector2(50, 50), 10);
                Circle c5 = new Circle(new Vector2(5 - 10, 25 + 50), 10);
                Circle c6 = new Circle(new Vector2(0, 0), 1000);
                Circle c7 = new Circle(new Vector2(40, 30), 10);
                Triangle t1 = new Triangle(q1, q2, q3);
                float delta = 0.0001f; // amount of acceptable error

                // Center in triangle
                Assert.AreEqual(0, Distance(c1, t1), delta); // interior
                Assert.AreEqual(0, Distance(c2, t1), delta); // vertex
                Assert.AreEqual(0, Distance(c3, t1), delta); // edge

                // Center outside triangle
                Assert.AreEqual(20 * Math.Sqrt(2) - 10, Distance(c4, t1), delta); // closest to vertex
                Assert.AreEqual(Math.Sqrt(10 * 10 + 50 * 50) - 10, Distance(c5, t1), delta); // closest to edge
                Assert.AreEqual(0, Distance(c6, t1), delta); // rectangle in circle
                Assert.AreEqual(0, Distance(c7, t1), delta); // edge intersect
            }

            /// <summary>
            /// Tests getting a normal for a polygon.
            /// </summary>
            [Test]
            public void TestGetNormalPolygon()
            {
                Vector2 q1 = new Vector2(10f, 10f);
                Vector2 q2 = new Vector2(100f, 10f);
                Vector2 q3 = new Vector2(100f, 100f);
                Vector2 q4 = new Vector2(10f, 100f);
                Vector2 q5 = new Vector2(35f, 35f);
                Vector2 q6 = new Vector2(60f, 10f);

                Polygon poly1 = new Polygon(new Vector2[] { q1, q2, q3, q4 });
                Polygon poly2 = new Polygon(new Vector2[] { q1, q5, q6, q2, q3, q4 });

                Point p1 = new Point(new Vector2(50, 50));
                Point p2 = new Point(new Vector2(100, 100));
                Point p3 = new Point(new Vector2(10, 20));
                Point p4 = new Point(new Vector2(0, 0));
                Point p5 = new Point(new Vector2(0, 120));
                Point p6 = new Point(new Vector2(130, 100));
                Point p7 = new Point(new Vector2(120, -20));
                Point p8 = new Point(new Vector2(70, 0));
                Point p9 = new Point(new Vector2(35, -15));
                Point p10 = new Point(new Vector2(35, 30));

                // Point in polygon
                Assert.AreEqual(GetNormal(poly1, p1), Vector2.Zero); // inside
                Assert.AreEqual(GetNormal(poly1, p2), Vector2.Zero); // on vertex
                Assert.AreEqual(GetNormal(poly1, p3), Vector2.Zero); // on edge

                // Point out of polygon, convex polygon
                Assert.AreEqual(GetNormal(poly1, p4), Vector2.Normalize(-Vector2.One)); // closest point is a vertex
                Assert.AreEqual(GetNormal(poly1, p5), Vector2.Normalize(new Vector2(-10, 20))); // ditto
                Assert.AreEqual(GetNormal(poly1, p6), Vector2.UnitX); // ditto
                Assert.AreEqual(GetNormal(poly1, p7), Vector2.Normalize(new Vector2(20, -30))); // ditto
                Assert.AreEqual(GetNormal(poly1, p8), -Vector2.UnitY); // closest point is on edge

                // Point out of polygon, concave polygon
                Assert.IsTrue(GetNormal(poly2, p9).Equals(Vector2.Normalize(-Vector2.One)) ||
                              GetNormal(poly2, p9).Equals(Vector2.Normalize(new Vector2(1, -1)))); // ambiguous normal from two vertices
                Assert.IsTrue(GetNormal(poly2, p10).Equals(Vector2.Normalize(-Vector2.One)) ||
                              GetNormal(poly2, p10).Equals(Vector2.Normalize(new Vector2(1, -1)))); // ambiguous normal from two edges
            }

            /// <summary>
            /// Tests translation of cartesian coordinates into barycentric coordinates.
            /// </summary>
            [Test]
            public void TestBarycentric()
            {
                Vector2 v1 = new Vector2(10f, 10f);
                Vector2 v2 = new Vector2(50f, 10f);
                Vector2 v3 = new Vector2(10f, 50f);
                Vector2 v4 = new Vector2(70f, 10f);
                Vector2 p1 = new Vector2(20f, 20f);
                float amount2, amount3;

                // Coordinates at triangle vertices.
                CartesianToBarycentric(v1, v2, v3, v1, out amount2, out amount3);
                Assert.AreEqual(amount2, 0f);
                Assert.AreEqual(amount3, 0f);
                CartesianToBarycentric(v1, v2, v3, v2, out amount2, out amount3);
                Assert.AreEqual(amount2, 1f);
                Assert.AreEqual(amount3, 0f);
                CartesianToBarycentric(v1, v2, v3, v3, out amount2, out amount3);
                Assert.AreEqual(amount2, 0);
                Assert.AreEqual(amount3, 1f);

                // Coordinates inside the triangle.
                CartesianToBarycentric(v1, v2, v3, p1, out amount2, out amount3);
                Assert.Greater(amount2, 0f);
                Assert.Less(amount2, 1f);
                Assert.Greater(amount3, 0f);
                Assert.Less(amount3, 1f);

                // Reduced triangle. There's no asserts as return values are undefined,
                // but the code shouldn't crash.
                CartesianToBarycentric(v1, v2, v4, v1, out amount2, out amount3);
                CartesianToBarycentric(v1, v2, v4, v2, out amount2, out amount3);
                CartesianToBarycentric(v1, v2, v4, v3, out amount2, out amount3);
                CartesianToBarycentric(v1, v2, v4, p1, out amount2, out amount3);

            }

        }
#endif
        #endregion Unit tests
    }
}

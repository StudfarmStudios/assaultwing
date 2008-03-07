// marked in as DEBUG because we don't want NUnit framework to release builds
#if DEBUG
using NUnit.Framework;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IndexPair = System.Collections.Generic.KeyValuePair<Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework.Vector3>;

namespace AW2.Helpers
{
    /// <summary>
    /// An equality comparer for the Graphics3D.GetOutline method.
    /// </summary>
    /// <see cref="Graphics3D.GetOutline(Model)"/>
    /// <see cref="Graphics3D.GetOutline(VertexPositionNormalTexture[], short[])"/>
    internal class IndexPairEqualityComparer : IEqualityComparer<IndexPair>
    {
        #region IEqualityComparer<IndexPair> Members

        /// <summary>
        /// Two index pairs are equal if they represent the same vertex location,
        /// regardless of order.
        /// </summary>
        /// <param name="x">One index pair.</param>
        /// <param name="y">Another index pair.</param>
        /// <returns>True iff the two index pairs are equal.</returns>
        public bool Equals(IndexPair x, IndexPair y)
        {
            return (x.Key.Equals(y.Key) && x.Value.Equals(y.Value))
                || (x.Key.Equals(y.Value) && x.Value.Equals(y.Key));
        }

        public int GetHashCode(IndexPair obj)
        {
            return obj.Key.GetHashCode() ^ obj.Value.GetHashCode();
        }

        #endregion
    }
    
    /// <summary>
    /// Describes a custom vertex format structure that contains position, texture coordinates and normal data. 
    /// </summary>
    /// <see cref="Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture"/>
    [System.Diagnostics.DebuggerDisplay("pos = {position} tex = {texture} norm = {normal}")]
    public struct VertexPositionTextureNormal
    {
        Vector3 position;
        Vector2 textureCoordinate;
        Vector3 normal;

        /// <summary>
        /// The vertex position.
        /// </summary>
        public Vector3 Position { get { return position; } set { position = value; } }
        
        /// <summary>
        /// The texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate { get { return textureCoordinate; } set { textureCoordinate = value; } }
        
        /// <summary>
        /// The vertex normal.
        /// </summary>
        public Vector3 Normal { get { return normal; } set { normal = value; } }
    }

    /// <summary>
    /// Contains helper methods for 3D graphics.
    /// </summary>
    public partial class Graphics3D
    {
        #region Type definitions

        /// <summary>
        /// Type of winding for the triangles of a 3D model when looked at
        /// from a certain camera point.
        /// </summary>
        [Flags]
        public enum TriangleWinding
        {

            /// <summary>
            /// There are no triangles in the model.
            /// </summary>
            None = 0,

            /// <summary>
            /// All triangles wind clockwise.
            /// </summary>
            Clockwise = 1,

            /// <summary>
            /// All triangles wind counterclockwise.
            /// </summary>
            CounterClockwise = 2,

            /// <summary>
            /// Some triangles wind clockwise and some wind counterclockwise.
            /// </summary>
            Mixed = Clockwise | CounterClockwise,
        }

        #endregion Type definitions

        #region Methods for exporting raw data from 3D models and importing it back

        /// <summary>
        /// Extracts vertex and triangle information out of a 3D model.
        /// </summary>
        /// The triangle data is stored as a triangle list.
        /// <param name="model">The 3D model.</param>
        /// <param name="vertexData">Where to store the vertex data.</param>
        /// <param name="indexData">Where to store the triangle data.</param>
        private static void GetModelData(Model model, out VertexPositionNormalTexture[] vertexData,
            out short[] indexData)
        {
            // TODO: Get rid of our assumption that the model has only one mesh.
            GetMeshData(model.Meshes[0], out vertexData, out indexData);
        }
        
        /// <summary>
        /// Extracts vertex and triangle information out of a 3D mesh.
        /// </summary>
        /// The triangle data is stored as a triangle list.
        /// <param name="mesh">The 3D mesh.</param>
        /// <param name="vertexData">Where to store the vertex data.</param>
        /// <param name="indexData">Where to store the triangle data.</param>
        private static void GetMeshData(ModelMesh mesh, out VertexPositionNormalTexture[] vertexData,
            out short[] indexData)
        {
            // TODO: Get rid of our assumption that the mesh has only one part.
            VertexBuffer vertexBuffer = mesh.VertexBuffer;
            IndexBuffer indexBuffer = mesh.IndexBuffer;
            int vertexCount = mesh.MeshParts[0].NumVertices;
            int indexCount = mesh.MeshParts[0].PrimitiveCount;
            vertexData = new VertexPositionNormalTexture[vertexCount];
            indexData = new short[indexCount * 3];
            vertexBuffer.GetData<VertexPositionNormalTexture>(vertexData);
            indexBuffer.GetData<short>(indexData);
            Matrix worldMatrix = Matrix.Identity;
            for (ModelBone bone = mesh.ParentBone; bone != null; bone = bone.Parent)
                worldMatrix *= bone.Transform;
            for (int i = 0; i < vertexData.Length; ++i)
                vertexData[i].Position = Vector3.Transform(vertexData[i].Position, worldMatrix);
        }

        /// <summary>
        /// Extracts from a 3D model necessary vertex and line segment data for a
        /// wireframe model.
        /// </summary>
        /// The line segment data is stored as a line list.
        /// <param name="model">The 3D model.</param>
        /// <param name="color">The color for the wireframe model.</param>
        /// <param name="wireVertexData">Where to store the wireframe vertex data.</param>
        /// <param name="wireIndexData">Where to store the wireframe line segment data.</param>
        public static void GetWireframeModelData(Model model, Color color,
            out VertexPositionColor[] wireVertexData, out short[] wireIndexData)
        {
            VertexPositionNormalTexture[] vertexData;
            short[] indexData;
            GetModelData(model, out vertexData, out indexData);
            GetWireframeModelData(vertexData, indexData, color, out wireVertexData, out wireIndexData);
        }

        /// <summary>
        /// Extracts from a 3D model necessary vertex and line segment data for a
        /// wireframe model.
        /// </summary>
        /// The line segment data is stored as a line list.
        /// <param name="vertexData">Vertex data of the 3D model.</param>
        /// <param name="indexData">Index data of the 3D model.</param>
        /// <param name="color">The color for the wireframe model.</param>
        /// <param name="wireVertexData">Where to store the wireframe vertex data.</param>
        /// <param name="wireIndexData">Where to store the wireframe line segment data.</param>
        public static void GetWireframeModelData(VertexPositionNormalTexture[] vertexData, short[] indexData, Color color,
            out VertexPositionColor[] wireVertexData, out short[] wireIndexData)
        {
            wireVertexData = Array.ConvertAll<VertexPositionNormalTexture, VertexPositionColor>(
                vertexData, delegate(VertexPositionNormalTexture v)
            {
                return new VertexPositionColor(v.Position, color);
            });
            wireIndexData = new short[2 * indexData.Length];
            for (int i = 0; i + 2 < indexData.Length; i += 3)
            {
                wireIndexData[2 * i + 0] = indexData[i + 0];
                wireIndexData[2 * i + 1] = indexData[i + 1];
                wireIndexData[2 * i + 2] = indexData[i + 1];
                wireIndexData[2 * i + 3] = indexData[i + 2];
                wireIndexData[2 * i + 4] = indexData[i + 2];
                wireIndexData[2 * i + 5] = indexData[i + 0];
            }
        }

        /// <summary>
        /// Extracts from a polygon necessary vertex data for a wireframe model.
        /// </summary>
        /// The returned vertex data is suitable for drawing a line strip.
        /// <param name="poly">The polygon.</param>
        /// <param name="z">The Z coordinate for all vertices of the wireframe model.</param>
        /// <param name="color">The color of the wireframe model.</param>
        /// <param name="vertexData">Where to store the vertex data.</param>
        public static void GetWireframeModelData(Polygon poly, float z, Color color,
            ref VertexPositionColor[] vertexData)
        {
            int vertexDataLength = poly.Vertices.Length + 1;
            if (vertexData == null || vertexData.Length != vertexDataLength)
                vertexData = new VertexPositionColor[vertexDataLength];
            for (int i = 0; i < poly.Vertices.Length; ++i)
                vertexData[i] = new VertexPositionColor(new Vector3(poly.Vertices[i], z), color);
            vertexData[poly.Vertices.Length] = vertexData[0];
        }

        /// <summary>
        /// Extracts from a sphere necessary vertex data for a wireframe model.
        /// </summary>
        /// The returned vertex data is suitable for drawing a line strip.
        /// The model is a projection of the sphere to the X-Y-plane.
        /// <param name="sphere">The sphere.</param>
        /// <param name="z">The Z coordinate for all vertices of the wireframe model.</param>
        /// <param name="color">The color of the wireframe model.</param>
        /// <param name="vertexData">Where to store the vertex data.</param>
        public static void GetWireframeModelData(BoundingSphere sphere, float z, Color color,
            out VertexPositionColor[] vertexData)
        {
            int vertexCount = (int)(sphere.Radius * MathHelper.TwoPi / 10.0);
            vertexCount = Math.Max(3, vertexCount);
            vertexCount = Math.Min(vertexCount, 1000);
            vertexData = new VertexPositionColor[vertexCount + 1];
            for (int i = 0; i <= vertexCount; ++i)
            {
                float angle = (float)(MathHelper.TwoPi * i / (float)vertexCount);
                Matrix rotation = Matrix.CreateRotationZ(angle);
                Vector3 pos = new Vector3(sphere.Center.X, sphere.Center.Y, z)
                    + Vector3.Transform(sphere.Radius * Vector3.UnitX, rotation);
                vertexData[i] = new VertexPositionColor(pos, color);
            }
        }

        /// <summary>
        /// Extracts from a box necessary vertex data for a wireframe model.
        /// </summary>
        /// The returned vertex data is suitable for drawing a line strip.
        /// The model is a projection of the box to the X-Y-plane.
        /// <param name="box">The box.</param>
        /// <param name="z">The Z coordinate for all vertices of the wireframe model.</param>
        /// <param name="color">The color of the wireframe model.</param>
        /// <param name="vertexData">Where to store the vertex data.</param>
        public static void GetWireframeModelData(BoundingBox box, float z, Color color,
            out VertexPositionColor[] vertexData)
        {
            vertexData = new VertexPositionColor[5] {
               new VertexPositionColor(new Vector3(box.Min.X, box.Min.Y, z), color),
               new VertexPositionColor(new Vector3(box.Min.X, box.Max.Y, z), color),
               new VertexPositionColor(new Vector3(box.Max.X, box.Max.Y, z), color),
               new VertexPositionColor(new Vector3(box.Max.X, box.Min.Y, z), color),
               new VertexPositionColor(new Vector3(box.Min.X, box.Min.Y, z), color),
            };
        }

        #endregion Methods for exporting raw data from 3D models and importing it back

        #region Methods for exporting other objects from 3D models

        /// <summary>
        /// Finds out a polygonal outline for a 3D model.
        /// </summary>
        /// As the return value is only a simple polygon (and because of implementation
        /// details), the interior of the input should be simply connected, i.e. the
        /// model must define one piece without holes and without two pieces connected
        /// by a single vertex. Otherwise the return value is undefined (but will still
        /// try to be nice).
        /// <param name="model">The 3D model.</param>
        /// <returns>A polygonal outline for the 3D model.</returns>
        public static Polygon GetOutline(Model model)
        {
            VertexPositionNormalTexture[] vertexData;
            short[] indexData;
            GetModelData(model, out vertexData, out indexData);
            return GetOutline(vertexData, indexData);
        }

        /// <summary>
        /// Finds out a polygonal outline for a 3D mesh.
        /// </summary>
        /// As the return value is only a simple polygon (and because of implementation
        /// details), the interior of the input should be simply connected, i.e. the
        /// model must define one piece without holes and without two pieces connected
        /// by a single vertex. Otherwise the return value is undefined (but will still
        /// try to be nice).
        /// <param name="mesh">The 3D mesh.</param>
        /// <returns>A polygonal outline for the 3D mesh.</returns>
        public static Polygon GetOutline(ModelMesh mesh)
        {
            VertexPositionNormalTexture[] vertexData;
            short[] indexData;
            GetMeshData(mesh, out vertexData, out indexData);
            return GetOutline(vertexData, indexData);
        }

        /// <summary>
        /// Finds out a polygonal outline for a 3D model.
        /// </summary>
        /// As the return value is only a simple polygon (and because of implementation
        /// details), the interior of the input should be simply connected, i.e. the
        /// model must define one piece without holes and without two pieces connected
        /// by a single vertex. Otherwise the return value is undefined (but will still
        /// try to be nice).
        /// <param name="vertexData">Vertices of the 3D model.</param>
        /// <param name="indexData">Triangles of the 3D model.</param>
        /// <returns>A polygonal outline for the 3D model.</returns>
        public static Polygon GetOutline(VertexPositionNormalTexture[] vertexData, short[] indexData)
        {
            // The algorithm counts the number of times each face is used in a triangle.
            // Faces that are used only once must be on the boundary of the model.
            // We refer to vertices by their coordinates and not by their index
            // in vertexData because 3DSMax has the habit of saving the same vertex
            // multiple times, each time with a different normal (understandable, yes).
            IndexPairEqualityComparer comparer = new IndexPairEqualityComparer();
            Dictionary<IndexPair, int> faceUseCounts = new Dictionary<KeyValuePair<Vector3, Vector3>, int>(indexData.Length, comparer);
            for (int index = 0; index + 2 < indexData.Length; index += 3)
                for (int vertI = 0; vertI < 3; ++vertI)
                {
                    Vector3 pos1 = vertexData[indexData[index + vertI]].Position;
                    Vector3 pos2 = vertexData[indexData[index + (vertI + 1) % 3]].Position;
                    IndexPair face = new IndexPair(pos1, pos2);
                    if (!faceUseCounts.ContainsKey(face))
                        faceUseCounts.Add(face, 0);
                    ++faceUseCounts[face];
                }

            List<Vector2> polyVertices = new List<Vector2>();
            
            // Find first polygon vertex.
            Vector3 firstIndex = new Vector3(Single.NaN, Single.NaN, Single.NaN);
            Vector3 prevIndex = new Vector3(Single.NaN, Single.NaN, Single.NaN);
            Vector3 nextIndex = new Vector3(Single.NaN, Single.NaN, Single.NaN);
            foreach (KeyValuePair<IndexPair, int> faceUseCount in faceUseCounts)
                if (faceUseCount.Value == 1)
                {
                    prevIndex = firstIndex = faceUseCount.Key.Key;
                    nextIndex = faceUseCount.Key.Value;
                    polyVertices.Add(new Vector2(firstIndex.X, firstIndex.Y));
                    break;
                }

            // Find the remaining polygon vertices.
            while (!nextIndex.Equals(firstIndex))
            {
                bool foundNext = false;
                foreach (KeyValuePair<IndexPair, int> faceUseCount in faceUseCounts)
                    if (faceUseCount.Value == 1 && faceUseCount.Key.Key.Equals(nextIndex)
                        && !faceUseCount.Key.Value.Equals(prevIndex))
                    {
                        polyVertices.Add(new Vector2(nextIndex.X, nextIndex.Y));
                        prevIndex = nextIndex;
                        nextIndex = faceUseCount.Key.Value;
                        foundNext = true;
                        break;
                    }
                    else if (faceUseCount.Value == 1 && faceUseCount.Key.Value.Equals(nextIndex)
                        && !faceUseCount.Key.Key.Equals(prevIndex))
                {
                    polyVertices.Add(new Vector2(nextIndex.X, nextIndex.Y));
                    prevIndex = nextIndex;
                    nextIndex = faceUseCount.Key.Key;
                    foundNext = true;
                    break;
                }
                if (!foundNext)
                    throw new ArgumentException("Unable to find outline for 3D model");
            }

            // HACK: Use exceptions to pass raw data for debug.
            try
            {
                Polygon poly = new Polygon(polyVertices.ToArray());
                return poly;
            }
            catch (Exception e)
            {
                e.Data.Add("debug", polyVertices);
                throw e;
            }

            // NON-HACK
            //Polygon poly = new Polygon(polyVertices.ToArray());
            //return poly;
        }

        /// <summary>
        /// Returns the winding direction of a triangle.
        /// </summary>
        /// <param name="v1">The first vertex of the triangle.</param>
        /// <param name="v2">The second vertex of the triangle.</param>
        /// <param name="v3">The third vertex of the triangle.</param>
        /// <returns>The triangle's winding direction.</returns>
        public static TriangleWinding GetTriangleWinding(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            switch (Geometry.Stand(v3, v1, v2))
            {
                case Geometry.StandType.Left: return TriangleWinding.CounterClockwise;
                case Geometry.StandType.Right: return TriangleWinding.Clockwise;
                case Geometry.StandType.Edge: return TriangleWinding.None; // reduced triangle
                default: return TriangleWinding.None;
            }
        }

        /// <summary>
        /// Returns the triangle winding of a 3D model when projected to the X-Y-plane.
        /// </summary>
        /// <param name="vertexData">The vertices of the 3D model.</param>
        /// <param name="indexData">The triangles of the 3D model.</param>
        /// <returns>The triangle winding of the 3D model.</returns>
        public static TriangleWinding GetTriangleWinding(VertexPositionNormalTexture[] vertexData, short[] indexData)
        {
            TriangleWinding wind = TriangleWinding.None;
            for (int i = 0; i + 2 < indexData.Length; i += 3)
            {
                Vector3 v1 = vertexData[indexData[i + 0]].Position;
                Vector3 v2 = vertexData[indexData[i + 1]].Position;
                Vector3 v3 = vertexData[indexData[i + 2]].Position;
                wind |= GetTriangleWinding(new Vector2(v1.X, v1.Y), new Vector2(v2.X, v2.Y), new Vector2(v3.X, v3.Y));

                // Break out if the condition is not going to change any more.
                if (wind == TriangleWinding.Mixed)
                    break;
            }
            return wind;
        }

        #endregion Methods for exporting other objects from 3D models

        #region Methods for modifying 3D models

        /// <summary>
        /// Removes the area of a convex polygon from a 3D model.
        /// </summary>
        /// The polygon's convexity is not checked, it is assumed.
        /// <param name="vertexData">Vertices of the 3D model.</param>
        /// <param name="indexData">Triangles of the 3D model.</param>
        /// <param name="area">The convex polygonal area to remove from the 3D model.</param>
        public static void RemoveArea(ref VertexPositionNormalTexture[] vertexData, ref short[] indexData, Polygon area)
        {
            // Brute force algorithm. Remove the polygonal area from each
            // triangle separately.
            bool[] triRemove = new bool[indexData.Length / 3]; // marks triangles to remove
            List<short> triCreate = new List<short>(20); // new triangles to create
            List<VertexPositionNormalTexture> newVertexData = // new list of vertices
                new List<VertexPositionNormalTexture>(vertexData.Length + area.Vertices.Length + 3);
            newVertexData.AddRange(vertexData);
            for (int triI = 0; triI + 2 < indexData.Length; triI += 3)
            {
                // Triangle's vertices are vertexData[indexData[triI + n]] for n = 0,1,2.

                // If triangle's vertices are all inside the polygon,
                // remove the whole triangle.
                // TODO: To prevent creation of ridiculously small leftover triangles,
                // consider triangle's vertices as inside the polygon even when they
                // are at a small distance from it.
                short[] triVertIndex = new short[] {
                    indexData[triI + 0],
                    indexData[triI + 1],
                    indexData[triI + 2]};
                VertexPositionNormalTexture[] triVertData = new VertexPositionNormalTexture[] {
                    vertexData[triVertIndex[0]],
                    vertexData[triVertIndex[1]],
                    vertexData[triVertIndex[2]]};
                Vector2[] triVert = new Vector2[] {
                    new Vector2(triVertData[0].Position.X, triVertData[0].Position.Y),
                    new Vector2(triVertData[1].Position.X, triVertData[1].Position.Y),
                    new Vector2(triVertData[2].Position.X, triVertData[2].Position.Y)};
                if (Geometry.Intersect(new Point(triVert[0]), area) &&
                    Geometry.Intersect(new Point(triVert[1]), area) &&
                    Geometry.Intersect(new Point(triVert[2]), area))
                {
                    triRemove[triI / 3] = true;
                    continue;
                }

                // Otherwise we do the general case.
                // Create a Polygon instance out of the triangle, for later use.
                Polygon trigon = new Polygon(new Vector2[] { triVert[0], triVert[1], triVert[2] });

                // Loop counterclockwise through the polygon's faces.
                int verInc = area.Clockwise() ? -1 : 1;
                int verI1 = 0; // index of the face's tail vertex
                int verI2 = 0; // index of the face's head vertex
                int triFirstOutIndex = -1; // first outvertex, or -1 if none yet, indexes our local tables
                int triLastOutIndex = -1; // latest outvertex relative to first outvertex, or -1 if none yet
                bool triPrevHadFirstOut = false; // true iff previous face had first outvertex as an outvertex
                while (!(verI2 == 0 && verI1 != 0)) // stop after the last face was processed
                {
                    verI1 = verI2;
                    verI2 = (verI2 + verInc + area.Vertices.Length) % area.Vertices.Length;

                    // Find out if the face's start point is inside the triangle.
                    bool tailInTri = Geometry.Intersect(new Point(area.Vertices[verI1]), trigon);

                    // Find out intersection points of the polygon face with the triangle's edge.
                    // In a special case an intersection point may come up twice. We skip duplicates.
                    Vector2? sectPnt = null; // temporary storage
                    Vector2? sectPnt1 = null; // one intersection point
                    Vector2? sectPnt2 = null; // another (different) intersection point
                    for (int triVertCheck = 0; triVertCheck < 3; ++triVertCheck)
                    {
                        Geometry.Intersect(triVert[triVertCheck], triVert[(triVertCheck + 1) % 3],
                            area.Vertices[verI1], area.Vertices[verI2], ref sectPnt);
                        if (sectPnt != null)
                        {
                            if (sectPnt1 == null)
                                sectPnt1 = sectPnt;
                            else if (!sectPnt1.Value.Equals(sectPnt.Value))
                            {
                                sectPnt2 = sectPnt;
                                break;
                            }
                        }
                    }

                    // Do nothing if the face is completely outside the triangle.
                    if (sectPnt1 == null && !tailInTri)
                    {
                        triPrevHadFirstOut = false;
                        continue;
                    }

                    // Remove the triangle. It will be replaced by several smaller ones.
                    triRemove[triI / 3] = true;

                    // Figure out the relevant part of the polygon's face.
                    Vector2 a = area.Vertices[verI1];
                    Vector2 b = area.Vertices[verI2];
                    if (sectPnt1 != null)
                    {
                        if (sectPnt2 != null)
                        {
                            // Figure out which intersection point is closer to the tail vertex.
                            float dist1 = Vector2.DistanceSquared(a, sectPnt1.Value);
                            float dist2 = Vector2.DistanceSquared(a, sectPnt2.Value);
                            if (dist1 < dist2)
                            {
                                a = sectPnt1.Value;
                                b = sectPnt2.Value;
                            }
                            else
                            {
                                a = sectPnt2.Value;
                                b = sectPnt1.Value;
                            }
                        }
                        else
                        {
                            // One of a and b is inside the triangle and the other one gets sectPnt1.
                            if (Geometry.Intersect(new Point(a), trigon))
                                b = sectPnt1.Value;
                            else
                                a = sectPnt1.Value;
                        }
                    }

                    // Add vertices a and b to the 3D model.
                    short aIndex = (short)newVertexData.Count;
                    short bIndex = (short)(aIndex + 1);
                    newVertexData.Add(InterpolateVertex(triVertData[0], triVertData[1], triVertData[2], a));
                    newVertexData.Add(InterpolateVertex(triVertData[0], triVertData[1], triVertData[2], b));

                    // Figure out triangle's vertices that are "outside" (right of) 
                    // the polygon's face. The triangle's vertices are in clockwise order,
                    // and we want them in counterclockwise order. 
                    bool[] triVertOut = new bool[] {
                        (Geometry.Stand(triVert[0], a, b) == AW2.Helpers.Geometry.StandType.Right),
                        (Geometry.Stand(triVert[1], a, b) == Geometry.StandType.Right),
                        (Geometry.Stand(triVert[2], a, b) == Geometry.StandType.Right)};
                    bool triLastOutIndexUpdated = false;
                    if (triLastOutIndex == -1)
                    {
                        // Find the first triangle vertex "outside" this face (an "outvertex"),
                        // in counterclockwise order.
                        if (triVertOut[2] && !triVertOut[0]) triFirstOutIndex = 2;
                        else if (triVertOut[1] && !triVertOut[2]) triFirstOutIndex = 1;
                        else if (triVertOut[0] && !triVertOut[1]) triFirstOutIndex = 0;
                        triLastOutIndex = 3;
                        triLastOutIndexUpdated = true;
                        triPrevHadFirstOut = true; // actually it's us who has the first outvertex
                    }

                    // Add triangles to the 3D model if this face found new outvertices.
                    for (int triOutIndex = triLastOutIndex - 1; 
                        triOutIndex > 0 || (triOutIndex == 0 && !triPrevHadFirstOut);
                        --triOutIndex)
                    {
                        if (!triVertOut[(triFirstOutIndex + triOutIndex) % 3])
                            continue;
                        if (tailInTri || triLastOutIndexUpdated)
                        {
                            triCreate.Add(aIndex);
                            triCreate.Add(triVertIndex[(triFirstOutIndex + triOutIndex) % 3]);
                            triCreate.Add(triVertIndex[(triFirstOutIndex + triLastOutIndex) % 3]);
                        }
                        triLastOutIndex = triOutIndex;
                        triLastOutIndexUpdated = true;
                    }
                    triPrevHadFirstOut = triVertOut[triFirstOutIndex]; // set for the next face

                    // Add a triangle to the 3D model, covering this polygon face.
                    triCreate.Add(aIndex);
                    triCreate.Add(bIndex);
                    triCreate.Add(triVertIndex[(triFirstOutIndex + triLastOutIndex) % 3]);
                }

                // If the first face's first outvertex and the last face's last outvertex
                // aren't the same, create one final triangle, unless it's negative.
                if (triLastOutIndex != 0 && Geometry.Intersect(new Point(area.Vertices[0]), trigon))
                {
                    triCreate.Add((short)vertexData.Length);
                    triCreate.Add(triVertIndex[triFirstOutIndex]);
                    triCreate.Add(triVertIndex[(triFirstOutIndex + triLastOutIndex) % 3]);
                }
            }

            // Create a new triangle list.
            List<short> newIndexData = new List<short>(indexData.Length + triCreate.Count / 3);
            for (int i = 0; i + 2 < indexData.Length; i += 3)
                if (!triRemove[i / 3])
                {
                    newIndexData.Add(indexData[i + 0]);
                    newIndexData.Add(indexData[i + 1]);
                    newIndexData.Add(indexData[i + 2]);
                }

            // Add new triangles, but skip ugly triangles with very short faces.
            for (int addTriIndex = 0; addTriIndex + 2 < triCreate.Count; addTriIndex += 3)
            {
                short i1 = triCreate[addTriIndex + 0];
                short i2 = triCreate[addTriIndex + 1];
                short i3 = triCreate[addTriIndex + 2];
                float len1 = Vector3.DistanceSquared(newVertexData[i1].Position, newVertexData[i2].Position);
                float len2 = Vector3.DistanceSquared(newVertexData[i2].Position, newVertexData[i3].Position);
                float len3 = Vector3.DistanceSquared(newVertexData[i3].Position, newVertexData[i1].Position);
                float minimum = 0.3f * 0.3f;
                if (len1 >= minimum && len2 >= minimum && len3 >= minimum)
                {
                    newIndexData.Add(i1);
                    newIndexData.Add(i2);
                    newIndexData.Add(i3);
                }
            }
            indexData = newIndexData.ToArray();

            // Create a new vertex list.
            // TODO: Remove unused vertices.
            vertexData = newVertexData.ToArray();
        }

        /// <summary>
        /// Changes triangle winding in a 3D model by editing vertex order of triangles.
        /// Winding is determined by triangles' projections to the X-Y-plane.
        /// </summary>
        /// <param name="vertexData">The vertices of the 3D model.</param>
        /// <param name="indexData">The triangles of the 3D model.</param>
        /// <param name="wind">The desired winding.</param>
        /// <exception cref="ArgumentException"><b>wind</b> is not TriangleWinding.Clockwise or
        /// TriangleWinding.CounterClockwise.</exception>
        public static void SetTriangleWinding(VertexPositionNormalTexture[] vertexData, ref short[] indexData,
            TriangleWinding wind)
        {
            if (wind != TriangleWinding.Clockwise && wind != TriangleWinding.CounterClockwise)
                throw new ArgumentException("Illegal winding " + wind.ToString());
            for (int i = 0; i + 2 < indexData.Length; i += 3)
            {
                Vector3 v1 = vertexData[indexData[i + 0]].Position;
                Vector3 v2 = vertexData[indexData[i + 1]].Position;
                Vector3 v3 = vertexData[indexData[i + 2]].Position;
                TriangleWinding oldWind = GetTriangleWinding(new Vector2(v1.X, v1.Y), new Vector2(v2.X, v2.Y), new Vector2(v3.X, v3.Y));
                if (oldWind != TriangleWinding.None && oldWind != wind)
                {
                    short swap = indexData[i + 1];
                    indexData[i + 1] = indexData[i + 2];
                    indexData[i + 2] = swap;
                }
            }
        }

        /// <summary>
        /// Reverses the winding of each triangle in a 3D model by editing vertex order of triangles.
        /// </summary>
        /// <param name="model">The 3D model.</param>
        public static void ReverseTriangleWinding(Model model)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                IndexBuffer indexBuffer = mesh.IndexBuffer;
                if (indexBuffer.IndexElementSize != IndexElementSize.SixteenBits)
                    throw new Exception("Index buffer is not 16 bit -- not supported");
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    int vertexCount = meshPart.NumVertices;
                    short[] indexData = new short[vertexCount];
                    indexBuffer.GetData(meshPart.StartIndex * sizeof(short), indexData, 0, vertexCount);
                    for (int i = 0; i + 2 < indexData.Length; i += 3)
                    {
                        short swap = indexData[i + 1];
                        indexData[i + 1] = indexData[i + 2];
                        indexData[i + 2] = swap;
                    }
                    indexBuffer.SetData(meshPart.StartIndex * sizeof(short), indexData, 0, vertexCount);
                }
            }
        }

        /// <summary>
        /// Transforms the vertices and normals of a 3D model.
        /// </summary>
        /// Use this method to translate a 3D model from one coordinate system to another.
        /// <param name="model">The 3D model to modify.</param>
        /// <param name="vertexTransform">The transformation to apply to the model's vertices.</param>
        /// <param name="normalTransform">The transformation to apply to the model's normals.</param>
        public static void TransformModel(Model model, Matrix vertexTransform, Matrix normalTransform)
        {
            Matrix[] modelPartTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(modelPartTransforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                Matrix meshTransform = modelPartTransforms[mesh.ParentBone.Index];
                Matrix totalVertexTransform = Matrix.Invert(meshTransform) * vertexTransform * meshTransform;
                Vector3 scale;
                Quaternion rotation;
                Vector3 translation;
                meshTransform.Decompose(out scale, out rotation, out translation);
                Matrix meshTransformNoTranslate = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation);
                Matrix totalNormalTransform = Matrix.Invert(meshTransformNoTranslate) * normalTransform * meshTransformNoTranslate;
                VertexBuffer vertexBuffer = mesh.VertexBuffer;
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    int vertexCount = meshPart.NumVertices;
                    VertexPositionTextureNormal[] vertexData = new VertexPositionTextureNormal[vertexCount];
                    vertexBuffer.GetData(meshPart.StreamOffset, vertexData, 0, vertexCount, meshPart.VertexStride);
                    for (int i = 0; i < vertexData.Length; ++i)
                    {
                        vertexData[i].Position = Vector3.Transform(vertexData[i].Position, totalVertexTransform);
                        vertexData[i].Normal = Vector3.Transform(vertexData[i].Normal, totalNormalTransform);
                    }
                    vertexBuffer.SetData(meshPart.StreamOffset, vertexData, 0, vertexCount, meshPart.VertexStride);
                }
            }
        }

        #endregion Methods for modifying 3D models

        #region Utility methods for 3D graphics

        /// <summary>
        /// Interpolates a new 3D model vertex based on three other vertices.
        /// </summary>
        /// The vertices are assumed to be lifted from the X-Y-plane and thus the interpolation
        /// is done solely based on the X and Y coordinates. If the three vertices form a reduced
        /// triangle, the return value is undefined but probably still usable.
        /// <param name="vert0">First known vertex.</param>
        /// <param name="vert1">Second known vertex.</param>
        /// <param name="vert2">Third known vertex.</param>
        /// <param name="pos">Location of new vertex to interpolate.</param>
        /// <returns>The new, interpolated vertex.</returns>
        private static VertexPositionNormalTexture InterpolateVertex(VertexPositionNormalTexture vert0,
            VertexPositionNormalTexture vert1, VertexPositionNormalTexture vert2, Vector2 pos)
        {
            float amount2, amount3;
            Geometry.CartesianToBarycentric(new Vector2(vert0.Position.X, vert0.Position.Y),
                new Vector2(vert1.Position.X, vert1.Position.Y),
                new Vector2(vert2.Position.X, vert2.Position.Y),
                pos, out amount2, out amount3);

            Vector3 newPos = Vector3.Barycentric(vert0.Position, vert1.Position, vert2.Position,
                amount2, amount3);
            Vector3 newNormal = Vector3.Barycentric(vert0.Normal, vert1.Normal, vert2.Normal,
                amount2, amount3);
            Vector2 newTextureCoord = Vector2.Barycentric(vert0.TextureCoordinate, vert1.TextureCoordinate, vert2.TextureCoordinate,
                amount2, amount3);
            return new VertexPositionNormalTexture(newPos, newNormal, newTextureCoord);
        }

        #endregion Utility methods for 3D graphics


        #region Unit tests

#if DEBUG

        /// <summary>
        /// Tests the Graphics3D class.
        /// </summary>
        [TestFixture]
        public class Graphics3DTest
        {
            /// <summary>
            /// Sets up the testing.
            /// </summary>
            [SetUp]
            public void SetUp()
            {
            }
            /// <summary>
            /// Tests finding a polygonal outline for a 3D model.
            /// </summary>
            [Test]
            public void TestGetOutline()
            {
                Vector3 z1 = Vector3.UnitZ;
                Vector3 z2 = Vector3.Normalize(new Vector3(0,1,2));
                Vector2 c = Vector2.Zero;
                Vector2 q12d = new Vector2(20f, 20f);
                Vector2 q22d = new Vector2(40f, 20f);
                Vector2 q32d = new Vector2(30f, 40f);
                Vector2 q42d = new Vector2(50f, 40f);
                Vector3 q1 = new Vector3(q12d, 0f);
                Vector3 q2 = new Vector3(q22d, 0f);
                Vector3 q3 = new Vector3(q32d, 0f);
                Vector3 q4 = new Vector3(q42d, 0f);

                VertexPositionNormalTexture[] vertexData1 = new VertexPositionNormalTexture[] {
                    new VertexPositionNormalTexture(q1,z1,c), // 0
                    new VertexPositionNormalTexture(q2,z1,c), // 1
                    new VertexPositionNormalTexture(q3,z1,c), // 2
                    new VertexPositionNormalTexture(q4,z1,c), // 3
                };
                short[] indexData1 = new short[] {
                    0,2,1, 2,1,3
                };
                VertexPositionNormalTexture[] vertexData2 = new VertexPositionNormalTexture[] {
                    new VertexPositionNormalTexture(q1,z1,c), // 0
                    new VertexPositionNormalTexture(q2,z1,c), // 1
                    new VertexPositionNormalTexture(q3,z1,c), // 2
                    new VertexPositionNormalTexture(q4,z1,c), // 3
                    new VertexPositionNormalTexture(q2,z2,c), // 4
                    new VertexPositionNormalTexture(q3,z2,c), // 5
                };
                short[] indexData2 = new short[] {
                    0,2,1, 2,1,3
                };

                Polygon poly1 = Graphics3D.GetOutline(vertexData1, indexData1);
                Polygon poly1Expected = new Polygon(new Vector2[] { q12d, q22d, q42d, q32d });
                Assert.IsTrue(poly1Expected.Equals(poly1));

                Polygon poly2 = Graphics3D.GetOutline(vertexData2, indexData2);
                Polygon poly2Expected = new Polygon(new Vector2[] { q12d, q22d, q42d, q32d });
                Assert.IsTrue(poly2Expected.Equals(poly2));
            }

            /// <summary>
            /// Tests removal of polygonal area from a 3D model.
            /// </summary>
            [Test]
            public void TestRemoveArea()
            {
                Vector3 z = Vector3.UnitZ; // shorthand
                Vector2 c = Vector2.Zero; // shorthand
                Vector3 q1 = new Vector3(20f, 20f, 0f);
                Vector3 q2 = new Vector3(40f, 20f, 0f);
                Vector3 q3 = new Vector3(30f, 40f, 0f);
                Vector3 q4 = new Vector3(50f, 40f, 0f);
                VertexPositionNormalTexture[] vertexData = new VertexPositionNormalTexture[] {
                    new VertexPositionNormalTexture(q1,z,c), // 0
                    new VertexPositionNormalTexture(q2,z,c), // 1
                    new VertexPositionNormalTexture(q3,z,c), // 2
                    new VertexPositionNormalTexture(q4,z,c), // 3
                };
                short[] indexData = new short[] {
                    0,2,1, 2,1,3
                };

                Vector2 v1 = new Vector2(10f, 10f);
                Vector2 v2 = new Vector2(60f, 10f);
                Vector2 v3 = new Vector2(60f, 60f);
                Vector2 v4 = new Vector2(10f, 60f);
                Polygon poly1 = new Polygon(new Vector2[] { v1, v2, v3, v4 });

                // All of the model is removed.
                RemoveArea(ref vertexData, ref indexData, poly1);
                Assert.IsEmpty(indexData);
            }
        }
#endif
        #endregion // Unit tests

    }
}

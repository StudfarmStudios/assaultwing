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
using AW2.Helpers.Geometric;

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
    public static class Graphics3D
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

        static BasicEffect debugEffect;
        static BasicEffect DebugEffect
        {
            get
            {
                if (debugEffect == null)
                {
                    debugEffect = new BasicEffect(AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice, null);
                    debugEffect.TextureEnabled = false;
                    debugEffect.VertexColorEnabled = true;
                    debugEffect.LightingEnabled = false;
                    debugEffect.FogEnabled = false;
                }
                return debugEffect;
            }
        }

        #region Methods for exporting raw data from 3D models and importing it back

        /// <summary>
        /// Extracts vertex and triangle information out of a 3D model.
        /// </summary>
        /// The triangle data is stored as a triangle list.
        /// <param name="model">The 3D model.</param>
        /// <param name="vertexData">Where to store the vertex data.</param>
        /// <param name="indexData">Where to store the triangle data.</param>
        public static void GetModelData(Model model, out VertexPositionNormalTexture[] vertexData,
            out short[] indexData)
        {
            var vertices = new List<VertexPositionNormalTexture>();
            var indices = new List<short>();
            foreach (var mesh in model.Meshes)
                GetMeshData(mesh, vertices, indices);
            vertexData = vertices.ToArray();
            indexData = indices.ToArray();
        }
        
        /// <summary>
        /// Extracts vertex and triangle information out of a 3D mesh.
        /// </summary>
        /// The triangle data is stored as a triangle list.
        /// <param name="mesh">The 3D mesh.</param>
        /// <param name="vertices">Where to store the vertex data.</param>
        /// <param name="indices">Where to store the triangle data.</param>
        private static void GetMeshData(ModelMesh mesh, List<VertexPositionNormalTexture> vertices,
            List<short> indices)
        {
            foreach (var part in mesh.MeshParts)
            {
                int vertexCount = part.NumVertices;
                int indexCount = part.PrimitiveCount;
                var vertexData = new VertexPositionNormalTexture[vertexCount];
                var indexData = new short[indexCount * 3];
                mesh.VertexBuffer.GetData(vertexData);
                mesh.IndexBuffer.GetData(indexData);
                Matrix worldMatrix = Matrix.Identity;
                for (ModelBone bone = mesh.ParentBone; bone != null; bone = bone.Parent)
                    worldMatrix *= bone.Transform;
                for (int i = 0; i < vertexData.Length; ++i)
                {
                    vertexData[i].Position = Vector3.Transform(vertexData[i].Position, worldMatrix);
                    vertexData[i].Normal = Vector3.TransformNormal(vertexData[i].Normal, worldMatrix);
                }
                vertices.AddRange(vertexData);
                indices.AddRange(indexData);
            }
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
            var vertexData = new List<VertexPositionNormalTexture>();
            var indexData = new List<short>();
            GetMeshData(mesh, vertexData, indexData);
            return GetOutline(vertexData.ToArray(), indexData.ToArray());
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
                e.Data.Add("debug", polyVertices.ToArray());
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

        /// <summary>
        /// Fines the triangles of a 3D model. Triangles are split to smaller ones
        /// until they don't have edges longer than the given maximum dimension.
        /// Z coordinates don't affect the fining.
        /// </summary>
        /// <param name="maxDimension">The maximum length of edges in resulting triangles.</param>
        /// <param name="vertexData">The vertices of the 3D model.</param>
        /// <param name="indexData">The indices of the 3D model's triangles as a triangle list.</param>
        /// <param name="newVertexData">Where to store the vertex data of the fined 3D model.</param>
        /// <param name="newIndexData">Where to store the index data of the fined 3D model,
        /// as a triangle list.</param>
        public static void FineTriangles(float maxDimension, VertexPositionNormalTexture[] vertexData, short[] indexData,
            out VertexPositionNormalTexture[] newVertexData, out short[] newIndexData)
        {
            int originalIndexLength = indexData.Length;
            float maxDimensionSquared = maxDimension * maxDimension;
            List<short> fineIndexData = new List<short>(indexData.Length);
            List<VertexPositionNormalTexture> fineVertexData = new List<VertexPositionNormalTexture>(vertexData);

            // Iterate over the 3D model until it's fine enough.
            VertexPositionNormalTexture newVertex = new VertexPositionNormalTexture();
            while (true)
            {
                // Loop through triangles.
                for (int i = 0; i + 2 < indexData.Length; i += 3)
                {
                    // If any face of the triangle is too long, we split the triangle in two by
                    // adding a new vertex in the middle of the longest face of the triangle.
                    Vector2 v0 = new Vector2(fineVertexData[indexData[i + 0]].Position.X, fineVertexData[indexData[i + 0]].Position.Y);
                    Vector2 v1 = new Vector2(fineVertexData[indexData[i + 1]].Position.X, fineVertexData[indexData[i + 1]].Position.Y);
                    Vector2 v2 = new Vector2(fineVertexData[indexData[i + 2]].Position.X, fineVertexData[indexData[i + 2]].Position.Y);
                    Vector2[] verts = new Vector2[] { v0, v1, v2 }; // for indexing with variables
                    float length01Squared = Vector2.DistanceSquared(v0, v1);
                    float length02Squared = Vector2.DistanceSquared(v0, v2);
                    float length12Squared = Vector2.DistanceSquared(v1, v2);
                    int splitFace = -1; // -1 or the face [splitFace, (splitFace + 1) % 3]
                    if (length01Squared >= length02Squared &&
                        length01Squared >= length12Squared &&
                        length01Squared > maxDimensionSquared)
                    {
                        splitFace = 0;
                        newVertex = InterpolateEdge(fineVertexData[indexData[i + 0]], fineVertexData[indexData[i + 1]]);
                    }
                    else if (length02Squared >= length01Squared &&
                        length02Squared >= length12Squared &&
                        length02Squared > maxDimensionSquared)
                    {
                        splitFace = 2;
                        newVertex = InterpolateEdge(fineVertexData[indexData[i + 2]], fineVertexData[indexData[i + 0]]);
                    }
                    else if (length12Squared >= length01Squared &&
                            length12Squared >= length02Squared &&
                            length12Squared > maxDimensionSquared)
                    {
                        splitFace = 1;
                        newVertex = InterpolateEdge(fineVertexData[indexData[i + 1]], fineVertexData[indexData[i + 2]]);
                    }
                    if (splitFace == -1)
                    {
                        fineIndexData.Add(indexData[i + 0]);
                        fineIndexData.Add(indexData[i + 1]);
                        fineIndexData.Add(indexData[i + 2]);
                    }
                    else
                    {
                        short newIndex = (short)fineVertexData.Count;
                        fineVertexData.Add(newVertex);
                        fineIndexData.Add(newIndex);
                        fineIndexData.Add(indexData[i + (splitFace + 1) % 3]);
                        fineIndexData.Add(indexData[i + (splitFace + 2) % 3]);

                        fineIndexData.Add(newIndex);
                        fineIndexData.Add(indexData[i + (splitFace + 2) % 3]);
                        fineIndexData.Add(indexData[i + (splitFace + 0) % 3]);
                    }
                }

                // If no triangles were split, they are all small enough.
                if (indexData.Length == fineIndexData.Count) break;

                indexData = fineIndexData.ToArray();
                fineIndexData.Clear();
            }
            newVertexData = fineVertexData.ToArray();
            newIndexData = fineIndexData.ToArray();
        }

        #endregion Methods for modifying 3D models

        #region Utility methods for 3D graphics

        /// <summary>
        /// Draws a bounding sphere for debug purposes.
        /// </summary>
        public static void DebugDraw(BoundingSphere sphere, Matrix view, Matrix projection, Matrix world)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            VertexPositionColor[] vertexData;
            Graphics3D.GetWireframeModelData(sphere, 300, Color.Aquamarine, out vertexData);
            DebugEffect.View = view;
            DebugEffect.Projection = projection;
            DebugEffect.World = world;
            gfx.VertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements);
            DebugEffect.Begin(SaveStateMode.SaveState);
            foreach (var pass in DebugEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineStrip, vertexData, 0, vertexData.Length - 1);
                pass.End();
            }
            DebugEffect.End();
        }

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

        /// <summary>
        /// Interpolates a new 3D model vertex based on three other vertices.
        /// </summary>
        /// The vertices are assumed to be lifted from the X-Y-plane and thus the interpolation
        /// is done solely based on the X and Y coordinates. If the three vertices form a reduced
        /// triangle, the return value is undefined but probably still usable.
        /// <param name="vert0">First known vertex.</param>
        /// <param name="vert1">Second known vertex.</param>
        /// <param name="vert2">Third known vertex.</param>
        /// <param name="posX">X coordinate of new vertex to interpolate.</param>
        /// <param name="posY">Y coordinate of new vertex to interpolate.</param>
        /// <returns>The new, interpolated vertex.</returns>
        private static VertexPositionNormalTexture InterpolateVertex(VertexPositionNormalTexture vert0,
            VertexPositionNormalTexture vert1, VertexPositionNormalTexture vert2,
            double posX, double posY)
        {
            double amount2, amount3;
            Geometry.CartesianToBarycentric(new Vector2(vert0.Position.X, vert0.Position.Y),
                new Vector2(vert1.Position.X, vert1.Position.Y),
                new Vector2(vert2.Position.X, vert2.Position.Y),
                posX, posY, out amount2, out amount3);

            Vector3 newPos = Geometry.BarycentricToCartesian(vert0.Position, vert1.Position, vert2.Position,
                amount2, amount3);
            Vector3 newNormal = Geometry.BarycentricToCartesian(vert0.Normal, vert1.Normal, vert2.Normal,
                amount2, amount3);
            Vector2 newTextureCoord = Geometry.BarycentricToCartesian(vert0.TextureCoordinate, vert1.TextureCoordinate, vert2.TextureCoordinate,
                amount2, amount3);
            return new VertexPositionNormalTexture(newPos, newNormal, newTextureCoord);
        }

        /// <summary>
        /// Interpolates a new 3D model vertex midway between two other vertices.
        /// </summary>
        /// <param name="vert0">First known vertex.</param>
        /// <param name="vert1">Second known vertex.</param>
        /// <returns>The new, interpolated vertex.</returns>
        private static VertexPositionNormalTexture InterpolateEdge(VertexPositionNormalTexture vert0,
            VertexPositionNormalTexture vert1)
        {
#if false // single precision interpolation -- ugly results
            return new VertexPositionNormalTexture(Vector3.Lerp(vert0.Position, vert1.Position, 0.5f),
                Vector3.Lerp(vert0.Normal, vert1.Normal, 0.5f),
                Vector2.Lerp(vert0.TextureCoordinate, vert0.TextureCoordinate, 0.5f));
#else // double precision interpolation -- beautiful results
            Vector3 position = new Vector3((float)(((double)vert0.Position.X + vert1.Position.X) / 2),
                (float)(((double)vert0.Position.Y + vert1.Position.Y) / 2),
                (float)(((double)vert0.Position.Z + vert1.Position.Z) / 2));
            Vector3 normal = new Vector3((float)(((double)vert0.Normal.X + vert1.Normal.X) / 2),
                (float)(((double)vert0.Normal.Y + vert1.Normal.Y) / 2),
                (float)(((double)vert0.Normal.Z + vert1.Normal.Z) / 2));
            Vector2 textureCoordinate = new Vector2((float)(((double)vert0.TextureCoordinate.X + vert1.TextureCoordinate.X) / 2),
                (float)(((double)vert0.TextureCoordinate.Y + vert1.TextureCoordinate.Y) / 2));
            return new VertexPositionNormalTexture(position, normal, textureCoordinate);
#endif
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
        }
#endif
        #endregion // Unit tests
    }
}

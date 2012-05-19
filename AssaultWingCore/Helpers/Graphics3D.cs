using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Helpers.Geometric;
using IndexPair = System.Tuple<Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework.Vector3>;

namespace AW2.Helpers
{
    /// <summary>
    /// An equality comparer for the Graphics3D.GetOutline method.
    /// </summary>
    /// <see cref="Graphics3D.GetOutline(Model)"/>
    /// <see cref="Graphics3D.GetOutline(VertexPositionNormalTexture[], short[])"/>
    internal class IndexPairEqualityComparer : IEqualityComparer<IndexPair>
    {
        /// <summary>
        /// Two index pairs are equal if they represent the same vertex location,
        /// regardless of order.
        /// </summary>
        public bool Equals(IndexPair x, IndexPair y)
        {
            return (x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2))
                || (x.Item1.Equals(y.Item2) && x.Item2.Equals(y.Item1));
        }

        public int GetHashCode(IndexPair obj)
        {
            return obj.Item1.GetHashCode() ^ obj.Item2.GetHashCode();
        }
    }

    /// <summary>
    /// Contains helper methods for 3D graphics.
    /// </summary>
    public static class Graphics3D
    {
        public struct DebugDrawContext
        {
            public static readonly Color DEFAULT_COLOR = Color.Aquamarine;

            public Matrix View;
            public Matrix Projection;
            public Matrix World;
            public Color Color;

            public DebugDrawContext(Matrix view, Matrix projection)
                :this (view, projection, Matrix.Identity)
            {
            }

            public DebugDrawContext(Matrix view, Matrix projection, Matrix world)
            {
                View = view;
                Projection = projection;
                World = world;
                Color = DEFAULT_COLOR;
            }
        }

        private const float DEBUG_DRAW_Z = 300;

        static BasicEffect debugEffect;
        static BasicEffect DebugEffect
        {
            get
            {
                if (debugEffect == null)
                {
                    debugEffect = new BasicEffect(AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice);
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
        /// Note: Call this method only from under a Draw() method. Otherwise the returned
        /// data may be wrong. This will happen at least if GetModelData is called when
        /// the game is in fullscreen mode but doesn't have focus.
        /// </summary>
        /// The triangle data is stored as a triangle list.
        /// <param name="model">The 3D model.</param>
        /// <param name="vertexData">Where to store the vertex data.</param>
        /// <param name="indexData">Where to store the triangle data.</param>
        public static void GetModelData(Model model, out VertexPositionNormalTexture[] vertexData, out short[] indexData)
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
                if (part.VertexBuffer.VertexDeclaration.VertexStride != VertexPositionNormalTexture.VertexDeclaration.VertexStride)
                    throw new InvalidOperationException("Model has an unsupported VertexDeclaration " + part.VertexBuffer.VertexDeclaration.Name);

                int vertexCount = part.NumVertices;
                var vertexData = new VertexPositionNormalTexture[vertexCount];
                var vertexStride = VertexPositionNormalTexture.VertexDeclaration.VertexStride;
                part.VertexBuffer.GetData(part.VertexOffset * vertexStride, vertexData, 0, vertexCount, vertexStride);

                int indexCount = part.PrimitiveCount * 3;
                var indexData = new short[indexCount];
                var indexStride = part.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits ? 2
                    : part.IndexBuffer.IndexElementSize == IndexElementSize.ThirtyTwoBits ? 4
                    : -1;
                if (indexStride == -1) throw new InvalidOperationException("Model has unexpected IndexElementSize " + part.IndexBuffer.IndexElementSize);
                part.IndexBuffer.GetData(part.StartIndex * indexStride, indexData, 0, indexCount);

                var worldMatrix = Matrix.Identity;
                for (var bone = mesh.ParentBone; bone != null; bone = bone.Parent)
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

        #endregion Methods for exporting raw data from 3D models and importing it back

        #region Methods for exporting other objects from 3D models

        /// <summary>
        /// Finds out a polygonal outline for a 3D mesh.
        /// </summary>
        /// <remarks>
        /// <para>
        /// As the return value is only a simple polygon (and because of implementation
        /// details), the interior of the input should be simply connected, i.e. the
        /// model must define one piece without holes and without two pieces connected
        /// by a single vertex. Otherwise the return value is undefined (but will still
        /// try to be nice).
        /// </para>
        /// <para>
        /// Tip: To save a ModelMesh <c>mesh</c> as an outline in an XML file, try this in Visual Studio Immediate Window:
        /// <code>System.Type.GetType("AW2.Helpers.Serialization.TypeLoader, AssaultWingCore").InvokeMember("SaveTemplate", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, null, new object[] { GetOutline(mesh), @"c:\temp\mesh_outline.xml", System.Type.GetType("AW2.Helpers.Geometric.IGeomPrimitive, AssaultWingCore"), System.Type.GetType("AW2.Helpers.Serialization.RuntimeStateAttribute, AssaultWingCore")})</code>
        /// This can useful for creating collision areas from 3D model data.
        /// </para>
        /// </remarks>
        public static Polygon GetOutline(ModelMesh mesh)
        {
            // Note: Don't delete this method even though it's not called from anywhere.
            // It is a useful tool for converting 3D model meshes into collision area polygons.
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
            var comparer = new IndexPairEqualityComparer();
            var faceUseCounts = new Dictionary<IndexPair, int>(indexData.Length, comparer);
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

            var polyVertices = new List<Vector2>();
            
            // Find first polygon vertex.
            Vector3 firstIndex = new Vector3(Single.NaN, Single.NaN, Single.NaN);
            Vector3 prevIndex = new Vector3(Single.NaN, Single.NaN, Single.NaN);
            Vector3 nextIndex = new Vector3(Single.NaN, Single.NaN, Single.NaN);
            foreach (KeyValuePair<IndexPair, int> faceUseCount in faceUseCounts)
                if (faceUseCount.Value == 1)
                {
                    prevIndex = firstIndex = faceUseCount.Key.Item1;
                    nextIndex = faceUseCount.Key.Item2;
                    polyVertices.Add(firstIndex.ProjectXY());
                    break;
                }

            // Find the remaining polygon vertices.
            while (!nextIndex.Equals(firstIndex))
            {
                bool foundNext = false;
                foreach (KeyValuePair<IndexPair, int> faceUseCount in faceUseCounts)
                    if (faceUseCount.Value == 1 && faceUseCount.Key.Item1.Equals(nextIndex)
                        && !faceUseCount.Key.Item2.Equals(prevIndex))
                    {
                        polyVertices.Add(nextIndex.ProjectXY());
                        prevIndex = nextIndex;
                        nextIndex = faceUseCount.Key.Item2;
                        foundNext = true;
                        break;
                    }
                    else if (faceUseCount.Value == 1 && faceUseCount.Key.Item2.Equals(nextIndex)
                        && !faceUseCount.Key.Item1.Equals(prevIndex))
                {
                    polyVertices.Add(nextIndex.ProjectXY());
                    prevIndex = nextIndex;
                    nextIndex = faceUseCount.Key.Item1;
                    foundNext = true;
                    break;
                }
                if (!foundNext) throw new ArgumentException("Unable to find outline for 3D model");
            }

            return new Polygon(polyVertices.ToArray());
        }

        #endregion Methods for exporting other objects from 3D models

        #region Utility methods for 3D graphics

        public static void DebugDrawCircle(DebugDrawContext context, BoundingSphere sphere)
        {
            VertexPositionColor[] vertexData;
            Graphics3D.GetWireframeModelData(sphere, DEBUG_DRAW_Z, context.Color, out vertexData);
            DebugDraw(context, vertexData, PrimitiveType.LineStrip);
        }

        public static void DebugDrawPolyline(DebugDrawContext context, params Vector2[] vertices)
        {
            var vertexData = new VertexPositionColor[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                vertexData[i] = new VertexPositionColor(new Vector3(vertices[i], DEBUG_DRAW_Z), context.Color);
            DebugDraw(context, vertexData, PrimitiveType.LineStrip);
        }

        public static void DebugDrawPoints(DebugDrawContext context, params Vector2[] points)
        {
            var vertexData = new VertexPositionColor[points.Length * 2];
            for (int i = 0; i < points.Length; i++)
            {
                vertexData[i * 2 + 0] = new VertexPositionColor(new Vector3(points[i], DEBUG_DRAW_Z), context.Color);
                vertexData[i * 2 + 1] = new VertexPositionColor(new Vector3(points[i] + Vector2.UnitX, DEBUG_DRAW_Z), context.Color);
            }
            DebugDraw(context, vertexData, PrimitiveType.LineList);
        }

        private static void DebugDraw(DebugDrawContext context, VertexPositionColor[] vertexData, PrimitiveType primitiveType)
        {
            var gfx = AssaultWingCore.Instance.GraphicsDeviceService.GraphicsDevice;
            DebugEffect.View = context.View;
            DebugEffect.Projection = context.Projection;
            DebugEffect.World = context.World;
            var primitiveCount = primitiveType == PrimitiveType.LineList ? vertexData.Length / 2
                : primitiveType == PrimitiveType.LineStrip ? vertexData.Length - 1
                : primitiveType == PrimitiveType.TriangleList ? vertexData.Length / 3
                : primitiveType == PrimitiveType.TriangleStrip ? vertexData.Length - 2
                : 0;
            if (primitiveCount <= 0) throw new ArgumentException("Invalid primitive type or vertex count");
            foreach (var pass in DebugEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                gfx.DrawUserPrimitives<VertexPositionColor>(primitiveType, vertexData, 0, primitiveCount);
            }
        }

        #endregion Utility methods for 3D graphics
    }
}

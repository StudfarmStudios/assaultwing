using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Core;
using AW2.Graphics.Content;
using AW2.Helpers.Geometric;
using IndexPair = System.Tuple<Microsoft.Xna.Framework.Vector3, Microsoft.Xna.Framework.Vector3>;

namespace AW2.Helpers
{
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

        public static void GetVertexAndIndexData(ModelGeometry modelGeometry, out VertexPositionNormalTexture[] vertexData, out short[] indexData)
        {
            if (modelGeometry.Meshes.Length != 1) throw new NotImplementedException("Multiple meshes");
            var mesh = modelGeometry.Meshes[0];
            if (mesh.MeshParts.Length != 1) throw new NotImplementedException("Multiple mesh parts");
            var meshPartToworldMatrix = Matrix.Identity;
            for (var bone = mesh.ParentBone; bone != null; bone = bone.Parent) meshPartToworldMatrix *= bone.Transform;
            var meshPart = mesh.MeshParts[0];
            vertexData = meshPart.VertexBuffer.Vertices
                .Select(vertex => new VertexPositionNormalTexture(
                    position: Vector3.Transform(vertex.Position, meshPartToworldMatrix),
                    normal: Vector3.TransformNormal(vertex.Normal, meshPartToworldMatrix),
                    textureCoordinate: vertex.TextureCoordinate))
                .ToArray();
            indexData = new short[meshPart.PrimitiveCount * 3];
            Array.Copy(meshPart.IndexBuffer.Indices, meshPart.StartIndex, indexData, 0, indexData.Length);
        }

        /// <summary>
        /// Extracts from a sphere necessary vertex data for a wireframe model.
        /// The returned vertex data is suitable for drawing a line strip.
        /// The model is a projection of the sphere to the X-Y-plane.
        /// </summary>
        public static void GetWireframeModelData(BoundingSphere sphere, float z, Color color, out VertexPositionColor[] vertexData)
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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AW2.Helpers;
using AWRectangle = AW2.Helpers.Geometric.Rectangle;

namespace AW2.Game.GobUtils
{
    public class WallIndexMap
    {
        public delegate void RemoveTriangleDelegate(int index);

        /// <summary>
        /// Triangle index map of the wall's 3D model in the X-Y plane.
        /// </summary>
        /// If indexMap[y,x] == null then no triangle covers index map point (x,y).
        /// Otherwise indexMap[y,x] is an array of indices n such that 
        /// the triangle that is defined by the 3D model's index map elements 
        /// 3n, 3n+1 and 3n+2 covers the index map point (x,y).
        /// The index map has its own coordinate system that can be obtained from
        /// the 3D model's coordinate system by <see cref="IndexMapTransform"/>.
        private int[,][] _indexMap;

        /// <summary>
        /// Transformation matrix from wall's 3D model's coordinates to index map coordinates.
        /// </summary>
        public Matrix WallToIndexMapTransform { get; private set; }

        /// <summary>
        /// Triangle cover counts of the wall's 3D model.
        /// </summary>
        /// Index n corresponds to the triangle defined by the 3D model's
        /// index list members 3n, 3n+1 and 3n+2. A positive cover count signifies
        /// the number of index map points covered by the triangle that still 
        /// need to be deleted before the triangle is erased from the 3D model.
        /// A negative cover count marks a deleted triangle.
        private int[] _triangleCovers;

        private RemoveTriangleDelegate _removeTriangle;

        public int Width { get { return _indexMap.GetLength(1); } }
        public int Height { get { return _indexMap.GetLength(0); } }

        public static BasicEffect CreateIndexMapEffect(GraphicsDevice gfx)
        {
            var effect = new BasicEffect(gfx, null);
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = false;
            effect.TextureEnabled = false;
            effect.View = Matrix.CreateLookAt(new Vector3(0, 0, 1000), Vector3.Zero, Vector3.Up);
            return effect;
        }

        public WallIndexMap(int[,][] indexMap)
        {
            _indexMap = indexMap;
            var subArrays = indexMap.Cast<int[]>().Where(arr => arr != null);
            int triangleCount = subArrays.Any() ? subArrays.Max(arr => arr.Max()) + 1 : 0;
            _triangleCovers = CreateTriangleCovers(triangleCount, indexMap);
        }

        public WallIndexMap(RemoveTriangleDelegate removeTriangle, BasicEffect indexMapEffect,
            VertexPositionNormalTexture[] vertexData, short[] indexData, AWRectangle boundingBox)
        {
            _removeTriangle = removeTriangle;
            Initialize(indexMapEffect, vertexData, indexData, boundingBox);
        }

        public void Remove(int x, int y)
        {
            if (_indexMap[y, x] == null) return;
            foreach (int index in _indexMap[y, x])
                if (--_triangleCovers[index] == 0) _removeTriangle(index);
        }

        private static int[] CreateTriangleCovers(int triangleCount, int[,][] indexMap)
        {
            var triangleCovers = new int[triangleCount];
            foreach (int[] indices in indexMap)
                if (indices != null)
                    foreach (int index in indices) ++triangleCovers[index];
            return triangleCovers;
        }

        private void Initialize(BasicEffect indexMapEffect, VertexPositionNormalTexture[] vertexData,
            short[] indexData, AWRectangle boundingArea)
        {
            var modelMin = boundingArea.Min;
            var modelDim = boundingArea.Dimensions;

            // Create an index map for the model.
            // The mask is initialised by a render of the 3D model by the graphics card.
            _indexMap = new int[(int)Math.Ceiling(modelDim.Y) + 1, (int)Math.Ceiling(modelDim.X) + 1][];
            WallToIndexMapTransform = Matrix.CreateTranslation(-modelMin.X, -modelMin.Y, 0);

            // Create colour-coded vertices for each triangle.
            var colouredVertexData = new VertexPositionColor[indexData.Length];
            for (int indexI = 0; indexI < indexData.Length; ++indexI)
            {
                var originalVertex = vertexData[indexData[indexI]];
                var color = new Color((byte)((indexI / 3) % 256), (byte)((indexI / 3 / 256) % 256), (byte)((indexI / 3 / 256 / 256) % 256));
                colouredVertexData[indexI] = new VertexPositionColor(originalVertex.Position, color);
            }

            // Draw the colour-coded triangles on our own render target for
            // index map initialisation. Render target will be a square with
            // size ('targetSize') a power of two to meet the demands of some
            // graphics devices. If the model dimensions are larger than 
            // 'targetSize', we will have to render the coloured triangles in pieces.

            // This method is run usually in a background thread -- during arena initialisation.
            // Therefore we have to tell the main draw routines to let us use the device in peace.
            // We break out of the lock regularly to allow others use the device, too.
            var gfx = AssaultWing.Instance.GraphicsDevice;
            RenderTarget2D maskTarget = null;
            int targetSize = -1;
            lock (gfx) CreateMaskTarget(out maskTarget, out targetSize);

            // Set up the effect.
            indexMapEffect.Projection = Matrix.CreateOrthographicOffCenter(0, targetSize - 1, 0, targetSize - 1, 10, 1000);
            indexMapEffect.World = WallToIndexMapTransform;

            // Draw the coloured triangles in as many parts as necessary to cover 
            // the whole model with one unit in world coordinates corresponding to
            // one pixel width in the render target.
            for (int startY = 0; startY < Height; startY += targetSize)
                for (int startX = 0; startX < Width; )
                    try
                    {
                        lock (gfx) ComputeIndexMapFragment(indexMapEffect, colouredVertexData, maskTarget, targetSize, startY, startX);
                        startX += targetSize;
                        System.Threading.Thread.Sleep(0);
                    }
                    // Some exceptions may be thrown if the graphics card is reset e.g.
                    // by a window resize. Just retry.
                    catch (NullReferenceException) { }
                    catch (InvalidOperationException) { }

            _triangleCovers = CreateTriangleCovers(indexData.Length / 3, _indexMap);
            ForceVerySmallTrianglesIntoIndexMap(vertexData, indexData);
        }

        private void ForceVerySmallTrianglesIntoIndexMap(VertexPositionNormalTexture[] vertexData, short[] indexData)
        {
            bool indexMapChanged = false;
            for (int i = 0; i < _triangleCovers.Length; ++i)
                if (_triangleCovers[i] == 0)
                {
                    indexMapChanged = true;
                    var vert0 = vertexData[indexData[3 * i + 0]].Position;
                    var vert1 = vertexData[indexData[3 * i + 1]].Position;
                    var vert2 = vertexData[indexData[3 * i + 2]].Position;
                    var triangleCenter = (vert0 + vert1 + vert2) / 3;
                    var centerInIndexMap = Vector3.Transform(triangleCenter, WallToIndexMapTransform);
                    int centerInIndexMapX = (int)(Math.Round(centerInIndexMap.X) + 0.1);
                    int centerInIndexMapY = (int)(Math.Round(centerInIndexMap.Y) + 0.1);
                    var oldIndices = _indexMap[centerInIndexMapY, centerInIndexMapX];
                    int[] newIndices = null;
                    if (oldIndices != null)
                    {
                        newIndices = new int[oldIndices.Length + 1];
                        Array.Copy(oldIndices, newIndices, oldIndices.Length);
                        newIndices[oldIndices.Length] = i;
                    }
                    else
                        newIndices = new int[] { i };
                    _indexMap[centerInIndexMapY, centerInIndexMapX] = newIndices;
                }
            if (indexMapChanged) _triangleCovers = CreateTriangleCovers(indexData.Length / 3, _indexMap);
        }

        private void CreateMaskTarget(out RenderTarget2D maskTarget, out int targetSize)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;
            var gfxCaps = gfx.GraphicsDeviceCapabilities;
            var gfxAdapter = gfx.CreationParameters.Adapter;
            if (!gfxAdapter.CheckDeviceFormat(DeviceType.Hardware, gfx.DisplayMode.Format,
                TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, SurfaceFormat.Color))
                throw new ApplicationException("Cannot create render target of type SurfaceFormat.Color");
            targetSize = Math.Min(
                AWMathHelper.FloorPowerTwo(Math.Min(gfxCaps.MaxTextureHeight, gfxCaps.MaxTextureWidth)),
                AWMathHelper.CeilingPowerTwo(Math.Max(Width, Height)));
            maskTarget = null;
            while (maskTarget == null)
                try
                {
                    maskTarget = new RenderTarget2D(gfx, targetSize, targetSize, 1, SurfaceFormat.Color);
                }
                catch (OutOfVideoMemoryException)
                {
                    targetSize /= 2;
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Cannot create render target for index map creation", e);
                }
        }

        private void ComputeIndexMapFragment(BasicEffect indexMapEffect, VertexPositionColor[] colouredVertexData,
            RenderTarget2D maskTarget, int targetSize, int startY, int startX)
        {
            var gfx = AssaultWing.Instance.GraphicsDevice;

            // Set up graphics device.
            var oldVertexDeclaration = gfx.VertexDeclaration;
            var oldDepthStencilBuffer = gfx.DepthStencilBuffer;
            gfx.VertexDeclaration = new VertexDeclaration(gfx, VertexPositionColor.VertexElements);
            gfx.DepthStencilBuffer = null;

            // Move view to current start coordinates.
            indexMapEffect.View = Matrix.CreateLookAt(new Vector3(startX, startY, 1000), new Vector3(startX, startY, 0), Vector3.Up);

            // Set and clear our own render target.
            gfx.SetRenderTarget(0, maskTarget);
            gfx.Clear(ClearOptions.Target, Color.White, 0, 0);

            // Draw the coloured triangles.
            indexMapEffect.Begin();
            foreach (EffectPass pass in indexMapEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                gfx.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList,
                    colouredVertexData, 0, colouredVertexData.Length / 3);
                pass.End();
            }
            indexMapEffect.End();

            // Restore render target so what we can extract drawn pixels.
            gfx.SetRenderTarget(0, null);

            // Figure out mask data from the render target.
            Texture2D maskTexture = maskTarget.GetTexture();
            Color[] maskData = new Color[targetSize * targetSize];
            maskTexture.GetData<Color>(maskData);
            for (int y = 0; y < targetSize; ++y)
                for (int x = 0; x < targetSize; ++x)
                {
                    Color color = maskData[x + y * maskTexture.Width];
                    if (color == Color.White) continue;
                    int indexMapY = startY + targetSize - 1 - y;
                    int indexMapX = startX + x;
                    if (indexMapY >= Height || indexMapX >= Width)
                        throw new IndexOutOfRangeException(string.Format("Index map overflow (x={0}, y={1}), color={2}", indexMapX, indexMapY, color));
                    int maskValue = color.R + color.G * 256 + color.B * 256 * 256;
                    _indexMap[indexMapY, indexMapX] = new int[] { maskValue };
                }

            // Restore graphics device's old settings.
            gfx.VertexDeclaration = oldVertexDeclaration;
            gfx.DepthStencilBuffer = oldDepthStencilBuffer;
            maskTarget.Dispose();
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;
using AW2.Helpers.Geometric;

namespace AW2.Helpers
{
    [TestFixture]
    public class Graphics3DTest
    {
        [Test]
        public void TestGetOutline()
        {
            var z1 = Vector3.UnitZ;
            var z2 = Vector3.Normalize(new Vector3(0, 1, 2));
            var c = Vector2.Zero;
            var q12d = new Vector2(20f, 20f);
            var q22d = new Vector2(40f, 20f);
            var q32d = new Vector2(30f, 40f);
            var q42d = new Vector2(50f, 40f);
            var q1 = new Vector3(q12d, 0f);
            var q2 = new Vector3(q22d, 0f);
            var q3 = new Vector3(q32d, 0f);
            var q4 = new Vector3(q42d, 0f);

            var vertexData1 = new[]
            {
                new VertexPositionNormalTexture(q1,z1,c), // 0
                new VertexPositionNormalTexture(q2,z1,c), // 1
                new VertexPositionNormalTexture(q3,z1,c), // 2
                new VertexPositionNormalTexture(q4,z1,c), // 3
            };
            var indexData1 = new short[]
            {
                0,2,1,
                2,1,3,
            };
            var vertexData2 = new[]
            {
                new VertexPositionNormalTexture(q1,z1,c), // 0
                new VertexPositionNormalTexture(q2,z1,c), // 1
                new VertexPositionNormalTexture(q3,z1,c), // 2
                new VertexPositionNormalTexture(q4,z1,c), // 3
                new VertexPositionNormalTexture(q2,z2,c), // 4
                new VertexPositionNormalTexture(q3,z2,c), // 5
            };
            var indexData2 = new short[]
            {
                0,2,1,
                2,1,3,
            };

            var poly1 = Graphics3D.GetOutline(vertexData1, indexData1);
            var poly1Expected = new Polygon(new Vector2[] { q12d, q22d, q42d, q32d });
            Assert.IsTrue(poly1Expected.Equals(poly1));

            var poly2 = Graphics3D.GetOutline(vertexData2, indexData2);
            var poly2Expected = new Polygon(new Vector2[] { q12d, q22d, q42d, q32d });
            Assert.IsTrue(poly2Expected.Equals(poly2));
        }
    }
}

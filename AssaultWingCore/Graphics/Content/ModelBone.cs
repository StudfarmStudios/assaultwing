using Microsoft.Xna.Framework;

namespace AW2.Graphics.Content
{
    public class ModelBone
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public Matrix Transform { get; set; }
        public ModelBone Parent { get; set; }
        public ModelBone[] Children { get; set; }
    }
}

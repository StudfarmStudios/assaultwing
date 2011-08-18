using System;
using Microsoft.Xna.Framework;

namespace AW2.Graphics.Content
{
    public class ModelGeometry
    {
        public ModelBone RootBone { get; set; }
        public ModelBone[] Bones { get; set; }
        public ModelMesh[] Meshes { get; set; }

        public void CopyAbsoluteBoneTransformsTo(Matrix[] transforms)
        {
            if (transforms == null) throw new ArgumentNullException("transforms");
            if (transforms.Length < Bones.Length) throw new ArgumentException("Array too short", "transforms");
            CopyAbsoluteBoneTransformTo(RootBone, Matrix.Identity, transforms);
        }

        private void CopyAbsoluteBoneTransformTo(ModelBone bone, Matrix parentTransform, Matrix[] transforms)
        {
            var transform = bone.Transform * parentTransform;
            transforms[bone.Index] = transform;
            foreach (var child in bone.Children) CopyAbsoluteBoneTransformTo(child, transform, transforms);
        }
    }
}

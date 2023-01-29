namespace AW2.Graphics.Content
{
    public class ModelMesh
    {
        public string Name { get; set; }
        public ModelBone ParentBone { get; set; }
        public ModelMeshPart[] MeshParts { get; set; }
        public object Tag { get; set; }
    }
}

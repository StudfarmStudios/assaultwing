namespace AW2.Graphics.Content
{
    public class ModelMeshPart
    {
        public int VertexOffset { get; set; }
        public int NumVertices { get; set; }
        public int StartIndex { get; set; }
        public int PrimitiveCount { get; set; }
        public object Tag { get; set; }
        public IndexBuffer IndexBuffer { get; set; }
        public VertexBuffer VertexBuffer { get; set; }
    }
}

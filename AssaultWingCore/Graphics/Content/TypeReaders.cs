using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Graphics.Content
{
    public static class TypeReaders
    {
        public class ReaderMissingException : Exception
        {
            public ReaderMissingException(string typeReaderName) : base("No such type reader, " + typeReaderName) { }
        }

        private class ModelMeshPartTagHelper
        {
            public object OriginalTag { get; set; }
            public int VertexBufferID { get; set; }
            public int IndexBufferID { get; set; }
            public int EffectID { get; set; }
        }

        public static object ReadObject(BinaryReader reader, List<string> typeReaders)
        {
            var typeID = reader.Read7BitEncodedInt();
            if (typeID == 0) return null;
            if (typeID < 0 || typeID > typeReaders.Count) throw new InvalidDataException("Invalid type reader " + typeID);
            var typeReaderName = typeReaders[typeID - 1];
            return InvokeStaticMethod(typeReaderName, reader, typeReaders);
        }

        public static void InsertSharedReferences(object obj, object[] sharedObjects)
        {
            var interpreterName = obj.GetType().Name + "FinalPass";
            InvokeStaticMethod(interpreterName, obj, sharedObjects);
        }

        private static T InterpretReference<T>(T[] pool, int reference) where T : class
        {
            if (reference == 0) return null;
            var value = pool[reference - 1];
            if (value == null)
                return null; // TODO: PETER: What is missing here? throw new InvalidDataException("Missing shared reference");
            return value;
        }

        private static object InvokeStaticMethod(string methodName, params object[] parameters)
        {
            var typeReader = typeof(TypeReaders).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            if (typeReader == null) throw new ReaderMissingException(methodName);
            return typeReader.Invoke(null, parameters);
        }

        private static string StringReader(BinaryReader reader, List<string> typeReaders)
        {
            return reader.ReadString();
        }

        private static IndexBuffer IndexBufferReader(BinaryReader reader, List<string> typeReaders)
        {
            // This reader supports only 16-bit index buffers.
            reader.AssertBoolean("32-bit index buffers not supported", true);
            var dataSize = reader.ReadUInt32();
            if (dataSize % 2 != 0) throw new InvalidDataException("Odd index buffer data size, " + dataSize);
            var indexCount = dataSize / 2;
            var indexData = new short[indexCount];
            for (int i = 0; i < indexCount; i++) indexData[i] = reader.ReadInt16();
            return new IndexBuffer { Indices = indexData };
        }

        private static VertexBuffer VertexBufferReader(BinaryReader reader, List<string> typeReaders)
        {
            // This reader supports only the VertexPositionNormalTexture vertex declaration.
            reader.AssertInt32s("Unexpected vertex declaration", 32, 3);
            reader.AssertInt32s("Unexpected vertex declaration element #1", 0, 2, 0, 0);
            reader.AssertInt32s("Unexpected vertex declaration element #2", 12, 2, 3, 0);
            reader.AssertInt32s("Unexpected vertex declaration element #3", 24, 1, 2, 0);
            var vertexCount = (int)reader.ReadUInt32();
            var vertexData = new VertexPositionNormalTexture[vertexCount];
            for (int i = 0; i < vertexCount; i++) vertexData[i] = reader.ReadVertexPositionNormalTexture();
            return new VertexBuffer { Vertices = vertexData };
        }

        private static ModelGeometry ModelReader(BinaryReader reader, List<string> typeReaders)
        {
            var boneCount = reader.ReadUInt32();
            var modelBones = new ModelBone[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var name = (string)TypeReaders.ReadObject(reader, typeReaders);
                var matrix = reader.ReadMatrix();
                modelBones[i] = new ModelBone { Name = name, Transform = matrix, Index = i };
            }
            Func<int> readBoneReference = () => boneCount < 255 ? reader.ReadByte() : (int)reader.ReadUInt32();
            for (int i = 0; i < boneCount; i++)
            {
                var boneReference = readBoneReference();
                var childBoneCount = reader.ReadUInt32();
                var childBones = new int[childBoneCount];
                for (uint j = 0; j < childBoneCount; j++) childBones[j] = readBoneReference();
                modelBones[i].Children = childBones.Select(bone => InterpretReference(modelBones, bone)).ToArray();
                foreach (var child in modelBones[i].Children) child.Parent = modelBones[i];
            }
            var meshCount = reader.ReadUInt32();
            var meshes = new ModelMesh[meshCount];
            for (int meshI = 0; meshI < meshCount; meshI++)
            {
                var meshName = (string)TypeReaders.ReadObject(reader, typeReaders);
                var parentBone = readBoneReference();
                var bounds = reader.ReadBoundingSphere();
                var meshTag = TypeReaders.ReadObject(reader, typeReaders);
                var meshPartCount = reader.ReadUInt32();
                var meshParts = new ModelMeshPart[meshPartCount];
                for (int meshPartI = 0; meshPartI < meshPartCount; meshPartI++)
                {
                    var vertexOffset = reader.ReadUInt32();
                    var numVertices = reader.ReadUInt32();
                    var startIndex = reader.ReadUInt32();
                    var primitiveCount = reader.ReadUInt32();
                    var meshPartTag = TypeReaders.ReadObject(reader, typeReaders);
                    var vertexBufferID = reader.Read7BitEncodedInt(); // shared resource
                    var indexbufferID = reader.Read7BitEncodedInt(); // shared resource
                    var effectID = reader.Read7BitEncodedInt(); // shared resource
                    meshParts[meshPartI] = new ModelMeshPart
                    {
                        VertexOffset = (int)vertexOffset,
                        NumVertices = (int)numVertices,
                        StartIndex = (int)startIndex,
                        PrimitiveCount = (int)primitiveCount,
                        Tag = new ModelMeshPartTagHelper
                        {
                            OriginalTag = meshPartTag,
                            VertexBufferID = vertexBufferID,
                            IndexBufferID = indexbufferID,
                            EffectID = effectID
                        },
                    };
                }
                meshes[meshI] = new ModelMesh
                {
                    Name = meshName,
                    ParentBone = InterpretReference(modelBones, parentBone),
                    MeshParts = meshParts,
                    Tag = meshTag,
                };
            }

            var modelRootBone = readBoneReference();
            var modelTag = TypeReaders.ReadObject(reader, typeReaders);
            return new ModelGeometry
            {
                RootBone = InterpretReference(modelBones, modelRootBone),
                Bones = modelBones,
                Meshes = meshes,
            };
        }

        private static void ModelGeometryFinalPass(ModelGeometry model, object[] sharedObjects)
        {
            foreach (var mesh in model.Meshes)
                foreach (var meshPart in mesh.MeshParts)
                {
                    var tag = (ModelMeshPartTagHelper)meshPart.Tag;
                    meshPart.Tag = tag.OriginalTag;
                    meshPart.IndexBuffer = (IndexBuffer)InterpretReference(sharedObjects, tag.IndexBufferID);
                    meshPart.VertexBuffer = (VertexBuffer)InterpretReference(sharedObjects, tag.VertexBufferID);
                }
        }
    }
}

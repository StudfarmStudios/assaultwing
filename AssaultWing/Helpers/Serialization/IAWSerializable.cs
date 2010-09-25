using System.IO;

namespace AW2.Helpers.Serialization
{
    public interface IAWSerializable
    {
        void Serialize(BinaryWriter writer);
    }
}

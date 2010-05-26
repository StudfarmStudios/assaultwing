using System.IO;

namespace AW2.Helpers
{
    public interface IAWSerializable
    {
        void Serialize(BinaryWriter writer);
    }
}

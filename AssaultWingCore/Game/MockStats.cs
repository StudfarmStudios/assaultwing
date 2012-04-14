using AW2.Helpers.Serialization;

namespace AW2.Game
{
    public class MockStats : INetworkSerializable
    {
        public void Serialize(NetworkBinaryWriter writer, SerializationModeFlags mode)
        {
            throw new System.NotImplementedException();
        }

        public void Deserialize(NetworkBinaryReader reader, SerializationModeFlags mode, int framesAgo)
        {
            throw new System.NotImplementedException();
        }
    }
}

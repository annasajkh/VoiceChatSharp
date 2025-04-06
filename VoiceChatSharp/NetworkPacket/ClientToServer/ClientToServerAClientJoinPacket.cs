using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkCommunicationData.Client
{
    /// <summary>
    /// This packet is sended when the client want to join a server
    /// </summary>
    public struct ClientToServerAClientJoinPacket : INetSerializable
    {
        public string Name { get; set; }
        public bool Muted { get; set; }
        public bool Deafened { get; set; }
        public byte Volume { get; set; }

        public ClientToServerAClientJoinPacket(string name, bool muted, bool deafened, byte volume)
        {
            Name = name;
            Muted = muted;
            Deafened = deafened;
            Volume = volume;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Name);
            writer.Put(Muted);
            writer.Put(Deafened);
            writer.Put(Volume);
        }

        public void Deserialize(NetDataReader reader)
        {
            Name = reader.GetString();
            Muted = reader.GetBool();
            Deafened = reader.GetBool();
            Volume = reader.GetByte();
        }
    }
}
using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkPacket.ServerToClient
{
    public struct ServerToClientEncodedAudioPacket : INetSerializable
    {
        public int ID { get; set; }
        public byte[] Data { get; set; }

        public ServerToClientEncodedAudioPacket(int id, byte[] data)
        {
            ID = id;
            Data = data;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ID);
            writer.Put(Data);
        }

        public void Deserialize(NetDataReader reader)
        {
            ID = reader.GetInt();
            Data = reader.GetRemainingBytes();
        }
    }
}

using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkPacket.ClientToServer
{
    public struct ClientToServerEncodedAudioPacket : INetSerializable
    {
        public byte[] Data { get; set; }

        public ClientToServerEncodedAudioPacket(byte[] data)
        {
            Data = data;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Data);
        }

        public void Deserialize(NetDataReader reader)
        {
            Data = reader.GetRemainingBytes();
        }
    }
}

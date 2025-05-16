using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkPacket.ClientToServer
{
    public struct ClientToServerEncodedAudioPacket : INetSerializable
    {
        /// <summary>
        /// The time the packet is created in ms
        /// </summary>
        public long PacketTimeMS { get; set; }

        /// <summary>
        /// The encoded data
        /// </summary>
        public byte[] Data { get; set; }

        public ClientToServerEncodedAudioPacket(long packetTimeMS, byte[] data)
        {
            PacketTimeMS = packetTimeMS;
            Data = data;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PacketTimeMS);
            writer.Put(Data);
        }

        public void Deserialize(NetDataReader reader)
        {
            PacketTimeMS = reader.GetLong();
            Data = reader.GetRemainingBytes();
        }
    }
}

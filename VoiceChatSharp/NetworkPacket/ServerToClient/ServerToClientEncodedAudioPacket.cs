using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkPacket.ServerToClient;

public struct ServerToClientEncodedAudioPacket : INetSerializable
{
    public int ID { get; set; }

    /// <summary>
    /// The time the packet is created in ms
    /// </summary>
    public long PacketTimeMS { get; set; }

    /// <summary>
    /// The encoded data
    /// </summary>
    public byte[] Data { get; set; }

    public ServerToClientEncodedAudioPacket(int id, long packetTimeMS, byte[] data)
    {
        ID = id;
        PacketTimeMS = packetTimeMS;
        Data = data;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
        writer.Put(PacketTimeMS);
        writer.Put(Data);
    }

    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
        PacketTimeMS = reader.GetLong();
        Data = reader.GetRemainingBytes();
    }
}

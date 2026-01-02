using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkPacket.ServerToClient;

public struct ServerToClientEncodedAudioPacket : INetSerializable
{
    public int ID { get; set; }

    /// <summary>
    /// The date when the packet is created
    /// </summary>
    public long CreationDate { get; set; }

    /// <summary>
    /// The encoded data
    /// </summary>
    public byte[] Data { get; set; }

    public ServerToClientEncodedAudioPacket(int id, long creationDate, byte[] data)
    {
        ID = id;
        CreationDate = creationDate;
        Data = data;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
        writer.Put(CreationDate);
        writer.Put(Data);
    }

    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
        CreationDate = reader.GetLong();
        Data = reader.GetRemainingBytes();
    }
}

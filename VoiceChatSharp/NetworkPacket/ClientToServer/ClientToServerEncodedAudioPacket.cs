using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkPacket.ClientToServer;

public struct ClientToServerEncodedAudioPacket : INetSerializable
{
    /// <summary>
    /// The date when the packet is created
    /// </summary>
    public long CreationDate { get; set; }

    /// <summary>
    /// The encoded data
    /// </summary>
    public byte[] Data { get; set; }

    public ClientToServerEncodedAudioPacket(long creationDate, byte[] data)
    {
        CreationDate = creationDate;
        Data = data;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CreationDate);
        writer.Put(Data);
    }

    public void Deserialize(NetDataReader reader)
    {
        CreationDate = reader.GetLong();
        Data = reader.GetRemainingBytes();
    }
}

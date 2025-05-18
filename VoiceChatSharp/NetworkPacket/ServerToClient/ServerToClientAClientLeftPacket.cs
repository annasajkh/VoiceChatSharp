using LiteNetLib.Utils;

namespace VoiceChatSharp.NetworkPacket.ServerToClient;

public struct ServerToClientAClientLeftPacket : INetSerializable
{
    /// <summary>
    /// The client id
    /// </summary>
    public int ID { get; set; }

    public ServerToClientAClientLeftPacket(int id)
    {
        ID = id;
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
    }

    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
    }
}
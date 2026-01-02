namespace VoiceChatSharp.NetworkStorageData.Shared;

public struct EncodedAudioPacket
{
    /// <summary>
    /// The encoded data
    /// </summary>
    public byte[] Data { get; private set; }

    /// <summary>
    /// The time the packet was created in ms
    /// </summary>
    public long CreationDate { get; private set; }

    public EncodedAudioPacket(long creationDate, byte[] data)
    {
        CreationDate = creationDate;
        Data = data;
    }
}

namespace VoiceChatSharp.NetworkStorageData.Shared;

/// <summary>
/// The Data of the client
/// </summary>
public struct ClientData
{
    public string Name { get; set; }
    public bool Muted { get; set; }
    public bool Deafened { get; set; }
    public byte Volume { get; set; }

    /// <summary>
    /// The client id
    /// </summary>
    public int ID { get; private set; }

    public ClientData(string name, bool muted, bool deafened, byte volume, int id)
    {
        Name = name;
        Muted = muted;
        Deafened = deafened;
        Volume = volume;
        ID = id;
    }
}

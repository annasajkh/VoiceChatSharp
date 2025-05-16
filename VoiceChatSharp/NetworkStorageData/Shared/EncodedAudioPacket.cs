namespace VoiceChatSharp.NetworkStorageData.Shared
{
    public struct EncodedAudioPacket
    {
        /// <summary>
        /// The encoded data
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// The time the packet was created in ms
        /// </summary>
        public long PacketTimeMS { get; private set; }

        public EncodedAudioPacket(long packetTimeMS, byte[] data)
        {
            PacketTimeMS = packetTimeMS;
            Data = data;
        }
    }
}

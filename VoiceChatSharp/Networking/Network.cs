using LiteNetLib.Utils;
using LiteNetLib;
using VoiceChatSharp.NetworkCommunicationData.Client;
using VoiceChatSharp.Utils;
using VoiceChatSharp.NetworkPacket.ServerToClient;
using VoiceChatSharp.NetworkCommunicationPacket.ClientToServer;

namespace VoiceChatSharp.Networking
{
    public abstract class Network
    {
        public EventBasedNetListener Listener { get; private set; }
        public NetManager NetManager { get; private set; }
        public NetPacketProcessor NetPacketProcessor { get; private set; }

        private NetDataWriter netDataWriter;
        protected NetworkLogger networkLogger;

        public Network(NetworkLoggerType networkLoggerType)
        {
            Listener = new EventBasedNetListener();
            NetManager = new NetManager(Listener);
            NetPacketProcessor = new NetPacketProcessor();

            netDataWriter = new NetDataWriter();
            networkLogger = new NetworkLogger(networkLoggerType);

            NetPacketProcessor.RegisterNestedType<ClientToServerAClientJoinPacket>();
            NetPacketProcessor.RegisterNestedType<ClientToServerEncodedAudioPacket>();

            NetPacketProcessor.RegisterNestedType<ServerToClientEncodedAudioPacket>();
            NetPacketProcessor.RegisterNestedType<ServerToClientAClientJoiningPacket>();
            NetPacketProcessor.RegisterNestedType<ServerToClientAClientLeftPacket>();
        }

        public void SendPacket<T>(T packet, NetPeer peer, DeliveryMethod deliveryMethod) where T : INetSerializable
        {
            netDataWriter.Reset();
            NetPacketProcessor.WriteNetSerializable(netDataWriter, ref packet);
            peer.Send(netDataWriter, deliveryMethod);
        }

        public virtual void Update()
        {
            NetManager.PollEvents();
        }

        public void Stop()
        {
            NetManager.Stop();
        }
    }

}
using LiteNetLib;
using LiteNetLib.Utils;
using VoiceChatSharp.NetworkPacket.ClientToServer;
using VoiceChatSharp.NetworkPacket.ServerToClient;
using VoiceChatSharp.NetworkStorageData.Shared;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.Networking;

public class VoiceChatServer : Network
{
    Dictionary<int, ClientData> clients = new();
    public string Key { get; private set; }

    public VoiceChatServer(string key) : base(NetworkLoggerType.Server)
    {
        Key = key;

        Listener.ConnectionRequestEvent += OnConnectionRequest;
        Listener.NetworkReceiveEvent += OnNetworkReceive;
        Listener.PeerDisconnectedEvent += OnPeerDisconnected;
        Listener.PeerConnectedEvent += OnPeerConnected;

        NetPacketProcessor.SubscribeNetSerializable<ClientToServerAClientJoinPacket, NetPeer>(OnClientToServerAClientJoiningPacket);
        NetPacketProcessor.SubscribeNetSerializable<ClientToServerEncodedAudioPacket, NetPeer>(OnClientToServerEncodedAudioPacket);
    }

    public void OnClientToServerAClientJoiningPacket(ClientToServerAClientJoinPacket clientJoiningPacket, NetPeer peer)
    {
        if (clients.ContainsKey(peer.Id))
        {
            networkLogger.LogError($"client with peer id {peer.Id} are already in the server");
            return;
        }

        networkLogger.LogInfo($"{clientJoiningPacket.Name} Joined");

        clients.Add(peer.Id, new ClientData(clientJoiningPacket.Name, clientJoiningPacket.Muted, clientJoiningPacket.Deafened, clientJoiningPacket.Volume, peer.Id));

        // Joining Flow 2
        networkLogger.LogInfo($"Sending joined packet response to {clientJoiningPacket.Name}");
        SendPacket(new ServerToClientAClientJoiningPacket(), peer, DeliveryMethod.ReliableOrdered);
    }

    public void OnClientToServerEncodedAudioPacket(ClientToServerEncodedAudioPacket clientEncodedAudioPacket, NetPeer peer)
    {
        // Sending Encoded Audio Flow 2
        foreach (NetPeer connectedPeer in NetManager.ConnectedPeerList)
        {
            if (peer.Id != connectedPeer.Id)
            {
                SendPacket(new ServerToClientEncodedAudioPacket(peer.Id, clientEncodedAudioPacket.PacketTimeMS, clientEncodedAudioPacket.Data), connectedPeer, DeliveryMethod.ReliableOrdered);
            }
        }
    }

    public void Start(int port)
    {
        NetManager.Start(port);
        networkLogger.LogInfo($"Server started at 127.0.0.1 with port {port}");
    }

    private void OnConnectionRequest(ConnectionRequest connectionRequest)
    {
        connectionRequest.AcceptIfKey(Key);
    }

    private void OnPeerConnected(NetPeer peer)
    {
        networkLogger.LogInfo("A client connected");
    }

    private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        NetPacketProcessor.ReadAllPackets(reader, peer);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (!clients.ContainsKey(peer.Id))
        {
            networkLogger.LogError($"clients dictionary doesn't contain peer ID of {peer.Id} something wrong is going on");
            return;
        }

        networkLogger.LogInfo($"Client with id {clients[peer.Id]} is disconnected reason: {disconnectInfo.Reason}");

        clients.Remove(peer.Id);

        // When a client left notify all other clients about it
        foreach (NetPeer connectedPeer in NetManager.ConnectedPeerList)
        {
            if (peer.Id != connectedPeer.Id)
            {
                SendPacket(new ServerToClientAClientLeftPacket(peer.Id), connectedPeer, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}
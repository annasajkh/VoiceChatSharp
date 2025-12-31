using LiteNetLib;
using LiteNetLib.Utils;
using System.Timers;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.NetworkPacket.ClientToServer;
using VoiceChatSharp.NetworkPacket.ServerToClient;
using VoiceChatSharp.Utils;
using VoiceChatSharp.VoiceChat;
using Timer = System.Timers.Timer;

namespace VoiceChatSharp.Networking;

public class VoiceChatClient<T> : Network, IDisposable where T : VoiceChatAudioSourceInterface, new()
{
    public NetDataWriter NetDataWriter { get; private set; } = new();
    public bool IsInServer { get; private set; }

    public VoiceChatRecorder VoiceChatRecorder { get; private set; }
    public VoiceChatPlayer VoiceChatPlayer { get; private set; }

    NetPeer? serverPeer;

    public string Name { get; private set; }
    public bool Muted { get; private set; }
    public bool Deafened { get; private set; }
    public byte Volume { get; private set; }

    double joiningAttemptTime = 500;
    int joiningAttempt = 6;
    Timer joiningAttemptTimer;
    bool isDisposed;

    public VoiceChatClient(VoiceChatRecorder voiceChatRecorder, VoiceChatPlayer voiceChatPlayer, string name, bool muted = false, bool deafened = false, byte volume = 100) : base(NetworkLoggerType.Client)
    {
        Name = name;
        Muted = muted;
        Deafened = deafened;
        Volume = volume;

        VoiceChatRecorder = voiceChatRecorder;
        VoiceChatPlayer = voiceChatPlayer;

        joiningAttemptTimer = new Timer(100);

        networkLogger = new NetworkLogger(NetworkLoggerType.Client);

        NetPacketProcessor.SubscribeNetSerializable<ServerToClientAClientJoiningPacket>(OnServerToClientAClientJoiningPacket);
        NetPacketProcessor.SubscribeNetSerializable<ServerToClientEncodedAudioPacket>(OnServerToClientEncodedAudioPacket);
        NetPacketProcessor.SubscribeNetSerializable<ServerToClientAClientLeftPacket>(OnServerToClientAClientLeftPacket);

        Listener.NetworkReceiveEvent += OnNetworkReceive;
        Listener.PeerDisconnectedEvent += OnPeerDisconnected;

        joiningAttemptTimer.Elapsed += (object? source, ElapsedEventArgs elapsedEventArgs) =>
        {
            if (serverPeer is null)
            {
                networkLogger.LogError("Error server peer is null");
                return;
            }

            ClientToServerAClientJoinPacket clientToServerJoiningPacket = new ClientToServerAClientJoinPacket(name, Muted, Deafened, Volume);

            // Joining Flow 1
            networkLogger.LogInfo($"Attempting to join to the server with an address of {serverPeer}");
            SendPacket(clientToServerJoiningPacket, serverPeer, DeliveryMethod.ReliableOrdered);

            joiningAttempt--;

            if (joiningAttempt == 0)
            {
                joiningAttemptTimer.Stop();
                joiningAttemptTime = 500;
                joiningAttempt = 6;
                return;
            }

            joiningAttemptTime += 500;
        };

        joiningAttemptTimer.AutoReset = true;
    }

    public void Join(string address, int port, string key)
    {
        NetManager.Start();

        NetPeer serverPeerTemp = NetManager.Connect(address, port, key);

        if (serverPeerTemp is null)
        {
            networkLogger.LogWarning("There is already connection request awaiting of this client in the server the client is trying to connect");
            return;
        }

        if (serverPeer != null)
        {
            if (serverPeer != serverPeerTemp)
            {
                networkLogger.LogWarning("Already connected to different server");
                return;
            }

            if (serverPeer == serverPeerTemp)
            {
                networkLogger.LogWarning("Already connected to the same server");
                return;
            }
        }

        serverPeer = serverPeerTemp;

        joiningAttemptTimer.Start();
    }

    public void Leave()
    {
        if (serverPeer is null)
        {
            networkLogger.LogError("Can't disconnect you haven't connect to any server yet");
            return;
        }

        NetManager.DisconnectPeer(serverPeer);
        IsInServer = false;
    }

    public void OnServerToClientAClientJoiningPacket(ServerToClientAClientJoiningPacket serverToClientJoinedPacket)
    {
        //Joining Flow 3
        networkLogger.LogInfo($"Successfully joined the server");

        VoiceChatRecorder.StartRecording();
        VoiceChatPlayer.Play();
        IsInServer = true;
        joiningAttemptTimer.Stop();
    }

    public void OnServerToClientAClientLeftPacket(ServerToClientAClientLeftPacket serverToClientAClientLeavedPacket)
    {
        if (VoiceChatPlayer.ContainsVoiceChatAudioSource(serverToClientAClientLeavedPacket.ID))
        {
            networkLogger.LogInfo($"Client with id {serverToClientAClientLeavedPacket.ID} left");
            VoiceChatPlayer.RemoveVoiceChatAudioSource(serverToClientAClientLeavedPacket.ID);
        }
        else
        {
            networkLogger.LogWarning("Cannot find client that disconnect locally there is a dync");
        }
    }

    public void OnServerToClientEncodedAudioPacket(ServerToClientEncodedAudioPacket serverToClientEncodedAudioPacket)
    {
        if (VoiceChatPlayer.ContainsVoiceChatAudioSource(serverToClientEncodedAudioPacket.ID))
        {
            VoiceChatPlayer.QueueEncodedAudioPacket(serverToClientEncodedAudioPacket.ID, serverToClientEncodedAudioPacket.Data);
        }
        else
        {
            VoiceChatPlayer.AddVoiceChatAudioSource<T>(serverToClientEncodedAudioPacket.ID);
            VoiceChatPlayer.PlayAudioSource(serverToClientEncodedAudioPacket.ID);
            VoiceChatPlayer.QueueEncodedAudioPacket(serverToClientEncodedAudioPacket.ID, new EncodedAudioPacket(serverToClientEncodedAudioPacket.PacketTimeMS, serverToClientEncodedAudioPacket.Data));
        }
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        if (serverPeer == peer)
        {
            IsInServer = false;
            networkLogger.LogInfo($"Disconnected from the server: {disconnectInfo.Reason}");
        }
        else
        {
            networkLogger.LogWarning("Disconnected from a server that is different than serverPeer there is a dsync");
        }
    }

    public override void Update()
    {
        if (IsInServer && serverPeer != null)
        {
            byte[]? encodedAudioPacketResult = VoiceChatRecorder.GetTheFirstEncodedAudioPacket();

            if (encodedAudioPacketResult is byte[] encodedAudioPacket)
            {
                // Sending Encoded Audio Flow 1
                SendPacket(new ClientToServerEncodedAudioPacket(encodedAudioPacket), serverPeer, DeliveryMethod.ReliableSequenced);
            }
        }

        base.Update();
    }

    private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        NetPacketProcessor.ReadAllPackets(reader);
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        VoiceChatRecorder.Dispose();
        VoiceChatPlayer.Dispose();
    }
}
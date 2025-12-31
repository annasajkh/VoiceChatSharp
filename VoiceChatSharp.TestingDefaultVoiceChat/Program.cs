using VoiceChatSharp.DefaultImplementation;
using VoiceChatSharp.NetworkStorageData.Shared;
using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.TestingVoiceChat;

internal class Program
{
    static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    static void Main(string[] args)
    {
        using VoiceChatRecorder voiceChatRecorder = new VoiceChatRecorder(new DefaultVoiceChatRecorder());
        using VoiceChatPlayer voiceChatPlayer = new VoiceChatPlayer(new DefaultVoiceChatPlayer());

        Console.WriteLine("--------------------------------------------------------------");

        List<string> recordingDeviceNames = voiceChatRecorder.GetRecordingDeviceNames();

        Console.WriteLine("List of recording audio devices: ");

        foreach (var recordingDeviceName in recordingDeviceNames)
        {
            Console.WriteLine("   " + recordingDeviceName);
        }

        Console.WriteLine("--------------------------------------------------------------");

        Console.WriteLine($"Current recording audio device: {voiceChatRecorder.GetCurrentRecordingDeviceName()}");

        Console.WriteLine("--------------------------------------------------------------");

        List<string> playbackDeviceNames = voiceChatPlayer.GetPlaybackDeviceNames();

        Console.WriteLine("List of playback audio devices: ");

        foreach (var playbackDeviceName in playbackDeviceNames)
        {
            Console.WriteLine("   " + playbackDeviceName);
        }

        Console.WriteLine("--------------------------------------------------------------");

        Console.WriteLine($"Current playback audio device: {voiceChatPlayer.GetCurrentPlaybackDeviceName()}");

        Console.WriteLine("--------------------------------------------------------------");


        voiceChatRecorder.StartRecording();


        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(0);
        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(1);
        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(2);

        voiceChatPlayer.PlayAudioSource(0);
        voiceChatPlayer.PlayAudioSource(1);
        voiceChatPlayer.PlayAudioSource(2);

        voiceChatPlayer.SetVolume(2f);

        voiceChatPlayer.Play();


        Task.Factory.StartNew(() =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                EncodedAudioPacket? encodedAudioPacketResult = voiceChatRecorder.GetTheFirstEncodedAudioPacket();

                if (encodedAudioPacketResult is EncodedAudioPacket encodedAudioPacket)
                {
                    voiceChatPlayer.QueueEncodedAudioPacket(0, new EncodedAudioPacket(encodedAudioPacket.PacketTimeMS, encodedAudioPacket.Data));
                    voiceChatPlayer.QueueEncodedAudioPacket(1, new EncodedAudioPacket(encodedAudioPacket.PacketTimeMS, encodedAudioPacket.Data));
                    voiceChatPlayer.QueueEncodedAudioPacket(2, new EncodedAudioPacket(encodedAudioPacket.PacketTimeMS, encodedAudioPacket.Data));
                    voiceChatPlayer.QueueEncodedAudioPacket(3, new EncodedAudioPacket(encodedAudioPacket.PacketTimeMS, encodedAudioPacket.Data));
                    voiceChatPlayer.QueueEncodedAudioPacket(4, new EncodedAudioPacket(encodedAudioPacket.PacketTimeMS, encodedAudioPacket.Data));
                }
            }
        }, cancellationTokenSource.Token);

        Console.WriteLine("Press enter to exit...");
        Console.Read();

        cancellationTokenSource.Cancel();
    }
}

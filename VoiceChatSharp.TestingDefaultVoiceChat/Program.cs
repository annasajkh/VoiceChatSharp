using VoiceChatSharp.DefaultImplementation;
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
        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(3);
        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(4);
        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(5);
        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(6);

        voiceChatPlayer.PlayAudioSource(0);
        voiceChatPlayer.PlayAudioSource(1);
        voiceChatPlayer.PlayAudioSource(2);
        voiceChatPlayer.PlayAudioSource(3);
        voiceChatPlayer.PlayAudioSource(4);
        voiceChatPlayer.PlayAudioSource(5);
        voiceChatPlayer.PlayAudioSource(6);

        voiceChatPlayer.Play();
        voiceChatPlayer.SetVolume(2);


        Task.Factory.StartNew(() =>
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                byte[]? encodedAudioPacketResult = voiceChatRecorder.GetTheFirstEncodedAudioPacket();

                if (encodedAudioPacketResult is byte[] encodedAudioPacket)
                {

                    voiceChatPlayer.QueueEncodedAudioPacket(0, encodedAudioPacket);
                    voiceChatPlayer.QueueEncodedAudioPacket(1, encodedAudioPacket);
                    voiceChatPlayer.QueueEncodedAudioPacket(2, encodedAudioPacket);
                    voiceChatPlayer.QueueEncodedAudioPacket(3, encodedAudioPacket);
                    voiceChatPlayer.QueueEncodedAudioPacket(4, encodedAudioPacket);
                    voiceChatPlayer.QueueEncodedAudioPacket(5, encodedAudioPacket);
                    voiceChatPlayer.QueueEncodedAudioPacket(6, encodedAudioPacket);
                }
            }
        }, cancellationTokenSource.Token);

        Console.WriteLine("Press enter to exit...");
        Console.Read();

        cancellationTokenSource.Cancel();
    }
}

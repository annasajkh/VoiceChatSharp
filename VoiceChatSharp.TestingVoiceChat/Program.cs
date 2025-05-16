using VoiceChatSharp.DefaultImplementation;
using VoiceChatSharp.NetworkStorageData.Shared;
using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.TestingVoiceChat;

internal class Program
{
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
        voiceChatPlayer.PlayAudioSource(0);

        voiceChatPlayer.Play();

        while (true)
        {
            EncodedAudioPacket? encodedAudioPacketResult = voiceChatRecorder.GetTheFirstEncodedAudioPacket();

            if (encodedAudioPacketResult is EncodedAudioPacket encodedAudioPacket)
            {
                voiceChatPlayer.QueueEncodedAudioPacket(0, new EncodedAudioPacket(encodedAudioPacket.PacketTimeMS, encodedAudioPacket.Data));
            }

            voiceChatPlayer.QueueEncodedSample(1, encodedSample);

            voiceChatPlayer.QueueEncodedSample(1, encodedSample);
        }
    }
}

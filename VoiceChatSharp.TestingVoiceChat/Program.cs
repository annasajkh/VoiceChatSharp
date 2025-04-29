using VoiceChatSharp.Core;
using VoiceChatSharp.DefaultImplementation;

namespace VoiceChatSharp.TestingVoiceChat;

internal class Program
{
    static void Main(string[] args)
    {
        Random random = new();

        using VoiceChatRecorder voiceChatRecorder = new VoiceChatRecorder(new DefaultVoiceChatRecorder());
        using VoiceChatPlayer voiceChatPlayer = new VoiceChatPlayer(new DefaultVoiceChatPlayer());

        voiceChatRecorder.StartRecording();

        voiceChatPlayer.AddVoiceChatAudioSource<DefaultVoiceChatAudioSource>(0);
        voiceChatPlayer.Play();

        List<string> audioDeviceNames = voiceChatRecorder.GetRecordingDeviceNames();

        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                Thread.Sleep(random.Next() % 100);
                voiceChatRecorder.SetCurrentRecordingDevice(audioDeviceNames[random.Next() % audioDeviceNames.Count]);
                Console.WriteLine($"Current recording device is {voiceChatRecorder.GetCurrentRecordingDeviceName()}");
            }
        });

        while (true)
        {
            byte[]? encodedSample = voiceChatRecorder.GetTheFirstEncodedSample();

            if (encodedSample is null)
            {
                continue;
            }

            Console.WriteLine(encodedSample.Length);

            //voiceChatPlayer.QueueEncodedSample(0, encodedSample);
        }
    }
}

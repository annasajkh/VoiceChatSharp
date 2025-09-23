using SoundFlow.Backends.MiniAudio;

namespace VoiceChatSharp.DefaultImplementation;


static class Global
{
    public static MiniAudioEngine AudioEngine { get; private set; } = new();
}

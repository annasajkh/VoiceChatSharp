namespace VoiceChatSharp.Utils;

public static class Helper
{
    /// <summary>
    /// Get total bytes
    /// </summary>
    /// <param name="sampleRate">The sample rate</param>
    /// <param name="frameSizeMs">The frame size in milliseconds</param>
    /// <param name="channels">The number of channels</param>
    /// <param name="bytesPerSample">The number of bytes per sample depends on the audio format</param>
    /// <returns></returns>
    public static int GetTotalBytes(int sampleRate, int frameSizeMs, int channels, int bytesPerSample)
    {
        return sampleRate * frameSizeMs / 1000 * channels * bytesPerSample;
    }
}
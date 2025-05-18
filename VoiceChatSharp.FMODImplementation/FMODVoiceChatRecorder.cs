using System;
using System.Collections.Generic;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.FMODImplementation;

public class FMODVoiceChatRecorder : VoiceChatRecorderInterface
{
    public FMODVoiceChatRecorder(int sampleRate = 48000, int channels = 2, string? recodingDevice = null) : base(sampleRate, channels, 4, recodingDevice)
    {
        if (Global.FMODSystem is null)
        {
            FMOD.RESULT initSystemResult = FMOD.Factory.System_Create(out FMOD.System system);

            if (initSystemResult != FMOD.RESULT.OK)
            {
                throw new Exception(FMOD.Error.String(initSystemResult));
            }

            Global.FMODSystem = system;

            Global.FMODSystem.Value.init(Channels, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
        }


        RefreshAudioDeviceMapping();
    }

    void RefreshAudioDeviceMapping()
    {
        int recordDriverCount;
        int recordDriverConnectedCount;

        FMOD.RESULT getRecordNumDriversResult = Global.FMODSystem!.Value.getRecordNumDrivers(out recordDriverCount, out recordDriverConnectedCount);

        if (getRecordNumDriversResult != FMOD.RESULT.OK)
        {
            throw new Exception(FMOD.Error.String(getRecordNumDriversResult));
        }

        for (int i = 0; i < recordDriverConnectedCount; i++)
        {

            Global.FMODSystem!.Value.getRecordDriverInfo(i, out string name, 256, out Guid guid, out int systemRate, out FMOD.SPEAKERMODE speakerMode, out int speakerModeChannels, out FMOD.DRIVER_STATE driveState);

            if (name.Contains("[loopback]"))
            {
                continue;
            }


        }

        Console.Read();
    }

    public override string GetCurrentRecordingDeviceName()
    {
        throw new System.NotImplementedException();
    }

    public override List<string> GetRecordingDeviceNames()
    {
        throw new System.NotImplementedException();
    }

    public override void SetCurrentRecordingDevice(string name)
    {
        throw new System.NotImplementedException();
    }

    public override void SetVolume(float volume)
    {
        throw new System.NotImplementedException();
    }

    public override void StartRecording()
    {
        throw new System.NotImplementedException();
    }

    public override void StopRecording()
    {
        throw new System.NotImplementedException();
    }
    public override void Dispose()
    {
        throw new System.NotImplementedException();
    }
}

using NAudio.Wave;

/// <summary>
/// Uses the NAudio library (https://github.com/naudio/NAudio) to provide a rudimentary abstraction to output
/// BinaryData audio segments to the default output (speaker/headphone) device.
/// </summary>
public class SpeakerOutput : IDisposable
{
    BufferedWaveProvider _waveProvider;
    WaveOutEvent _waveOutEvent;

    public SpeakerOutput(int deviceId)
    {
        WaveFormat outputAudioFormat = new(
            rate: 24000,
            bits: 16,
            channels: 1);
        _waveProvider = new(outputAudioFormat)
        {
            BufferDuration = TimeSpan.FromMinutes(2),
        };
        _waveOutEvent = new() { DeviceNumber = deviceId };
        _waveOutEvent.Init(_waveProvider);
        _waveOutEvent.Play();
    }


    public void EnqueueForPlayback(BinaryData audioData)
    {
        byte[] buffer = audioData.ToArray();
        _waveProvider.AddSamples(buffer, 0, buffer.Length);
    }

    public byte[] GetBufferedData()
    {
        // Extract buffered data for filtering
        int bytesAvailable = _waveProvider.BufferedBytes;
        byte[] buffer = new byte[bytesAvailable];
        _waveProvider.Read(buffer, 0, bytesAvailable);
        return buffer;
    }

    public void ClearPlayback()
    {
        _waveProvider.ClearBuffer();
    }

    public void Dispose()
    {
        _waveOutEvent?.Dispose();
    }
}
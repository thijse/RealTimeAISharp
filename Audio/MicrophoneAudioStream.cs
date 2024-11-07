#nullable disable
using System.Diagnostics;
using NAudio.Wave;
using static System.Runtime.InteropServices.MemoryMarshal;

// Utility class to enumerate the available audio devices. and select the default input and output devices.

namespace RealtimeInteractiveConsole;

/// <summary>
/// The class implements a stream that reads audio data from the microphone. It is special in that
/// noise gating is applied to the audio data. If the audio signal is below a certain threshold for a given time it stops adding data to the output buffer.
/// The output is a stream because the BinaryData input function in Azure.OpenAI.realtimeconverations has a big in its implementation.
/// </summary>
public class MicrophoneAudioStream : Stream, IDisposable
{
    private const int SamplesPerSecond   = 24000;
    private const int BytesPerSample     = 2;
    private const int Channels           = 1;

    // For simplicity, this is configured to use a static 10-second ring buffer.
    private readonly byte[] _buffer      = new byte[BytesPerSample * SamplesPerSecond * Channels * 10];
    private readonly object _bufferLock  = new();
    private int _bufferReadPos           = 0;
    private int _bufferWritePos          = 0;

    private readonly WaveInEvent         _waveInEvent;
    private readonly AutoResetEvent      _audioEvent = new AutoResetEvent(false);
    private readonly SimpleGate          _simpleGate;
    private readonly Stopwatch           _silenceTimer = new Stopwatch();

    private MicrophoneAudioStream(int deviceNumber)
    {
        _waveInEvent = new WaveInEvent()
        {
            DeviceNumber = deviceNumber,
            WaveFormat   = new WaveFormat(SamplesPerSecond, BytesPerSample * 8, Channels),
        };

        _simpleGate = new SimpleGate(200.0,100.0,100.0, SamplesPerSecond);
        _silenceTimer.Restart();

        _waveInEvent.DataAvailable +=  ProcessAvailableData; 
        _waveInEvent.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);
        _waveInEvent.StartRecording();
    }

    private void ProcessAvailableData(object _,WaveInEventArgs e)
    {
        // First apply the gating filter

        var conv = Cast<byte, Int16>(e.Buffer);
        var aboveThreshold = _simpleGate.Process(conv);
        if (!aboveThreshold) 
        {
            if (_silenceTimer.ElapsedMilliseconds > 5000)
            {
                // only if the signal is below the threshold for a given time, stop adding data to the buffer
                return;
            }
        }
        else
        {
            _silenceTimer.Restart();
        }

        lock (_bufferLock)
        {
            int bytesToCopy = e.BytesRecorded;
            if (_bufferWritePos + bytesToCopy >= _buffer.Length)
            {
                int bytesToCopyBeforeWrap = _buffer.Length - _bufferWritePos;
                Array.Copy(e.Buffer, 0, _buffer, _bufferWritePos, bytesToCopyBeforeWrap);
                bytesToCopy -= bytesToCopyBeforeWrap;
                _bufferWritePos = 0;
            }
            Array.Copy(e.Buffer, e.BytesRecorded - bytesToCopy, _buffer, _bufferWritePos, bytesToCopy);
            _bufferWritePos += bytesToCopy;
        }
        _audioEvent.Set();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalCount = count;

        int GetBytesAvailable() => _bufferWritePos < _bufferReadPos
            ? _bufferWritePos + (_buffer.Length - _bufferReadPos)
            : _bufferWritePos - _bufferReadPos;

        // For simplicity, we'll block until all requested data is available and not perform partial reads.
        while (GetBytesAvailable() < count)
        {
            _audioEvent.WaitOne();
        }

        lock (_bufferLock)
        {
            if (_bufferReadPos + count >= _buffer.Length)
            {
                int bytesBeforeWrap = _buffer.Length - _bufferReadPos;
                Array.Copy(
                    sourceArray: _buffer,
                    sourceIndex: _bufferReadPos,
                    destinationArray: buffer,
                    destinationIndex: offset,
                    length: bytesBeforeWrap);
                _bufferReadPos = 0;
                count -= bytesBeforeWrap;
                offset += bytesBeforeWrap;
            }

            Array.Copy(_buffer, _bufferReadPos, buffer, offset, count);
            _bufferReadPos += count;
        }
        return totalCount;
    }

    void waveSource_RecordingStopped(object sender, StoppedEventArgs e) { }

    public static MicrophoneAudioStream Start(int deviceId) => new(deviceId);

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position                                    { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()                                     { throw new NotImplementedException(); }

    public override long Seek(long offset, SeekOrigin origin)        { throw new NotImplementedException(); }

    public override void SetLength(long value)                       { throw new NotImplementedException(); }

    public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

    protected override void Dispose(bool disposing)
    {
        _audioEvent?.Set();
        if (_waveInEvent != null)
        {
            _waveInEvent.DataAvailable -= ProcessAvailableData;
            _waveInEvent.Dispose();
        }
        base.Dispose(disposing);
    }
}
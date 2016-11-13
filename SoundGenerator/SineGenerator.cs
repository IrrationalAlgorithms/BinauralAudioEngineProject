using System;
using System.Threading;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SoundGenerator
{
    public class SineGenerator
    {
        private const int _waitPrecision = 1;
        private SourceVoice _sourceVoice;
        private XAudio2 _xaudio2;
        private MasteringVoice _masteringVoice;
        private WaveFormat _waveFormat;
        private DataStream _dataStream;
        private AudioBuffer[] _audioBuffersRing;
        private DataPointer[] _memBuffers;
        private int _bufferSize;
        private AutoResetEvent _bufferEndEvent;
        private PlayerState _playerState { get; set; }
        private Task _playSoundTask;
        private int _nextBuffer;
        private ManualResetEventSlim _playEvent;

        private bool _isConvolutionOn;
        private double _horizontalAngle;
        private double _elevation;

        private List<string> _log;

        private int _valueRate;
        private float _valueAmp;

        private const double MAX_HORIZONATL_ANGLE_ABS = 180;
        private const int HORIZONTAL_ANGLE_RANGE = 360;
        private const int MAX_ELEVATION_ANGLE = 90;
        private const int MIN_ELEVATION_ANGLE = -40;

        public SineGenerator()
        {
            _xaudio2 = new XAudio2();
            _masteringVoice = new MasteringVoice(_xaudio2);

            _waveFormat = new WaveFormat(44100, 32, 2);

            _sourceVoice = new SourceVoice(_xaudio2, _waveFormat);

            _bufferSize = _waveFormat.ConvertLatencyToByteSize(7);
            _dataStream = new DataStream(_bufferSize, true, true);

            _sourceVoice.BufferEnd += sourceVoice_BufferEnd;
            _sourceVoice.Start();

            _isConvolutionOn = false;
            _horizontalAngle = 0;
            _elevation = 0;

            _bufferEndEvent = new AutoResetEvent(false);

            _valueRate = 20;
            _valueAmp = 0.5f;

            _nextBuffer = 0;

            _playEvent = new ManualResetEventSlim();
            _log = new List<string>();


            // Pre-allocate buffers
            _audioBuffersRing = new AudioBuffer[3];
            _memBuffers = new DataPointer[_audioBuffersRing.Length];
            for (int i = 0; i < _audioBuffersRing.Length; i++)
            {
                _audioBuffersRing[i] = new AudioBuffer();
                _memBuffers[i].Size = _bufferSize;
                _memBuffers[i].Pointer = Utilities.AllocateMemory(_memBuffers[i].Size);
            }

            _playSoundTask = Task.Factory.StartNew(PlaySoundAsync, TaskCreationOptions.LongRunning);
        }

        public void AddValue(int value = 10)
        {
            _valueRate += value;
        }

        public void AddValueAmp(float value)
        {
            _valueAmp += value;
        }

        public void SetConvolutionMode(bool isOn)
        {
            _isConvolutionOn = isOn;
        }

        public void SetHorizontalPosition(double angle)
        {
            _horizontalAngle = angle % MAX_HORIZONATL_ANGLE_ABS;
        }

        public void SetElevation(int angle)
        {
            if (angle < MIN_ELEVATION_ANGLE || angle > 180 + MIN_ELEVATION_ANGLE)
            {
                _elevation = MIN_ELEVATION_ANGLE;
            } else if (angle > 90 && angle <= 180 + MIN_ELEVATION_ANGLE)
            {
                _elevation = MAX_ELEVATION_ANGLE - (angle - MAX_ELEVATION_ANGLE);
            }
            else
            {
                _elevation = angle;
            }
        }

        public void Play()
        {
            if (_playerState != PlayerState.Playing)
            {
                _playerState = PlayerState.Playing;
                _playEvent.Set();
            }
        }

        public void Pause()
        {
            if (_playerState == PlayerState.Playing)
            {
                _playerState = PlayerState.Paused;
                _playEvent.Reset();
            }
        }

        private void PlaySoundAsync()
        {
            try
            {
                int numberOfSamples = _bufferSize / _waveFormat.BlockAlign;
                
                while (true)
                {
                    if (_playerState != PlayerState.Stopped)
                    {
                        while (_playerState != PlayerState.Stopped)
                        {
                            if (_playEvent.Wait(_waitPrecision))
                                break;
                        }

                        for (int i = 0; i < numberOfSamples; i++)
                        {
                            float value = (float) (Math.Sin(2*Math.PI*_valueRate*i/_waveFormat.SampleRate)*_valueAmp);
                            _dataStream.Write(value);
                            _dataStream.Write(value);
                        }

                        _dataStream.Position = 0;

                        while (_sourceVoice.State.BuffersQueued == _audioBuffersRing.Length)
                            _bufferEndEvent.WaitOne(_waitPrecision);

                        if (_bufferSize > _memBuffers[_nextBuffer].Size)
                        {
                            if (_memBuffers[_nextBuffer].Pointer != IntPtr.Zero)
                                Utilities.FreeMemory(_memBuffers[_nextBuffer].Pointer);

                            _memBuffers[_nextBuffer].Pointer = Utilities.AllocateMemory(_bufferSize);
                            _memBuffers[_nextBuffer].Size = _bufferSize;
                        }

                        // Copy the memory from MediaFoundation AudioDecoder to the buffer that is going to be played.
                        Utilities.CopyMemory(_memBuffers[_nextBuffer].Pointer, _dataStream.DataPointer, _bufferSize);

                        // Set the pointer to the data.
                        _audioBuffersRing[_nextBuffer].AudioDataPointer = _memBuffers[_nextBuffer].Pointer;
                        _audioBuffersRing[_nextBuffer].AudioBytes = _bufferSize;

                        if (!_isConvolutionOn)
                        {
                            var angle = _horizontalAngle > 90 ? MAX_HORIZONATL_ANGLE_ABS - _horizontalAngle : _horizontalAngle;
                            float rightChannelVolume = (float) (angle + MAX_HORIZONATL_ANGLE_ABS/2)/
                                                       (HORIZONTAL_ANGLE_RANGE/2);
                            float leftChannelVolume = 1 - rightChannelVolume;
                            _sourceVoice.SetChannelVolumes(2, new[] {leftChannelVolume, rightChannelVolume});
                        }

                        _sourceVoice.SubmitSourceBuffer(_audioBuffersRing[_nextBuffer], null);

                        // Go to next entry in the ringg audio buffer
                        _nextBuffer = ++_nextBuffer%_audioBuffersRing.Length;

                    }
                }
            }
            finally
            {
                Close();
            }

        }

        private void WriteLog(string logMsg)
        {
            _log.Add(logMsg);
        }

        public List<string> PrintLog()
        {
            var a = _log;
            _log = new List<string>();
            return a;
        }
        void sourceVoice_BufferEnd(IntPtr obj)
        {
            _bufferEndEvent.Set();
        }

        public void Close()
        {
            Utilities.Dispose(ref _masteringVoice);
            Utilities.Dispose(ref _xaudio2);
        }

        public void Dispose()
        {

            _sourceVoice.Dispose();
            _sourceVoice = null;

            for (int i = 0; i < _audioBuffersRing.Length; i++)
            {
                Utilities.FreeMemory(_memBuffers[i].Pointer);
                _memBuffers[i].Pointer = IntPtr.Zero;
            }
        }
    }
}

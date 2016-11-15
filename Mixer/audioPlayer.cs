using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.MediaFoundation;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace SoundGenerator
{
    class AudioPlayer : IAudioPlayer
    {
        private const int _WaitPrecision = 1;
        private XAudio2 _xaudio2;
        private AudioDecoder _audioDecoder;
        private SourceVoice _sourceVoice;
        private AudioBuffer[] _audioBuffersRing;
        private DataPointer[] _memBuffers;
        private ManualResetEvent _playEvent;
        private AutoResetEvent _bufferEndEvent;
        private Task _playingTask;
        private bool _IsDisposed;
        private Convolution _convolution;

        private bool _isConvolutionOn;
        private float _horizontalAngle;
        private float _elevation;

        private int _bufferSize;
        private DataStream _indDataStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayer" /> class.
        /// </summary>
        /// <param name="xaudio2">The xaudio2 engine.</param>
        /// <param name="audioStream">The input audio stream.</param>
        public AudioPlayer(XAudio2 xaudio2, Stream audioStream)
        {
            _xaudio2 = xaudio2;
            //_indDataStream = new SoundStream(audioStream).ToDataStream();
            _audioDecoder = new AudioDecoder(audioStream);
            _sourceVoice = new SourceVoice(xaudio2, _audioDecoder.WaveFormat);

            _bufferSize = _audioDecoder.WaveFormat.ConvertLatencyToByteSize(7);

            _isConvolutionOn = false;
            _horizontalAngle = 0f;
            _elevation = 0f;

            _sourceVoice.BufferEnd += sourceVoice_BufferEnd;
            _sourceVoice.Start();

            _bufferEndEvent = new AutoResetEvent(false);
            _playEvent = new ManualResetEvent(false);

            _convolution = new Convolution();

            // Pre-allocate buffers
            _audioBuffersRing = new AudioBuffer[3];
            _memBuffers = new DataPointer[_audioBuffersRing.Length];
            for (int i = 0; i < _audioBuffersRing.Length; i++)
            {
                _audioBuffersRing[i] = new AudioBuffer();
                _memBuffers[i].Size = 32 * 1024; // default size 32Kb
                _memBuffers[i].Pointer = Utilities.AllocateMemory(_memBuffers[i].Size);
            }

            // Initialize to stopped
            State = PlayerState.Stopped;

            // Starts the playing thread
            _playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void SetConvolutionMode(bool isOn)
        {
            _isConvolutionOn = isOn;

        }

        public void SetConvolutionFunctions(Stream leftStream, Stream rightStream)
        {

            _convolution.SetConvolutionStreams(leftStream, rightStream);

        }

        public void SetHorizontalPosition(float angle)
        {
            _horizontalAngle = angle;
            _convolution.horizontalPostion = angle;
        }

        public void SetElevation(float angle)
        {
            _elevation = angle;
            _convolution.elevation = angle;
        }

        /// <summary>
        /// Gets the XAudio2 <see cref="SourceVoice"/> created by this decoder.
        /// </summary>
        /// <value>The source voice.</value>
        public SourceVoice SourceVoice { get { return _sourceVoice; } }

        /// <summary>
        /// Gets the state of this instance.
        /// </summary>
        /// <value>The state.</value>
        public PlayerState State { get; private set; }

        /// <summary>
        /// Gets the duration in seconds of the current sound.
        /// </summary>
        /// <value>The duration.</value>
        
        /// <summary>
        /// Plays the sound.
        /// </summary>
        public void Play()
        {
            if (State != PlayerState.Playing)
            {
                State = PlayerState.Playing;
                _playEvent.Set();

            }
        }

        /// <summary>
        /// Pauses the sound.
        /// </summary>
        public void Pause()
        {
            if (State == PlayerState.Playing)
            {
                State = PlayerState.Paused;
                _playEvent.Reset();
            }
        }

        /// <summary>
        /// Stops the sound.
        /// </summary>
        public void Stop()
        {
            if (State != PlayerState.Stopped)
            {
                State = PlayerState.Stopped;
                _playEvent.Reset();
            }
        }

        /// <summary>
        /// Close this audio player.
        /// </summary>
        /// <remarks>
        /// This is similar to call Stop(), Dispose(), Wait().
        /// </remarks>
        public void Close()
        {
            Stop();
            _IsDisposed = true;
            Wait();
        }

        /// <summary>
        /// Wait that the player is finished.
        /// </summary>
        public void Wait()
        {
            _playingTask.Wait();
        }

        /// <summary>
        /// Internal method to play the sound.
        /// </summary>
        private void PlayAsync()
        {
            int nextBuffer = 0;

            try
            {
                while (true)
                {
                    // Check that this instanced is not disposed
                    while (!_IsDisposed)
                    {
                        if (_playEvent.WaitOne(_WaitPrecision))
                            break;
                    }

                    if (_IsDisposed)
                        break;


                    // Get the decoded samples from the specified starting position.
                    var sampleIterator = _audioDecoder.GetSamples().GetEnumerator();

                    bool isFirstTime = true;

                    bool endOfSong = false;
                    int offset = 0;
                    //var left = _indDataStream.Length;
                    int numberOfSamples = _bufferSize / _audioDecoder.WaveFormat.BlockAlign;
                    // Playing all the samples
                    while (true)
                    {
                        // If the player is stopped or disposed, then break of this loop
                        while (!_IsDisposed && State != PlayerState.Stopped)
                        {
                            if (_playEvent.WaitOne(_WaitPrecision))
                                break;
                        }

                        // If the player is stopped or disposed, then break of this loop
                        if (_IsDisposed || State == PlayerState.Stopped)
                        {
                            break;
                        }

                        // If ring buffer queued is full, wait for the end of a buffer.
                        while (_sourceVoice.State.BuffersQueued == _audioBuffersRing.Length && !_IsDisposed && State != PlayerState.Stopped)
                            _bufferEndEvent.WaitOne(_WaitPrecision);

                        // If the player is stopped or disposed, then break of this loop
                        if (_IsDisposed || State == PlayerState.Stopped)
                        {
                            break;
                        }

                        // Check that there is a next sample
                        if (!sampleIterator.MoveNext())
                        {
                            endOfSong = true;
                            break;
                        }

                        // Retrieve a pointer to the sample data
                        var bufferPointer = sampleIterator.Current;

                        var leftList = new List<float>();
                        var rightList = new List<float>();
                        //if(left <= 0)
                        //{
                        //    endOfSong = true;
                        //    break;
                        //}
                        //var count = left < numberOfSamples ? (int)left : numberOfSamples;
                        //var bufferPointer = DataStream.Create(_indDataStream.ReadRange<float>(count),true,true);
                        //left -= count;
                        var dataStream = bufferPointer.ToDataStream();
                        if (_isConvolutionOn)
                        {
                            //var trackLength = numberOfSamples / 2;
                            var trackLength = bufferPointer.Size / (sizeof(float) * 2);
                            while (leftList.Count < trackLength)
                            {
                                leftList.Add(dataStream.Read<float>());
                                rightList.Add(dataStream.Read<float>());
                            }


                            var buffer = _convolution.Calculate(leftList.ToArray(), rightList.ToArray());
                            bufferPointer = DataBuffer.Create(buffer);
                            //bufferPointer = DataStream.Create(buffer, true, true);
                        }

                        // Check that our ring buffer has enough space to store the audio buffer.
                        if (bufferPointer.Size > _memBuffers[nextBuffer].Size)
                        {
                            if (_memBuffers[nextBuffer].Pointer != IntPtr.Zero)
                                Utilities.FreeMemory(_memBuffers[nextBuffer].Pointer);

                            _memBuffers[nextBuffer].Pointer = Utilities.AllocateMemory(_bufferSize);
                            _memBuffers[nextBuffer].Size = (int)bufferPointer.Size;
                        }

                        // Copy the memory from MediaFoundation AudioDecoder to the buffer that is going to be played.
                        Utilities.CopyMemory(_memBuffers[nextBuffer].Pointer, bufferPointer.Pointer, bufferPointer.Size);

                        // Set the pointer to the data.
                        _audioBuffersRing[nextBuffer].AudioDataPointer = _memBuffers[nextBuffer].Pointer;
                        _audioBuffersRing[nextBuffer].AudioBytes = bufferPointer.Size;

                        if (_isConvolutionOn)
                        {
                           setLinearSound();
                        }
                        // Submit the audio buffer to xaudio2
                        _sourceVoice.SubmitSourceBuffer(_audioBuffersRing[nextBuffer], null);

                        // Go to next entry in the ringg audio buffer
                        nextBuffer = ++nextBuffer % _audioBuffersRing.Length;
                    }

                    if (endOfSong && State == PlayerState.Playing)
                    {
                        Stop();
                    }

                }
            }
            finally
            {
                DisposePlayer();
            }
        }

        private void setLinearSound()
        {
            var absHorizontalAngle = Math.Abs(_horizontalAngle);
            var angle = absHorizontalAngle > Constants.MAX_HORIZONATL_ANGLE_ABS / 2
                ? (Constants.MAX_HORIZONATL_ANGLE_ABS / 2 - (absHorizontalAngle - Constants.MAX_HORIZONATL_ANGLE_ABS / 2))
                  * Math.Sign(_horizontalAngle) * (-1)
                : _horizontalAngle;

            float rightChannelVolume = (float)(angle + Constants.MAX_HORIZONATL_ANGLE_ABS / 2) /
                                       (Constants.HORIZONTAL_ANGLE_RANGE / 2);
            float leftChannelVolume = 1 - rightChannelVolume;
            _sourceVoice.SetChannelVolumes(2, new[] { rightChannelVolume, leftChannelVolume });
        }

      

        private void DisposePlayer()
        {
            _audioDecoder.Dispose();
            _audioDecoder = null;

            _sourceVoice.Dispose();
            _sourceVoice = null;

            for (int i = 0; i < _audioBuffersRing.Length; i++)
            {
                Utilities.FreeMemory(_memBuffers[i].Pointer);
                _memBuffers[i].Pointer = IntPtr.Zero;
            }
        }

        void sourceVoice_BufferEnd(IntPtr obj)
        {
            _bufferEndEvent.Set();
        }
    }
}

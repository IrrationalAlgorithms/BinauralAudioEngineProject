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
        private const int WaitPrecision = 1;
        private XAudio2 xaudio2;
        private AudioDecoder audioDecoder;
        private SourceVoice sourceVoice;
        private AudioBuffer[] audioBuffersRing;
        private DataPointer[] memBuffers;
        private Stopwatch clock;
        private ManualResetEvent playEvent;
        private ManualResetEvent waitForPlayToOutput;
        private AutoResetEvent bufferEndEvent;
        private TimeSpan playPosition;
        private TimeSpan nextPlayPosition;
        private TimeSpan playPositionStart;
        private Task playingTask;
        private int playCounter;
        private float localVolume;
        private bool IsDisposed;
        private AudioDecoder leftConvolutionDecoder;
        private SoundStream leftConvolutionStream;
        private AudioDecoder rightConvolutionDecoder;
        private SoundStream rightConvolutionStream;


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
            this.xaudio2 = xaudio2;
            audioDecoder = new AudioDecoder(audioStream);
            sourceVoice = new SourceVoice(xaudio2, audioDecoder.WaveFormat);
            localVolume = 1.0f;

            var soundStream = new SoundStream(audioStream);
            _indDataStream = soundStream.ToDataStream();
            _bufferSize = audioDecoder.WaveFormat.ConvertLatencyToByteSize(7);

            _isConvolutionOn = false;
            _horizontalAngle = 0f;
            _elevation = 0f;
            leftConvolutionDecoder = null;

            sourceVoice.BufferEnd += sourceVoice_BufferEnd;
            sourceVoice.Start();

            bufferEndEvent = new AutoResetEvent(false);
            playEvent = new ManualResetEvent(false);
            waitForPlayToOutput = new ManualResetEvent(false);

            clock = new Stopwatch();

            // Pre-allocate buffers
            audioBuffersRing = new AudioBuffer[3];
            memBuffers = new DataPointer[audioBuffersRing.Length];
            for (int i = 0; i < audioBuffersRing.Length; i++)
            {
                audioBuffersRing[i] = new AudioBuffer();
                memBuffers[i].Size = 32 * 1024; // default size 32Kb
                memBuffers[i].Pointer = Utilities.AllocateMemory(memBuffers[i].Size);
            }

            // Initialize to stopped
            State = PlayerState.Stopped;

            // Starts the playing thread
            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Gets the XAudio2 <see cref="SourceVoice"/> created by this decoder.
        /// </summary>
        /// <value>The source voice.</value>
        public SourceVoice SourceVoice { get { return sourceVoice; } }

        /// <summary>
        /// Gets the state of this instance.
        /// </summary>
        /// <value>The state.</value>
        public PlayerState State { get; private set; }

        /// <summary>
        /// Gets the duration in seconds of the current sound.
        /// </summary>
        /// <value>The duration.</value>
        public TimeSpan Duration
        {
            get { return audioDecoder.Duration; }
        }

        /// <summary>
        /// Gets or sets the position in seconds.
        /// </summary>
        /// <value>The position.</value>
        public TimeSpan Position
        {
            get { return playPosition; }
            set
            {
                playPosition = value;
                nextPlayPosition = value;
                playPositionStart = value;
                clock.Restart();
                playCounter++;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to the sound is looping when the end of the buffer is reached.
        /// </summary>
        /// <value><c>true</c> if to loop the sound; otherwise, <c>false</c>.</value>
        public bool IsRepeating { get; set; }

        /// <summary>
        /// Plays the sound.
        /// </summary>
        public void Play()
        {
            if (State != PlayerState.Playing)
            {
                bool waitForFirstPlay = false;
                if (State == PlayerState.Stopped)
                {
                    playCounter++;
                    waitForPlayToOutput.Reset();
                    waitForFirstPlay = true;
                }
                else
                {
                    // The song was paused
                    clock.Start();
                }

                State = PlayerState.Playing;
                playEvent.Set();

                if (waitForFirstPlay)
                {
                    waitForPlayToOutput.WaitOne();
                }
            }
        }

        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public float Volume
        {
            get { return localVolume; }
            set
            {
                localVolume = value;
                sourceVoice.SetVolume(value);
            }
        }

        public void SetConvolutionMode(bool isOn)
        {
            _isConvolutionOn = isOn;
            
        }

        public void SetConvolutionFunctions(Stream leftStream, Stream rightStream)
        {

            leftConvolutionStream = new SoundStream(leftStream);
            rightConvolutionDecoder = new AudioDecoder(rightStream);

        }

        public void SetHorizontalPosition(float angle)
        {
            _horizontalAngle = angle;
        }

        public void SetElevation(float angle)
        {
            _elevation = angle;
        }

        /// <summary>
        /// Pauses the sound.
        /// </summary>
        public void Pause()
        {
            if (State == PlayerState.Playing)
            {
                clock.Stop();
                State = PlayerState.Paused;
                playEvent.Reset();
            }
        }

        /// <summary>
        /// Stops the sound.
        /// </summary>
        public void Stop()
        {
            if (State != PlayerState.Stopped)
            {
                playPosition = TimeSpan.Zero;
                nextPlayPosition = TimeSpan.Zero;
                playPositionStart = TimeSpan.Zero;
                clock.Stop();
                playCounter++;
                State = PlayerState.Stopped;
                playEvent.Reset();
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
            IsDisposed = true;
            Wait();
        }

        /// <summary>
        /// Wait that the player is finished.
        /// </summary>
        public void Wait()
        {
            playingTask.Wait();
        }

        /// <summary>
        /// Internal method to play the sound.
        /// </summary>
        private void PlayAsync()
        {
            int currentPlayCounter = 0;
            int nextBuffer = 0;

            try
            {
                while (true)
                {
                    // Check that this instanced is not disposed
                    while (!IsDisposed)
                    {
                        if (playEvent.WaitOne(WaitPrecision))
                            break;
                    }

                    if (IsDisposed)
                        break;

                    clock.Restart();
                    playPositionStart = nextPlayPosition;
                    playPosition = playPositionStart;
                    currentPlayCounter = playCounter;

                    // Get the decoded samples from the specified starting position.
                    var sampleIterator = audioDecoder.GetSamples(playPositionStart).GetEnumerator();

                    bool isFirstTime = true;

                    bool endOfSong = false;
                    int offset = 0;
                    int read = 0;
                    // Playing all the samples
                    while (true)
                    {
                        // If the player is stopped or disposed, then break of this loop
                        while (!IsDisposed && State != PlayerState.Stopped)
                        {
                            if (playEvent.WaitOne(WaitPrecision))
                                break;
                        }

                        // If the player is stopped or disposed, then break of this loop
                        if (IsDisposed || State == PlayerState.Stopped)
                        {
                            nextPlayPosition = TimeSpan.Zero;
                            break;
                        }

                        // If there was a change in the play position, restart the sample iterator.
                        if (currentPlayCounter != playCounter)
                            break;

                        // If ring buffer queued is full, wait for the end of a buffer.
                        while (sourceVoice.State.BuffersQueued == audioBuffersRing.Length && !IsDisposed && State != PlayerState.Stopped)
                            bufferEndEvent.WaitOne(WaitPrecision);

                        // If the player is stopped or disposed, then break of this loop
                        if (IsDisposed || State == PlayerState.Stopped)
                        {
                            nextPlayPosition = TimeSpan.Zero;
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
                        while (_indDataStream.CanRead && leftList.Count < _bufferSize)
                        {
                            leftList.Add(_indDataStream.Read<float>());
                            rightList.Add(_indDataStream.Read<float>());
                        }
                        
                        //if(_isConvolutionOn)
                        //    bufferPointer = convolution(bufferPointer);\

                        // If there was a change in the play position, restart the sample iterator.
                        if (currentPlayCounter != playCounter)
                            break;

                        // Check that our ring buffer has enough space to store the audio buffer.
                        if (bufferPointer.Size > memBuffers[nextBuffer].Size)
                        {
                            if (memBuffers[nextBuffer].Pointer != IntPtr.Zero)
                                Utilities.FreeMemory(memBuffers[nextBuffer].Pointer);

                            memBuffers[nextBuffer].Pointer = Utilities.AllocateMemory(bufferPointer.Size);
                            memBuffers[nextBuffer].Size = bufferPointer.Size;
                        }

                        // Copy the memory from MediaFoundation AudioDecoder to the buffer that is going to be played.
                        Utilities.CopyMemory(memBuffers[nextBuffer].Pointer, bufferPointer.Pointer, bufferPointer.Size);

                        // Set the pointer to the data.
                        audioBuffersRing[nextBuffer].AudioDataPointer = memBuffers[nextBuffer].Pointer;
                        audioBuffersRing[nextBuffer].AudioBytes = bufferPointer.Size;

                        // If this is a first play, restart the clock and notify play method.
                        if (isFirstTime)
                        {
                            clock.Restart();
                            isFirstTime = false;

                            waitForPlayToOutput.Set();
                        }

                        // Update the current position used for sync
                        playPosition = new TimeSpan(playPositionStart.Ticks + clock.Elapsed.Ticks);
                        if (_isConvolutionOn)
                        {
                           setLinearSound();
                        }
                        // Submit the audio buffer to xaudio2
                        sourceVoice.SubmitSourceBuffer(audioBuffersRing[nextBuffer], null);

                        // Go to next entry in the ringg audio buffer
                        nextBuffer = ++nextBuffer % audioBuffersRing.Length;
                    }

                    // If the song is not looping (by default), then stop the audio player.
                    if (endOfSong && !IsRepeating && State == PlayerState.Playing)
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
            sourceVoice.SetChannelVolumes(2, new[] { rightChannelVolume, leftChannelVolume });
        }

      

        private void DisposePlayer()
        {
            audioDecoder.Dispose();
            audioDecoder = null;

            sourceVoice.Dispose();
            sourceVoice = null;

            for (int i = 0; i < audioBuffersRing.Length; i++)
            {
                Utilities.FreeMemory(memBuffers[i].Pointer);
                memBuffers[i].Pointer = IntPtr.Zero;
            }
        }

        void sourceVoice_BufferEnd(IntPtr obj)
        {
            bufferEndEvent.Set();
        }
    }
}

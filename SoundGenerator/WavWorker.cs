using System.Threading.Tasks;
using System.Media;
using System.Threading;
using System.IO;
using System;

namespace SoundGenerator
{
    public class WavWorker
    {
        Task playingTask;
        ManualResetEventSlim playEvent;
        PlayerState _state;
        Stream _stream;

        public WavWorker()
        {
            playingTask = Task.Factory.StartNew(PlayTest, TaskCreationOptions.LongRunning);
            playEvent = new ManualResetEventSlim();
            _state = PlayerState.Stopped;
        }

        public void PlayWav(WavSample sound)
        {
            if (_state == PlayerState.Playing)
            {
                playEvent.Wait();
            }
            else
            {
                playEvent.Set();
                switch (sound)
                {
                    case WavSample.test:
                        _stream = Properties.Resources.test;
                        break;
                    case WavSample.alesis:
                        _stream = Properties.Resources.alesis;
                        break;
                }
            }
        }

        private Object thisLock = new Object();

        private void PlayTest()
        {
            while (true)
            {
                if (playEvent.Wait(1))
                {
                    _state = PlayerState.Playing;
                    lock (thisLock) {
                        SoundPlayer simpleSound = new SoundPlayer(_stream);
                        simpleSound.Play();
                    }
                    playEvent.Reset();
                    Thread.Sleep(100);
                }
                else
                {
                    _state = PlayerState.Stopped;
                }
            }
        }
    }
}

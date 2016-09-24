using System;
using System.Threading;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace SoundGenerator
{
    public class SineGenerator
    {
        public void Generate()
        {
            var xaudio2 = new XAudio2();
            var masteringVoice = new MasteringVoice(xaudio2);

            var waveFormat = new WaveFormat(44100, 32, 2);
            var sourceVoice = new SourceVoice(xaudio2, waveFormat);

            int bufferSize = waveFormat.ConvertLatencyToByteSize(60000);
            var dataStream = new DataStream(bufferSize, true, true);

            int numberOfSamples = bufferSize / waveFormat.BlockAlign;
            for (int i = 0; i < numberOfSamples; i++)
            {
                float value = (float)(Math.Cos(2 * Math.PI * (220.0 + 4.0) * i / waveFormat.SampleRate) * 0.5);
                dataStream.Write(value);
                dataStream.Write(value);
            }
            dataStream.Position = 0;

            var audioBuffer = new AudioBuffer { Stream = dataStream, Flags = BufferFlags.EndOfStream, AudioBytes = bufferSize };

            sourceVoice.SubmitSourceBuffer(audioBuffer, null);

            sourceVoice.Start();

            Console.WriteLine("Play sound");
            for (int i = 0; i < 60; i++)
            {
                Console.Write(".");
                Console.Out.Flush();
                Thread.Sleep(1000);
            }
        }
    }
}

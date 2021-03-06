﻿using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SharpDX.MediaFoundation;
using SharpDX.Multimedia;

namespace Mixer
{
    class Convolution
    {

        private SoundStream _leftConvolutionStream;
        private SoundStream _rightConvolutionStream;
        private float[] _leftConvolutionArray;
        private float[] _rightConvolutionArray;
        private float[] _leftTail;
        private float[] _rightTail;


        public Convolution()
        {

        }

        public void SetConvolutionStreams(Stream left, Stream right)
        {
            _leftConvolutionStream = new SoundStream(left);
            _rightConvolutionStream = new SoundStream(right);

            var leftLength = (int)(_leftConvolutionStream.Length / sizeof(short));
            _leftConvolutionArray = Utils.MakeFloatFromShortSoundArray(_leftConvolutionStream.ToDataStream().ReadRange<short>(leftLength));

            var rightLength = (int)(_rightConvolutionStream.Length / sizeof(short));
            _rightConvolutionArray = Utils.MakeFloatFromShortSoundArray(_rightConvolutionStream.ToDataStream().ReadRange<short>(rightLength));
        }

        public float[] Calculate(float[] leftChannel, float[] rightChannel)
        {
            float[] resultBuffer = new float[leftChannel.Length + rightChannel.Length];
            float[] leftResultBuffer = null;
            float[] rightResultBuffer = null;
            var leftTask = Task.Factory.StartNew(
                () => _Calculate(leftChannel, _leftConvolutionArray, out leftResultBuffer, ref _leftTail)
                );
            var rightTask = Task.Factory.StartNew(
                () => _Calculate(rightChannel, _rightConvolutionArray, out rightResultBuffer, ref _rightTail)
                );
            leftTask.Wait();
            rightTask.Wait();
            for (int i = 0, k = 0; i < leftResultBuffer.Length; i++)
            {
                resultBuffer[k++] = leftResultBuffer[i];
                resultBuffer[k++] = rightResultBuffer[i];
            }
            return resultBuffer;
        }

        private void _Calculate(float[] channel, float[] convolutionFunc, out float[] result, ref float[] tail)
        {
            result = new float[channel.Length];
            float[] tmpBuff = new float[channel.Length + convolutionFunc.Length - 1];
            if (tail != null)
            {
                for (int i = 0; i < tail.Length; i++)
                {
                    tmpBuff[i] += tail[i];
                }
            }
            
            for (int i = 0; i < convolutionFunc.Length; i++)
            {
                for (int k = 0; k < channel.Length; k++)
                {
                    tmpBuff[i + k] += channel[k] * convolutionFunc[i];
                }
                
            }

            tail = new float[convolutionFunc.Length - 1];
            result = new float[channel.Length];
            Array.Copy(tmpBuff, channel.Length, tail, 0, tail.Length);
            Array.Copy(tmpBuff, 0, result, 0, result.Length);
        }

    }
}

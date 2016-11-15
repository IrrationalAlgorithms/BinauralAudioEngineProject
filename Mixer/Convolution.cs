using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SharpDX.MediaFoundation;
using SharpDX.Multimedia;

namespace SoundGenerator
{
    class Convolution
    {

        private DataStream _leftConvolutionStream;
        private DataStream _rightConvolutionStream;
        private float[] _leftTail;
        private float[] _rightTail;

        public float elevation
        {
            get { return elevation; }
            set
            {
                if (value < Constants.MIN_ELEVATION_ANGLE || value > 180 + Constants.MIN_ELEVATION_ANGLE)
                {
                    elevation = Constants.MIN_ELEVATION_ANGLE;
                }
                else if (value > 90 && value <= 180 + Constants.MIN_ELEVATION_ANGLE)
                {
                    elevation = Constants.MAX_ELEVATION_ANGLE - (value - Constants.MAX_ELEVATION_ANGLE);
                }
                else
                {
                    elevation = value;
                }
            } 
        }

        public float horizontalPostion
        {
            get { return horizontalPostion; }
            set
            {
                var absAngle = Math.Abs(value);

                if (absAngle > Constants.MAX_HORIZONATL_ANGLE_ABS)
                {
                    horizontalPostion = (Constants.MAX_HORIZONATL_ANGLE_ABS - (absAngle - Constants.MAX_HORIZONATL_ANGLE_ABS))
                                       * Math.Sign(value) * (-1);
                }
                else
                {
                    horizontalPostion = value;
                }
            }
        }

        private void prepareArrays(out float[] leftArray, out float[] rightArray)
        {
            var leftLength = (int)(_leftConvolutionStream.Length / sizeof(float));
            leftArray = _leftConvolutionStream.ReadRange<float>(leftLength);

            var rightLength = (int)(_rightConvolutionStream.Length / sizeof(float));
            rightArray = _rightConvolutionStream.ReadRange<float>(rightLength);
        }

      

        public Convolution()
        {
            
        }

        public void SetConvolutionStreams(Stream left, Stream right)
        {
            _leftConvolutionStream = new SoundStream(left).ToDataStream();
            _rightConvolutionStream = new SoundStream(right).ToDataStream();
        }
        
        public float[] Calculate(float[] leftChannel, float[] rightChannel)
        {
            float[] leftConvolutionFunc = null;
            float[] rightConvolutionFunc = null;
            prepareArrays(out leftConvolutionFunc, out rightConvolutionFunc);
            float[] resultBuffer = new float[leftChannel.Length + rightChannel.Length];
            float[] leftResultBuffer = null;
            float[] rightResultBuffer = null;
            var leftTask = Task.Factory.StartNew(() => _Calculate(leftChannel, leftConvolutionFunc, out leftResultBuffer, _leftTail));
            var rightTask = Task.Factory.StartNew(() => _Calculate(rightChannel, rightConvolutionFunc, out rightResultBuffer, _rightTail));
            leftTask.Wait();
            rightTask.Wait();
            for (int i = 0, k = 0; i < leftResultBuffer.Length; i++)
            {
                resultBuffer[k++] = leftResultBuffer[i];
                resultBuffer[k++] = rightResultBuffer[i];
            }
            return resultBuffer;
        }

        private void _Calculate(float[] channel, float[] convolutionFunc, out float[] result, float[] tail)
        {
            result = new float[channel.Length];
           double[] tmpBuff = new double[channel.Length + convolutionFunc.Length - 1];
            if (tail != null)
            {
                for (int i = 0; i < tail.Length; i++)
                {
                    tmpBuff[i] += tail[i];
                }
            }
            int offset = 0;
            for (int i = 0; i < convolutionFunc.Length; i++)
            {
                for (int k = 0; k < channel.Length; k++)
                {
                    tmpBuff[offset + i] += (double)channel[k] * (double)convolutionFunc[i];
                }
                offset++;
            }

            tail = new float[convolutionFunc.Length - 1];
            result = new float[channel.Length];
            Array.Copy(tmpBuff, channel.Length - 1, tail, 0, tail.Length);
            Array.Copy(tmpBuff, 0, result, 0, result.Length);
        }
    }
}

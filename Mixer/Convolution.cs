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

        public DataStream leftConvolutionStream;
        public DataStream rightConvolutionStream;

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

      

        public Convolution()
        {
            
        }

        public void SetConvolutionStreams(Stream left, Stream right)
        {
            leftConvolutionStream = new SoundStream(left).ToDataStream();
            rightConvolutionStream = new SoundStream(right).ToDataStream();
        }
        
        private DataStream Calculate(float[] rightChannel, float[] leftChannel)
        {
            var leftConvolutionLength = leftConvolutionStream.Length / (sizeof(float));
            var rightConvolutionLength = rightConvolutionStream.Length / (sizeof(float));
            var resultBuffer = new float[rightChannel.Length + leftChannel.Length];
            return null;
        }
    }
}

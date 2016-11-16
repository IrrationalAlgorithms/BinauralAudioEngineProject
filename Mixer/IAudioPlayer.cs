using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixer
{
    interface IAudioPlayer
    {
        void SetElevation(float angle);
        void SetHorizontalPosition(float angle);
        void SetConvolutionMode(bool isConvolutionOn);
        void SetConvolutionFunctions(Stream leftStream, Stream rightStream);
        void Play();
        void Pause();
        void Stop();
        void Close();
    }
}

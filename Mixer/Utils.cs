using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixer
{
    static class Utils
    {
        public static float[] MakeFloatFromShortSoundArray(short[] source)
        {
            float[] floatArr = new float[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var f = ((float)source[i]) / ((float)short.MaxValue);
                if (f > 1) f = 1;
                if (f < -1) f = -1;
                floatArr[i] = f;
            }
            return floatArr;
        }
    }
}
    
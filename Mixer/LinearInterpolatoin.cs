using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixer
{
    class LinearInterpolatoin : IInterpolation
    {
        
        public double getValue(double x0, double x1, double y0, double y1, double x)
        {
            var a = (y1 - y0) / (x1 - x0);
            var b = y0 - a * x0;
            return a * x + b;
        } 
    }
}

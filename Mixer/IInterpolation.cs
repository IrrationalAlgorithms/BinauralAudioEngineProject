using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixer
{
    interface IInterpolation
    {
        double getValue(double x0, double x1, double y0, double y1, double x);
    }
}

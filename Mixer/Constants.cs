using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixer
{
    public static  class Constants
    {
        public const float MAX_HORIZONATL_ANGLE_ABS = 180f;
        public const float HORIZONTAL_ANGLE_RANGE = 360f;
        public const float MAX_ELEVATION_ANGLE = 90f;
        public const float MIN_ELEVATION_ANGLE = -40f;

    }

    public enum PlayerState
    {
        Stopped,
        Playing,
        Paused,
    }
}

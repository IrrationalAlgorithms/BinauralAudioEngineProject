using SharpDX.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixer
{
    class StreamLoader
    {
        //public Stream getStream(Dictionary<int, Dictionary<int, string>> paths)
        //{
            
        //    foreach (var key in paths.Keys)
        //    {
        //        foreach (var path in paths[key])
        //        {

        //        }
        //    }
        //}
        public Stream getStream(string path)
        {
            return new NativeFileStream(path, NativeFileMode.Open, NativeFileAccess.Read, NativeFileShare.Read);
        }
    }
}

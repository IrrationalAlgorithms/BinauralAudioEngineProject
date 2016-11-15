using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mixer
{
    public static class ConvolutionResourseReader
    {
        static Lazy<List<ConvolutionFile>> _files = new Lazy<List<ConvolutionFile>>(InitializeFiles);

        private static List<ConvolutionFile> InitializeFiles()
        {
            List<ConvolutionFile> list = new List<ConvolutionFile>();
            for (var i = 1; i <= 14; i++)
            {
                list.AddRange(JsonConvert.DeserializeObject<List<ConvolutionFile>>(File.ReadAllText($"{i}.json")));
            }
            return list;
        } 

        public static string GetSoundPath(int elevation, int angle, Channel channel)
        {
            if (angle < 0) angle += 360;
            var fileElevation = _files.Value.Where(x => x.Elevation == elevation && x.Channel == channel);
            int max = Int32.MinValue;
            var maxPath = String.Empty;
            int min = Int32.MaxValue;
            var minPath = String.Empty;
            foreach (var file in fileElevation)
            {
                if (file.Angle == angle)
                    return file.Path.Substring(1);
                if (file.Angle < angle)
                {
                    min = angle - file.Angle;
                    minPath = file.Path;
                }
                if (file.Angle > angle)
                {
                    max = file.Angle - angle;
                    maxPath = file.Path;
                    break;
                }
            }
            return min < max || max == Int32.MinValue
                ? minPath.Length > 0 ? minPath.Substring(1) : String.Empty
                : maxPath.Length > 0 ? maxPath.Substring(1) : String.Empty;
        }

        private static void Generator(string[] fileNames)
        {
            List<ConvolutionFile> list = new List<ConvolutionFile>();

            foreach (var item in fileNames)
            {
                var lenght = item.Length;
                list.Add(new ConvolutionFile
                {
                    Elevation = Int32.Parse(item.Substring(lenght - 11, 2)),
                    Angle = Int32.Parse(item.Substring(lenght - 8, 3)),
                    Path = item.Substring(item.IndexOf(@"\Convolution")),
                    Channel = item.Substring(lenght - 12, 1) == "L" ? Channel.Left : Channel.Right
                });
            }
            //File.WriteAllText(@"movie.json", JsonConvert.SerializeObject(movie));

            File.WriteAllText(@"14.json", JsonConvert.SerializeObject(list));
        }
    }
}

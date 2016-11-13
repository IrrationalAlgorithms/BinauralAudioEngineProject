namespace Mixer
{
    public enum Channel
    {
        Left ,
        Right
    }

    internal class ConvolutionFile
    {
        public int Elevation { get; set; }
        public int Angle { get; set; }
        public string Path { get; set; }
        public Channel Channel { get; set; }
    }
}
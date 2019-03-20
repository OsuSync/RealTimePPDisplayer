namespace RealTimePPDisplayer.MultiOutput
{
    public enum MultiOutputType
    {
        MMF,
        WPF
    }

    public class MultiOutputItem
    {
        public string name { get; set; }
        public string format { get; set; }
        public MultiOutputType type { get; set; }
        public bool smooth { get; set; }
    }

}

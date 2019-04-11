using OsuRTDataProvider.Listen;

namespace RealTimePPDisplayer.MultiOutput
{
    public class MultiOutputItem
    {
        public string name { get; set; }
        public string format { get; set; }
        public string type { get; set; }
        public bool smooth { get; set; }
        public string modes { get; set; } = string.Empty;
        public string formatter { get; set; } = "rtpp-fmt";
    }
}

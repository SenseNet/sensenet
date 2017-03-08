namespace SenseNet.ContentRepository
{
    public class KPIData
    {
        public KPIData(string label, int goal, int actual)
        {
            Label = label;
            Goal = goal;
            Actual = actual;
        }

        public int Goal { get; protected set; }
        public int Actual { get; protected set; }
        public string Label { get; protected set; }
    }
}

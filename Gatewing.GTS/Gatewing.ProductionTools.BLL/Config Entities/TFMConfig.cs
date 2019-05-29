namespace Gatewing.ProductionTools.BLL
{
    public class TFMConfig : ConfigBase<TFMConfig>
    {
        public string DataPath { get; private set; }
        public string FilesExtensions { get; private set; }
        public string Location { get; private set; }
        public string Users { get; private set; }
        public int ElevatorTolerance { get; private set; }
        public int AileronTolerance { get; private set; }
        public bool DoDeleteAfterCopy { get; private set; }
        public string EmailAddresses { get; private set; }
        public string FlightLogUX5 { get; private set; }
        public string QAFormulaUX5 { get; private set; }
        public string FlightLogUX5HP { get; private set; }
        public string QAFormulaUX5HP { get; private set; }
        public string FlightLogUX5Series { get; private set; }
        public string QAFormulaUX5Series { get; private set; }
    }
}
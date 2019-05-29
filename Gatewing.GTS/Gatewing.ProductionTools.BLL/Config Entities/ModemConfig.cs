namespace Gatewing.ProductionTools.BLL
{
    public class ModemConfig: ConfigBase<ModemConfig>
    {
        public int WBComPortNumber { get; private set; }
        public int APComPortNumber { get; private set; }
        public int APBootWaitTime { get; private set; }
        public int ActionTimeout { get; private set; }
        public string AccessCode { get; private set; }
    }
}

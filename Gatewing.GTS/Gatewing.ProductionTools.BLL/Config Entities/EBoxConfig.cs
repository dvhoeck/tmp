
namespace Gatewing.ProductionTools.BLL
{
    public class EBXConfig: ConfigBase<EBXConfig>
    {
        // General     
        public string AccessCode { get; private set; }
        public int EBoxType{ get; private set; }

        // Comms
        public int ComPortNumber { get; private set; }
    }
}

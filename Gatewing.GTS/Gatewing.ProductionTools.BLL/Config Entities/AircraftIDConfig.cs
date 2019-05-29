using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class AircraftIDConfig: ConfigBase<AircraftIDConfig>
    {
        // general
        public string AccessCode { get; private set; }
        /*
        // general
        public bool Override { get; private set; }
        public int AcType { get; private set; }
        public int AcPLBType { get; private set; }
        public int AcMPLBType { get; private set; }
        */
        // Commms    
        public int ComPortNumber { get; private set; }
    }
}

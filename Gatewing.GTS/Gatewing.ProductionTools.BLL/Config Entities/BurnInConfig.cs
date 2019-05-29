using System;
using System.Collections.Generic;

namespace Gatewing.ProductionTools.BLL
{
    public class BurnInConfig: ConfigBase<BurnInConfig>
    {
        // General     
        public string AccessCode { get; private set; }

        // Communications
        public int ComportNr { get; private set; }
        public List<string> EmailAddresses { get; private set; }

        // TestRules    
        public TimeSpan BurnInTime { get; private set; }
    }
}

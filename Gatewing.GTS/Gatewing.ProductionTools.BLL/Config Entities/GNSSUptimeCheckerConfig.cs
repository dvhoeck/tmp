using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class GNSSUptimeCheckerConfig: ConfigBase<GNSSUptimeCheckerConfig>
    {
        public int Interval { get; private set; }
    }
}

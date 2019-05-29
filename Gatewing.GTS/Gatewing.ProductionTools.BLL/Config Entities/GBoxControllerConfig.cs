using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class GBoxControllerConfig: ConfigBase<GBoxControllerConfig>
    {
        public int Interval { get; private set; }
        public string DownloadDestination { get; private set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class MapHelperConfig: ConfigBase<MapHelperConfig>
    {
        public string Key { get; private set; }
        public string CenterLat { get; private set; }
        public string CenterLon { get; private set; }
    }
}

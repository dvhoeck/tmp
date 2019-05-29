
using System.Collections.Generic;

namespace Gatewing.ProductionTools.BLL
{
    public class Satellite
    {
        private List<SatelliteSubChannel> _subChannels = new List<SatelliteSubChannel>();

        public int SatellitePrn { get; set; }
        public int SystemId { get; set; }
        public string System
        {
            get
            {
                return ((SatelliteSystem)SystemId).ToString();
            }
        }

        public List<SatelliteSubChannel> SubChannels
        {
            get
            {
                return _subChannels;
            }
        }

        public string UniqueSatelliteID
        {
            get
            {
                return SystemId + "_" + SatellitePrn;
            }
        }
    }
    
    public class SatelliteSubChannel
    {
        //public int Index { get; set; }
        public int CodeType { get; set; }
        public int Frequency { get; set; }
        public int SNRx4 { get; set; }
    }
}

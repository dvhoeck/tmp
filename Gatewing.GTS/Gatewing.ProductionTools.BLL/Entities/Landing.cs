using System;
using System.Device.Location;

namespace Gatewing.ProductionTools.BLL
{
    public class Landing: DomainObject
    {
        public virtual GeoCoordinate Location { get; set; }
        public virtual DateTime Timestamp { get; set; }
        public virtual string Caption { get; set; }
    }
}


using System;

namespace Gatewing.ProductionTools.BLL
{
    public class Report: DomainObject
    {
        public virtual string Caption { get; set; }
        public virtual ReportType ReportType { get; set; }
        public virtual string DeviceId { get; set; }
        public virtual DateTime Timestamp { get; set; }
        public virtual string Comments { get; set; }
    }
}

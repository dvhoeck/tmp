using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class ReportMap: ClassMap<Report>
    {
        public ReportMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Caption);
            Map(x => x.ReportType).CustomType<int>();
            Map(x => x.DeviceId);
            Map(x => x.Timestamp);
            Map(x => x.Comments);
        }
    }
}

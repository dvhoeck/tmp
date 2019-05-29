using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.TestManager.DAL
{
    public class FlightSessionMap : ClassMap<FlightSession>
    {
        public FlightSessionMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Timestamp);
            Map(x => x.FinalizedFoldersPercentage);
            Map(x => x.FinalizedMailPercentage);
            Map(x => x.FinalizedSheetsPercentage);
        }
    }
}
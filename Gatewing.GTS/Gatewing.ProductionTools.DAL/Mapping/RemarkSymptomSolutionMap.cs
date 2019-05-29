using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class RemarkSymptomSolutionMap : ClassMap<RemarkSymptomSolution>
    {
        public RemarkSymptomSolutionMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Description).Length(2500);
            References(x => x.RemarkSymptomSolutionType).Not.Nullable().Not.LazyLoad();
            References(x => x.ComponentAssembly).Not.LazyLoad();
            Map(x => x.PreviousComponentSerial);
            Map(x => x.ComponentSerial);
            Map(x => x.SolutionDate);
            Map(x => x.Successful);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            Map(x => x.EditedBy);
            Map(x => x.MaterialCost);
            Map(x => x.TimeSpent);
            HasMany(x => x.RemarkImages).AsBag().Not.LazyLoad();
        }
    }
}
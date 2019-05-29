using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public class RemarkSymptomCauseMap : ClassMap<RemarkSymptomCause>
    {
        public RemarkSymptomCauseMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Description).Length(2500);
            Map(x => x.CauseDate);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            Map(x => x.EditedBy);
            Map(x => x.MaterialCost);
            Map(x => x.TimeSpent);
            References(x => x.CauseType).Not.Nullable().Not.LazyLoad();
            References(x => x.ComponentAssembly).Not.LazyLoad();
            References(x => x.RemarkSymptom).Not.Nullable().Not.LazyLoad();
            References(x => x.RemarkSymptomSolution).Not.LazyLoad();
            HasMany(x => x.RemarkImages).AsBag().Not.LazyLoad();
        }
    }
}
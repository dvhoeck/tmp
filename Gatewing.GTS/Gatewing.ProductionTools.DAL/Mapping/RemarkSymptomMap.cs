using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public class RemarkSymptomMap : ClassMap<RemarkSymptom>
    {
        public RemarkSymptomMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Description).Length(2500);
            Map(x => x.Resolved);
            Map(x => x.CreationDate);
            Map(x => x.ResolutionDate);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            Map(x => x.EditedBy);
            References(x => x.ProductAssembly).Not.Nullable().Not.LazyLoad();
            References(x => x.RemarkSymptomType).Not.Nullable().Not.LazyLoad();
            HasMany(x => x.RemarkSymptomCauses).Inverse().AsBag().Not.LazyLoad();
            HasMany(x => x.RemarkImages).AsBag().Not.LazyLoad();
        }
    }
}
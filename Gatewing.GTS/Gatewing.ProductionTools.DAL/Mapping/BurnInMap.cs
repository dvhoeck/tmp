using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public class BurnInMap :  ClassMap<BurnIn>
    {
        public BurnInMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.EboxId);
            Map(x => x.Lifespan);
            Map(x => x.LastCheck);
            Map(x => x.LogicallyDeleted);
            Map(x => x.IsBurnedIn);
            HasMany(x => x.BurnInSessions).Inverse().AsBag().Not.LazyLoad();
        }
    }
}

using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.TestManager.DAL
{
    public class BurnInTestMap : ClassMap<BurnInTest>
    {
        public BurnInTestMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.EboxId);
            Map(x => x.Lifespan);
            Map(x => x.LogicallyDeleted);
            Map(x => x.LastCheck);
            Map(x => x.Position);
        }
    }
}

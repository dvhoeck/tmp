using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL.Mapping
{
    public class BurnInSessionMap: ClassMap<BurnInSession>
    {
        public BurnInSessionMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Position);
            Map(x => x.StartOfSession);
            Map(x => x.EndOfSession);
            References(x => x.BurnIn).Not.Nullable();
        }
    }
}

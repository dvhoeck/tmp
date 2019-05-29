using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.TestManager.DAL.Mapping
{
    public class ServoHistoryMap : ClassMap<ServoHistory>
    {
        public ServoHistoryMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.ServoIndex);
            Map(x => x.ServoId);
            Map(x => x.Start);
            Map(x => x.Stop);
            Map(x => x.LifeSpan);
        }
    }
}

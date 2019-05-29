using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.TestManager.DAL.Mapping
{
    public class ServoFailMap : ClassMap<ServoFail>
    {
        public ServoFailMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.WiringBoxLocation);
            Map(x => x.StartTime);
            Map(x => x.FailTime);
        }
    }
}

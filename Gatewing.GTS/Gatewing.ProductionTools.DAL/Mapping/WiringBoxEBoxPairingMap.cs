using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.TestManager.DAL.Mapping
{
    public class WiringBoxEBoxPairingMap : ClassMap<WiringBoxEBoxPairing>
    {
        public WiringBoxEBoxPairingMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.WiringBox);
            Map(x => x.EBox);
        }
    }
}

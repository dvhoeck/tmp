using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.TestManager.DAL
{
    internal class ServoMap : ClassMap<Servo>
    {
        public ServoMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.ServoId);
            Map(x => x.Lifespan);
            Map(x => x.Position);
            Map(x => x.LastCheck);
            //            Map(x => x.Archived);
            Map(x => x.LogicallyDeleted);
            Map(x => x.Used);
        }
    }
}
using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.TestManager.DAL
{
    public class ServoFailMeanCalculationMap : ClassMap<ServoFailMeanCalculation>
    {
        public ServoFailMeanCalculationMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.MeanRunTime);
            Map(x => x.ServoFailCount);
        }
    }
}

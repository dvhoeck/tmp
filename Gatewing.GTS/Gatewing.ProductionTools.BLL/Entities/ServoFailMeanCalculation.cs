using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class ServoFailMeanCalculation: DomainObject
    {
        public ServoFailMeanCalculation()
        { }

        public virtual TimeSpan MeanRunTime { get; set; }
        public virtual int ServoFailCount { get; set; }
    }
}

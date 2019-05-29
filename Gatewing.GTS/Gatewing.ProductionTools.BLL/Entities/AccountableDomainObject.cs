using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// This is a class derived from DomainObject with additional properties to keep
    /// track of costs.
    /// </summary>
    public class AccountableDomainObject : DomainObject
    {
        /*
        public virtual double CostEstimate
        {
            get
            {
                if (CostFactors == null)
                    return 0;

                var total = 0d;
                var parseVar = 0d;
                foreach (var pair in CostFactors)
                {
                    parseVar = 0;
                    if (double.TryParse(pair.Key.ToString(), out parseVar))
                        total += parseVar;
                }
                return total;
            }
        }

        public virtual Dictionary<object, string> CostFactors { get; set; }
        */

        public virtual decimal MaterialCost { get; set; }
        public virtual decimal TimeSpent { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class PartNumber : DomainObject
    {
        public virtual string Number { get; set; }

        public virtual ProductModel ProductModel { get; set; }
    }
}
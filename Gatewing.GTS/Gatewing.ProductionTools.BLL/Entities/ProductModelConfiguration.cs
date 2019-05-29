using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class ProductModelConfiguration : ArchivableDomainObject
    {
        public virtual string Color { get; set; }
        public virtual int ConfigIndex { get; set; }
        public virtual string Name { get; set; }
    }
}
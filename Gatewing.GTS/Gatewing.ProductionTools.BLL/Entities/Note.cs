using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class Note : DomainObject
    {
        public virtual string Text { get; set; }
        public virtual ProductAssembly ProductAssembly { get; set; }
        public virtual DateTime CreationDate { get; set; }
    }
}
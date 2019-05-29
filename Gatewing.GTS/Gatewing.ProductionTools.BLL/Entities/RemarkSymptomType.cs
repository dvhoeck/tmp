using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gatewing.ProductionTools.BLL
{
    public class RemarkSymptomType : ArchivableDomainObject
    {
        public virtual string Name {get; set;}
        public virtual string Description { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
namespace Gatewing.ProductionTools.BLL
{
    public class RemarkSymptomSolutionType: ArchivableDomainObject
    {
        [Required]
        public virtual string Name { get; set; }

        public virtual string Description { get; set; }
    }
}
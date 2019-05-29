using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public class RemarkSymptomSolution : AccountableDomainObject
    {
        [Required]
        public virtual string Description { get; set; }

        [Required]
        [Display(Name = "Solution type")]
        public virtual RemarkSymptomSolutionType RemarkSymptomSolutionType { get; set; }

        [Display(Name = "Component")]
        public virtual ComponentAssembly ComponentAssembly { get; set; }

        [Display(Name = "Previous component serial")]
        public virtual string PreviousComponentSerial { get; set; }

        [Display(Name = "Component serial")]
        public virtual string ComponentSerial { get; set; }

        public virtual DateTime SolutionDate { get; set; }

        public virtual bool Successful { get; set; }

        public virtual bool IsArchived { get; set; }
        public virtual string ArchivedBy { get; set; }
        public virtual DateTime? ArchivalDate { get; set; }

        [Display(Name = "Images")]
        public virtual IList<RemarkImage> RemarkImages { get; set; }
    }
}
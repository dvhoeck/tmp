using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public class RemarkSymptomCause : AccountableDomainObject
    {
        [Required]
        public virtual string Description { get; set; }

        [Display(Name = "Remark symptom")]
        public virtual RemarkSymptom RemarkSymptom { get; set; }

        [Display(Name = "Component")]
        public virtual ComponentAssembly ComponentAssembly { get; set; }

        [Required]
        [Display(Name = "Cause type")]
        public virtual RemarkSymptomCauseType CauseType { get; set; }

        public virtual RemarkSymptomSolution RemarkSymptomSolution { get; set; }
        public virtual DateTime CauseDate { get; set; }

        public virtual bool IsArchived { get; set; }
        public virtual string ArchivedBy { get; set; }
        public virtual DateTime? ArchivalDate { get; set; }

        [Display(Name = "Images")]
        public virtual IList<RemarkImage> RemarkImages { get; set; }
    }
}
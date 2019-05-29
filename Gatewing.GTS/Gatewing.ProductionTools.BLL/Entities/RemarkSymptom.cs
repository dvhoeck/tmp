using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Gatewing.ProductionTools.BLL
{
    public class RemarkSymptom : ArchivableDomainObject
    {
        [Display(Name = "Product")]
        public virtual ProductAssembly ProductAssembly { get; set; }

        [Display(Name = "Remark type")]
        public virtual RemarkSymptomType RemarkSymptomType { get; set; }

        [Required]
        public virtual string Description { get; set; }

        [CustomValidation(typeof(RemarkSymptom), "BR_RemarkResolution")]
        public virtual bool Resolved { get; set; }

        [Display(Name = "Symptom causes")]
        public virtual IList<RemarkSymptomCause> RemarkSymptomCauses { get; set; }

        [Display(Name = "Images")]
        public virtual IList<RemarkImage> RemarkImages { get; set; }

        public virtual DateTime CreationDate { get; set; }

        public virtual DateTime? ResolutionDate { get; set; }

        /// <summary>
        /// Custom validation method. Will check amount of images and logs uploaded
        /// </summary>
        /// <param name="AmountOfLogsUploaded"></param>
        /// <param name="pValidationContext"></param>
        /// <returns></returns>
        public static ValidationResult BR_RemarkResolution(bool Resolved, ValidationContext pValidationContext)
        {
            var remark = (RemarkSymptom)pValidationContext.ObjectInstance;
            if (remark.Resolved)
            {
                // check for at least one successful solution
                if (remark.RemarkSymptomCauses.Where(cause => cause.IsArchived == false && cause.RemarkSymptomSolution != null && cause.RemarkSymptomSolution.IsArchived == false && cause.RemarkSymptomSolution.Successful).Count() == 0)
                    return new ValidationResultWithSeverity("To resolve a remark, one of the remark's (unarchived) solutions must be set to 'successful'.", Severity.Warning);

                if (remark.RemarkSymptomType.Name == "RMA")
                {
                    // check for proper rma handling
                    if (remark.RemarkSymptomCauses.Where(cause =>
                            cause.CauseType.Name == "Needs Shipping" &&
                            cause.RemarkSymptomSolution.RemarkSymptomSolutionType.Name == "Re-shipped" &&
                            cause.RemarkSymptomSolution.Successful).Count() == 0)
                        return new ValidationResultWithSeverity("To resolve an RMA remark, one of the remark's causes must be of type \"Needs Shipping\" and that cause must have a solution of type \"Re-shipped\" that is set to 'successful'.", Severity.Warning);
                }
            }

            return ValidationResultWithSeverity.Success;
        }
    }
}
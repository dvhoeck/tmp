using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Gatewing.ProductionTools.BLL
{
    public class ComponentAssembly : ArchivableDomainObject
    {
        public virtual ProductComponent ProductComponent { get; set; }

        public virtual ProductAssembly ProductAssembly { get; set; }

        [CustomValidation(typeof(ComponentAssembly), "BR_ValidateSerial")]
        public virtual string Serial { get; set; }

        public virtual string Revision { get; set; }
        public virtual DateTime AssemblyDateTime { get; set; }

        public virtual bool? IsCompleted
        {
            get
            {
                if (ProductComponent.ComponentRequirement == ComponentRequirement.None) return null;
                if (ProductComponent.ComponentRequirement == ComponentRequirement.Serial) return !string.IsNullOrEmpty(Serial);
                if (ProductComponent.ComponentRequirement == ComponentRequirement.Revision) return !string.IsNullOrEmpty(Revision);
                if (ProductComponent.ComponentRequirement == ComponentRequirement.Both) return !string.IsNullOrEmpty(Serial) && !string.IsNullOrEmpty(Revision);
                return null;
            }
        }

        /// <summary>
        /// Custom validation method. If component references a model, the serial entered must conform to serial format of that product
        /// </summary>
        /// <param name="AmountOfLogsUploaded"></param>
        /// <param name="pValidationContext"></param>
        /// <returns></returns>
        public static ValidationResult BR_ValidateSerial(string serial, ValidationContext pValidationContext)
        {
            // don't validate empty serial values
            if (string.IsNullOrEmpty(serial))
                return ValidationResultWithSeverity.Success;

            var componentAssembly = (ComponentAssembly)pValidationContext.ObjectInstance;
            if (componentAssembly.ProductComponent.UnderlyingProductModel != null)
            {
                if (!Regex.IsMatch(serial, componentAssembly.ProductComponent.SerialInputMask))
                    return new ValidationResultWithSeverity("The serial entered for this component assembly (" + serial + ") doesn't match the referenced product's serial format (" + componentAssembly.ProductComponent.SerialInputMask + ").", Severity.Critical);
            }

            return ValidationResultWithSeverity.Success;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    [Serializable]
    public class DomainObject
    {
        private List<ValidationResult> _validationResults = new List<ValidationResult>();

        /// <summary>
        /// Identifier of this object
        /// </summary>
        public virtual Guid Id { get; set; }

        /// <summary>
        /// Name of the person that manipulated this entity
        /// </summary>
        public virtual string EditedBy { get; set; }

        /// <summary>
        /// Validates an object and throws a validation exception when this fails
        /// </summary>
        public virtual void Validate()
        {
            Validator.ValidateObject(this, new ValidationContext(this), true);
        }

        /// <summary>
        /// Validates an object and returns false when this fails. Will also place errors in the
        /// ValidationResults property
        /// </summary>
        /// <returns>True if object is valid</returns>
        public virtual bool IsValid()
        {
            _validationResults = new List<ValidationResult>();
            return Validator.TryValidateObject(this, new ValidationContext(this), _validationResults, true);
        }

        /// <summary>
        /// A list of validation results
        /// </summary>
        public virtual List<ValidationResult> ValidationResults
        {
            get { return _validationResults; }
        }

        /// <summary>
        /// Gets or sets the nh version, this is the internal NHibernate version number that's used for concurrency checking
        /// </summary>
        /// <value>
        /// The nh version.
        /// </value>
        public virtual int NHVersion { get; set; }
    }
}
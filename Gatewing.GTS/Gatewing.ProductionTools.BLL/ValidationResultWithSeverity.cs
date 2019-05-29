
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.ComponentModel.DataAnnotations.ValidationResult" />
    public class ValidationResultWithSeverity: ValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultWithSeverity"/> class.
        /// </summary>
        /// <param name="validationResult">The validation result.</param>
        /// <param name="severity">The severity.</param>
        public ValidationResultWithSeverity(ValidationResult validationResult, Severity severity) : base(validationResult)
        {
            Severity = severity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultWithSeverity"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="severity">The severity.</param>
        public ValidationResultWithSeverity(string errorMessage, Severity severity) : base(errorMessage)
        {
            Severity = severity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultWithSeverity"/> class.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="memberNames">The member names.</param>
        /// <param name="severity">The severity.</param>
        public ValidationResultWithSeverity(string errorMessage, IEnumerable<string> memberNames, Severity severity) : base(errorMessage, memberNames)
        {
            Severity = severity;
        }

        /// <summary>
        /// Gets the severity.
        /// </summary>
        /// <value>
        /// The severity.
        /// </value>
        public Severity Severity { get; private set; }

        /// <summary>
        /// Returns a list of base validation results
        /// </summary>
        /// <param name="results">The results.</param>
        /// <returns></returns>
        public static List<ValidationResult> BaseResults(List<ValidationResultWithSeverity> results)
        {
            var list = new List<ValidationResult>();
            return results.Select(result => new ValidationResult(result.ErrorMessage, result.MemberNames)).ToList();
        }
    }
}

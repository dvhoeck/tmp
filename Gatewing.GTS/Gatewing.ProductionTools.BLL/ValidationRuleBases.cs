using System;
using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// Validation attribute that demands that a boolean value must be true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MustBeTrueAttribute : ValidationAttribute
    {
        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override bool IsValid(object value)
        {
            return value != null && value is bool && (bool)value;
        }
    }

    /// <summary>
    /// Validation attribute that demands that a boolean value must be true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MustBeFalseAttribute : ValidationAttribute
    {
        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override bool IsValid(object value)
        {
            return value != null && value is bool && (bool)value == false;
        }
    }
}

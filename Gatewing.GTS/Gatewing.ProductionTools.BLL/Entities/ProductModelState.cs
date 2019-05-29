using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Gatewing.ProductionTools.BLL
{
    /// <summary>
    /// Represents a state of a product model
    /// </summary>
    /// <seealso cref="Gatewing.ProductionTools.BLL.ArchivableDomainObject" />
    public class ProductModelState : ArchivableDomainObject
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public virtual string Description { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [Required]
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or sets the prevent batch mode boolean.
        /// </summary>
        /// <value>
        /// The prevent batch mode.
        /// </value>
        public virtual bool PreventBatchMode { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
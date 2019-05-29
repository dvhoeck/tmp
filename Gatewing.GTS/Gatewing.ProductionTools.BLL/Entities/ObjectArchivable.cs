using System;

namespace Gatewing.ProductionTools.BLL
{
    public class ArchivableDomainObject : DomainObject
    {
        public virtual DateTime? ArchivalDate { get; set; }

        /// <summary>
        /// Name of the person that archived this entity
        /// </summary>
        public virtual string ArchivedBy { get; set; }

        public virtual bool IsArchived { get; set; }
    }
}
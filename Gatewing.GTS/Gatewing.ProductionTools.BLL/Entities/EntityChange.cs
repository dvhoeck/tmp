using System;

namespace Gatewing.ProductionTools.BLL
{
    public class EntityChange : DomainObject
    {
        public virtual Guid ChangedEntityId { get; set; }

        public virtual Type ChangedEntityType { get; set; }

        public virtual DateTime ChangeDate { get; set; }

        public virtual string ChangeMadeBy { get; set; }

        public virtual string ChangeDescription { get; set; }

        public virtual string OldValue { get; set; }

        public virtual string NewValue { get; set; }
    }
}
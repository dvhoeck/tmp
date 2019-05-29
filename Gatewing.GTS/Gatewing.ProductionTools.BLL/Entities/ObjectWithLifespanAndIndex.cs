using System;

namespace Gatewing.ProductionTools.BLL
{
    public class ObjectWithLifespanAndIndex: DomainObject
    {
        public virtual int Position { get; set; }
        public virtual TimeSpan Lifespan { get; set; }
        public virtual DateTime? LastCheck { get; set; }
        public virtual bool LogicallyDeleted { get; set; }
        //public virtual bool Archived { get; set; }
    }
}

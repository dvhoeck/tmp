using System;

namespace Gatewing.ProductionTools.BLL
{
    public class BurnInSession: DomainObject
    {
        public virtual DateTime StartOfSession { get; set; }
        public virtual DateTime? EndOfSession { get; set; }
        public virtual int Position { get; set; }
        public virtual BurnIn BurnIn { get; set; }
    }
}

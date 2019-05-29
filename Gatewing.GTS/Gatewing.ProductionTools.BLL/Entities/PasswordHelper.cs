using System;

namespace Gatewing.ProductionTools.BLL
{
    public class PasswordHelper : ArchivableDomainObject
    {
        public virtual string EmailAddress { get; set; }
        public virtual Guid Token { get; set; }
        public virtual DateTime RequestDate { get; set; }
    }
}
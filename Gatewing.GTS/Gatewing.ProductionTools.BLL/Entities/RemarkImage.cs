namespace Gatewing.ProductionTools.BLL
{
    public class RemarkImage : ArchivableDomainObject
    {
        public virtual string ImagePath { get; set; }

        public virtual string Description { get; set; }
    }
}
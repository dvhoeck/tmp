namespace Gatewing.ProductionTools.BLL
{
    public class ArchivedPublicProductSerial : ArchivableDomainObject
    {
        public virtual string InternalProductSerial { get; set; }
        public virtual ProductAssembly ProductAssembly { get; set; }
        public virtual string PublicProductSerial { get; set; }
    }
}
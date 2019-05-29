using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL.Mapping
{
    public class RemarkImageMap : ClassMap<RemarkImage>
    {
        public RemarkImageMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.ImagePath);
            Map(x => x.Description);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            Map(x => x.EditedBy);
        }
    }
}
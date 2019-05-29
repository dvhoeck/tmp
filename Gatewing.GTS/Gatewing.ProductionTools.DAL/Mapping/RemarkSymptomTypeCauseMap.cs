using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class RemarkSymptomCauseTypeMap : ClassMap<RemarkSymptomCauseType>
    {
        public RemarkSymptomCauseTypeMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Name);
            Map(x => x.Description);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            Map(x => x.EditedBy);
        }
    }
}
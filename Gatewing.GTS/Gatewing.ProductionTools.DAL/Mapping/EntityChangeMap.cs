using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class EntityChangeMap : ClassMap<EntityChange>
    {
        public EntityChangeMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.ChangedEntityId);
            Map(x => x.ChangedEntityType);
            Map(x => x.ChangeDate);
            Map(x => x.ChangeMadeBy);
            Map(x => x.ChangeDescription).Length(2500);
            Map(x => x.OldValue).Length(2500);
            Map(x => x.NewValue).Length(2500);
        }
    }
}
using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class ComponentAssemblyMap : ClassMap<ComponentAssembly>
    {
        public ComponentAssemblyMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.AssemblyDateTime);
            Map(x => x.Revision);
            Map(x => x.Serial);
            Map(x => x.EditedBy);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);

            References(x => x.ProductComponent).Not.Nullable().Not.LazyLoad();
            References(x => x.ProductAssembly).Not.Nullable().Not.LazyLoad();
        }
    }
}
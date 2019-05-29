using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class AssemblyToolMap : ClassMap<AssemblyTool>
    {
        public AssemblyToolMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            //Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.NHVersion);
            Map(x => x.Name);
            Map(x => x.ToolCode);
            Map(x => x.Setting);
            Map(x => x.Description);
            Map(x => x.EditedBy);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);

            //HasManyToMany(x => x.ProductModels).Table("ModelTools");

            //HasMany(x => x.ProductModels).Inverse().AsBag().Not.LazyLoad().Where(x => x.IsArchived == false);
        }
    }
}
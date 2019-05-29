using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class ProductModelConfigurationMap : ClassMap<ProductModelConfiguration>
    {
        public ProductModelConfigurationMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.Name);
            Map(x => x.Color);
            Map(x => x.ConfigIndex);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
        }
    }
}
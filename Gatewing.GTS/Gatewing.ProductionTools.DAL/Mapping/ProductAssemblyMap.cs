using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class ProductAssemblyMap : ClassMap<ProductAssembly>
    {
        public ProductAssemblyMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.StartDate);
            Map(x => x.EndDate);
            Map(x => x.ProductSerial);
            Map(x => x.PublicProductSerial);
            Map(x => x.EditedBy);
            Map(x => x.StartedBy);
            Map(x => x.CurrentStateIndex);
            References(x => x.ProductModel).Not.Nullable().Not.LazyLoad();
            References(x => x.ProductModelState).Not.Nullable().Not.LazyLoad();
            HasMany(x => x.ComponentAssemblies).Not.OptimisticLock().Inverse().AsBag().Not.LazyLoad();
            HasMany(x => x.RemarkSymptoms).Inverse().AsBag().Not.LazyLoad();

            HasManyToMany(x => x.ProductModelConfigurations).Table("ProductModelConfigurationsToProductAssembly").Not.OptimisticLock();
        }
    }
}
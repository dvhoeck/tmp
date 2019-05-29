using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public class ProductComponentMap : ClassMap<ProductComponent>
    {
        public ProductComponentMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.ComponentName);
            Map(x => x.UnderlyingProductModelId);
            References(x => x.UnderlyingProductModel).Nullable().Not.LazyLoad();
            References(x => x.ProductModel).Not.Nullable().Not.LazyLoad();
            References(x => x.ProductModelState).Not.Nullable().Not.LazyLoad();
            Map(x => x.DeviceKeyword);
            Map(x => x.SequenceOrder);
            Map(x => x.RevisionInputMask);
            Map(x => x.SerialInputMask);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ComponentRequirement).CustomType<ComponentRequirement>();
            HasMany(x => x.WorkInstructions).Inverse().AsBag().Not.LazyLoad().Not.OptimisticLock().Where(x => x.IsArchived == false);
            Map(x => x.ArchivedBy);
            Map(x => x.EditedBy);

            //HasMany(x => x.ProductModelConfigurationIndices).Table("TableName").KeyColumn("KeyColumnName").Element("ValueColumnName");
            //HasMany(x => x.ProductModelConfigurations).Not.OptimisticLock().AsBag().LazyLoad();

            HasManyToMany(x => x.ProductModelConfigurations).Table("ProductModelConfigurationsToProductComponents").Not.OptimisticLock();
        }
    }
}
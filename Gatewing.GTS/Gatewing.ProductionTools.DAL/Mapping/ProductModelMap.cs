using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public class ProductModelMap : ClassMap<ProductModel>
    {
        public ProductModelMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();

            Version(x => x.NHVersion).UnsavedValue("0");

            Map(x => x.Name);
            Map(x => x.Date);
            Map(x => x.IdMask);
            Map(x => x.PublicIdMask);
            Map(x => x.ModelType).CustomType<ModelType>();
            Map(x => x.Comment);
            Map(x => x.Version);
            Map(x => x.IsReleased);
            Map(x => x.BaseModelId);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            Map(x => x.EditedBy);
            Map(x => x.ReleaseComment);
            Map(x => x.ReleaseDate);

            HasManyToMany(x => x.Tooling).Table("ModelTools").Not.OptimisticLock();

            HasMany(x => x.ProductComponents).Inverse().Not.OptimisticLock().AsBag().LazyLoad();

            HasMany(x => x.PartNumbers).Not.OptimisticLock().AsBag().LazyLoad(); //.Cascade.DeleteOrphan();

            HasMany(x => x.ProductModelConfigurations).Not.OptimisticLock().AsBag().LazyLoad();
        }
    }
}
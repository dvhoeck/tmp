using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class PartNumberMap : ClassMap<PartNumber>
    {
        public PartNumberMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned().UnsavedValue("00000000-0000-0000-0000-000000000000");
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.Number);
            References<ProductModel>(x => x.ProductModel).Not.Nullable().LazyLoad(); ;
        }
    }
}
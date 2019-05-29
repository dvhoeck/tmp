using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.TestManager.DAL.Mapping
{
    public class WorkInstructionCategoryMap: ClassMap<WorkInstructionCategory>
    {
        public WorkInstructionCategoryMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Name);
            References(x => x.ParentWorkInstructionCategory).Nullable().Not.LazyLoad();
            Map(x => x.State).CustomType<int>();
            Map(x => x.SequenceIndex);
        }
    }
}

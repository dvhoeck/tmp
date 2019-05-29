using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.TestManager.DAL
{
    public class WorkInstructionMap: ClassMap<WorkInstruction>
    {
        public WorkInstructionMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Caption);
            Map(x => x.Description);
            Map(x => x.ImageUrl);
            Map(x => x.ToolingDescription);
            Map(x => x.State).CustomType<int>();
            Map(x => x.SequenceIndex);
            References(x => x.Category).Nullable().Not.LazyLoad(); 
        }
    }
}

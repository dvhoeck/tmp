using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL.Mapping
{
    public class GTSWorkInstructionMap : ClassMap<GTSWorkInstruction>
    {
        public GTSWorkInstructionMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.Title);
            Map(x => x.RelativeImagePath);
            Map(x => x.Description).Length(2500);
            Map(x => x.SequenceOrder);
            Map(x => x.EditedBy);
            Map(x => x.CreationDate);
            Map(x => x.ModificationDate);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            References(x => x.ProductComponent).Not.OptimisticLock().Not.Nullable().Not.LazyLoad();
        }
    }
}
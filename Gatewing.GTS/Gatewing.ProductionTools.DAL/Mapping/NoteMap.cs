using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class NoteMap : ClassMap<Note>
    {
        public NoteMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            //Version(x => x.NHVersion).UnsavedValue("0"); => no update possible, only create
            Map(x => x.Text).Length(1500);
            Map(x => x.EditedBy);
            Map(x => x.CreationDate);
            References(x => x.ProductAssembly).Not.Nullable().LazyLoad();
        }
    }
}
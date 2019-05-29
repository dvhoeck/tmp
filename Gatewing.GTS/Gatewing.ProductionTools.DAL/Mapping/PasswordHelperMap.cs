using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL.Mapping
{
    public class PasswordHelperMap : ClassMap<PasswordHelper>
    {
        public PasswordHelperMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.EmailAddress);
            Map(x => x.Token);
            Map(x => x.RequestDate);
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
        }
    }
}
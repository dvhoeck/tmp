using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public class ArchivedPublicProductSerialMap : ClassMap<ArchivedPublicProductSerial>
    {
        public ArchivedPublicProductSerialMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Version(x => x.NHVersion).UnsavedValue("0");
            Map(x => x.IsArchived);
            Map(x => x.ArchivalDate);
            Map(x => x.ArchivedBy);
            Map(x => x.InternalProductSerial);
            Map(x => x.PublicProductSerial);

            References(x => x.ProductAssembly).Not.Nullable().Not.LazyLoad();
        }
    }
}
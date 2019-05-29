using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.ProductionTools.DAL
{
    public class RemoveDeviceMap : ClassMap<RemoteDevice>
    {
        public RemoveDeviceMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Name);
            Map(x => x.Address);
            Map(x => x.DeviceKey);
            Map(x => x.LastOnline);
        }
    }
}
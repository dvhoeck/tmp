using FluentNHibernate.Mapping;
using Gatewing.ProductionTools.BLL;

namespace Gatewing.TestManager.DAL
{
    public class GBoxStatusMap: ClassMap<GBoxStatus>
    {
        public GBoxStatusMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.GBoxID);
            Map(x => x.IsFirmwareFlashed);
            Map(x => x.IsDeviceAuthorized);
            Map(x => x.IsDeviceConfigured);
            Map(x => x.IsCamTriggerFedBack);
            Map(x => x.DoesDeviceTestOk);
        }
    }
}

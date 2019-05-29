using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class RemoteDevice : DomainObject
    {
        public virtual string Name { get; set; }
        public virtual string Address { get; set; }
        public virtual Guid DeviceKey { get; set; }
        public virtual DateTime LastOnline { get; set; }
    }
}
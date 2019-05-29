using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class AssemblyTool : ArchivableDomainObject
    {
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual string ToolCode { get; set; }
        public virtual string Setting { get; set; }
        /*
        private ICollection<ProductModel> _productModels = new List<ProductModel>();

        public virtual ICollection<ProductModel> ProductModels
        {
            get { return _productModels; }
            set { _productModels = value; }
        }
        */

        public virtual AssemblyTool Clone()
        {
            var clonedTool = (AssemblyTool)this.MemberwiseClone();
            clonedTool.Id = Guid.NewGuid();

            return clonedTool;
        }
    }
}
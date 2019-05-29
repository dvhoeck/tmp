using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public class ObjectWithSequenceNumber: DomainObject
    {
        [Display(Name = "Sequence Index")]
        public virtual int SequenceIndex { get; set; }
    }
}

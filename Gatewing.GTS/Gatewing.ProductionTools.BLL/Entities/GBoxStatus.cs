using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public class GBoxStatus: DomainObject
    {
        [Display(Name = "GBox ID")]
        [RegularExpression("(GBX|FCS)+[0-9]{5}", ErrorMessage = "A gBox ID must start with GBX followed by a 5 digit serial number")]
        public virtual string GBoxID { get; set; }

        [Display(Name = "Updated?")]
        public virtual bool IsFirmwareFlashed { get; set; }

        [Display(Name = "Authorized?")]
        public virtual bool IsDeviceAuthorized { get; set; }

        [Display(Name = "Configured?")]
        public virtual bool IsDeviceConfigured { get; set; }

        [Display(Name = "Cam feedback?")]
        public virtual bool IsCamTriggerFedBack { get; set; }

        [Display(Name = "Test OK?")]
        public virtual bool DoesDeviceTestOk { get; set; }
    }
}

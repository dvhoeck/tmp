using System;
using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public class GBoxID: DomainObject
    {
        private string _gboxID;

        [Display(Name = "gBox ID")]
        [RegularExpression("(GBX)+[0-9]{5}", ErrorMessage = "A gBox ID must start with GBX followed by a 5 digit serial number. For example: \"GBX00001\".")]
        [StringLength(8)]
        public string ID 
        {
            get { return _gboxID; }
            set { _gboxID = value; }
        }

        public GBoxID(string gboxID)
        {
            _gboxID = gboxID;
        }

        public override string ToString()
        {
            return ID;
        }

        [Display(Name = "gBox Serial")]
        public int Serial { get {  return Convert.ToInt32(ID.Substring(3)); } }
    }
}

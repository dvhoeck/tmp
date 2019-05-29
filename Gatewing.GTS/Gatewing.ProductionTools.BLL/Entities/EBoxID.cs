using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class EBoxID: DomainObject
    {
        private string _eboxID;

        [Display(Name = "eBox ID")]
        [RegularExpression("(EBX)+[0-9]{5}", ErrorMessage = "An eBox ID must start with EBX followed by a 5 digit serial number. For example: \"EBX00001\".")]
        [StringLength(8)]
        public string ID 
        {
            get { return _eboxID; }
            set { _eboxID = value; }
        }

        public EBoxID(string eboxID)
        {
            _eboxID = eboxID;
        }

        public override string ToString()
        {
            return ID;
        }

        [Display(Name = "eBox Serial")]
        public int Serial { get {  return Convert.ToInt32(ID.Substring(3)); } }
    }
}

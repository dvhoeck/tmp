using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class ModemID : DomainObject
    {
        private string _modemID;

        [Display(Name = "Modem ID")]
        [RegularExpression("(MDG)+[0-9]{5}", ErrorMessage = "A modem ID must start with MDG followed by a 5 digit serial number. For example: \"MDG00001\".")]
        [StringLength(8)]
        public string ID 
        {
            get { return _modemID; }
            set { _modemID = value; }
        }

        public ModemID(string modemID)
        {
            _modemID = modemID;
        }

        public override string ToString()
        {
            return ID;
        }
    }
}

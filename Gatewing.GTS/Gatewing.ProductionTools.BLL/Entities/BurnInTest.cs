using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class BurnInTest : ObjectWithLifespanAndIndex
    {
        public BurnInTest() { }
        public BurnInTest(string eboxId)
        {
            Id = Guid.NewGuid();
            EboxId = eboxId;
        }

        
        [Display(Name="Ebox ID")]
        [RegularExpression("(EBX)+[0-9]{5}", ErrorMessage="An eBox ID must start with EBX followed by a 5 digit serial number")]
        [StringLength(8)]
        public virtual string EboxId { get; set; }


        [Display(Name = "Total time spent")]
        public virtual string TotalTimeString 
        {
            get { return string.Format("{0}:{1}:{2}", ((int)Lifespan.TotalHours).ToString().PadLeft(2, '0'), Lifespan.Minutes.ToString().PadLeft(2, '0'), Lifespan.Seconds.ToString().PadLeft(2, '0')); }
        }
        

        public virtual string IndexAsString { get { return Position.ToString().PadLeft(2, '0'); } }
    }

}

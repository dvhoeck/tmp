using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class Servo: ObjectWithLifespanAndIndex
    {
        [Display(Name = "Servo Id")]
        public virtual string ServoId { get; set; }

        [Display(Name = "Total time spent")]
        public virtual string TotalTimeString
        {
            get { return string.Format("{0}:{1}:{2}", ((int)Lifespan.TotalHours).ToString().PadLeft(2, '0'), Lifespan.Minutes.ToString().PadLeft(2, '0'), Lifespan.Seconds.ToString().PadLeft(2, '0')); }
        }

        [Display(Name="Used Servo")]
        public virtual bool Used { get; set; }
    }
}

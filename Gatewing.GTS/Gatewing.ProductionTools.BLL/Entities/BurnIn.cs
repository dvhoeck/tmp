
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Gatewing.ProductionTools.BLL
{
    public class BurnIn: ObjectWithLifespanAndIndex
    {
        [Display(Name = "Ebox ID")]
        [RegularExpression("(EBX)+[0-9]{5}", ErrorMessage = "An eBox ID must start with EBX followed by a 5 digit serial number")]
        [StringLength(8)]
        public virtual string EboxId { get; set; }

        [Display(Name = "Burn in sessions ID")]
        public virtual IList<BurnInSession> BurnInSessions { get; set; }

        public virtual BurnInSession ActiveSession
        {
            get
            {
                return BurnInSessions.Where(session => session.EndOfSession == null).OrderBy(session => session.StartOfSession).LastOrDefault();
            }
        }

        [Display(Name = "Is burn-in complete?")]
        public virtual bool IsBurnedIn { get; set; }

        [Display(Name = "Total time spent")]
        public virtual string TotalTimeString
        {
            get { return string.Format("{0}:{1}:{2}", ((int)Lifespan.TotalHours).ToString().PadLeft(2, '0'), Lifespan.Minutes.ToString().PadLeft(2, '0'), Lifespan.Seconds.ToString().PadLeft(2, '0')); }
        }
    }
}

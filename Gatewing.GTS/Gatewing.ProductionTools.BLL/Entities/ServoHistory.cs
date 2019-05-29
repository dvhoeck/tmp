using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class ServoHistory: DomainObject
    {
        [Display(Name = "Servo Index")]
        public virtual int ServoIndex { get; set; }

        

        [Display(Name = "Servo Id")]
        [RegularExpression("(SRV)+[0-9]{5}", ErrorMessage = "An Servo ID must start with SRV followed by a 5 digit serial number. For example: \"SRV00001\".")]
        [StringLength(8)]
        public virtual string ServoId { get; set; }

        /*
        
        [Display(Name = "Start time")]
        public virtual DateTime StartTime { get; set; }

        [Display(Name = "Fail time")]
        public virtual DateTime FailTime { get; set; }
        */

        private TimeSpan _existingLifeSpan;
        [Display(Name = "Life Span")]
        public virtual TimeSpan LifeSpan 
        {
            get
            {
                if (_stop == DateTime.MinValue)
                    return DateTime.Now - _start;

                return _stop - _start;
            }
            set
            {
                _existingLifeSpan = value;
            }
        }

        private DateTime _start;
        [Display(Name = "Test started")]
        public virtual DateTime Start
        {
            get { return _start; }
            set
            {
                _start = value;
           }
        }


        private DateTime _stop;
        [Display(Name = "Test stopped")]
        public virtual DateTime Stop
        {
            get
            {
                return _stop;
            }
            set
            {
                    _stop = value;
                
            }
        }

        [Display(Name = "Servo health")]
        public virtual int CalculateHealth(double meanSeconds)
        {
            return Convert.ToInt32((LifeSpan.TotalSeconds / meanSeconds) * 100);
        }
    }
}

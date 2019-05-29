using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public class GTSWorkInstruction : ArchivableDomainObject
    {
        public virtual string Title { get; set; }
        public virtual string Description { get; set; }

        private string _relativeImagePath = string.Empty;

        public virtual string RelativeImagePath
        {
            get { return _relativeImagePath == string.Empty ? "placeholder.png" : _relativeImagePath; }
            set { _relativeImagePath = value; }
        }

        [Display(Name = "Instruction creation date")]
        public virtual DateTime CreationDate { get; set; }

        [Display(Name = "Instruction modification date")]
        public virtual DateTime ModificationDate { get; set; }

        public virtual int SequenceOrder { get; set; }

        public virtual ProductComponent ProductComponent { get; set; }

        public virtual GTSWorkInstruction Clone()
        {
            var cloneInstruction = (GTSWorkInstruction)this.MemberwiseClone();
            cloneInstruction.Id = Guid.NewGuid();
            cloneInstruction.NHVersion = 0;

            return cloneInstruction;
        }
    }
}
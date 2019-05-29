using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public class WorkInstructionCategory: ObjectWithSequenceNumber
    {
        public virtual string Name { get; set; }

        [Display(Name="Parent Category")]
        public virtual WorkInstructionCategory ParentWorkInstructionCategory { get; set; }

        public virtual State State { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}

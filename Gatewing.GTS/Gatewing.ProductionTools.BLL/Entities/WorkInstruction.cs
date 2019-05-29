using System.ComponentModel.DataAnnotations;

namespace Gatewing.ProductionTools.BLL
{
    public class WorkInstruction: ObjectWithSequenceNumber
    {
        public virtual string Caption { get; set; }

        public virtual string Description { get; set; }

        [Display(Name="Tooling")]
        public virtual string ToolingDescription { get; set; }

        [Display(Name = "Image")]
        public virtual string ImageUrl { get; set; }

        public virtual State State { get; set; }

        [Display(Name = "Work Instruction Category")]
        public virtual WorkInstructionCategory Category { get; set; }
    }
}

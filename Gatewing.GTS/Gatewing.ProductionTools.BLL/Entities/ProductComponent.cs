using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Gatewing.ProductionTools.BLL
{
    public class ProductComponent : ArchivableDomainObject
    {
        private IList<GTSWorkInstruction> _workInstructions = new List<GTSWorkInstruction>();

        [Display(Name = "Name")]
        public virtual string ComponentName { get; set; }

        [Display(Name = "Requirement")]
        public virtual ComponentRequirement ComponentRequirement { get; set; }

        [Display(Name = "Device keyword")]
        public virtual string DeviceKeyword { get; set; }

        [Display(Name = "Product")]
        public virtual ProductModel ProductModel { get; set; }

        public virtual IList<ProductModelConfiguration> ProductModelConfigurations { get; set; }

        [Display(Name = "State")]
        public virtual ProductModelState ProductModelState { get; set; }

        [Display(Name = "Revision input mask")]
        public virtual string RevisionInputMask { get; set; }

        [Display(Name = "Order")]
        public virtual int SequenceOrder { get; set; }

        [Display(Name = "Revision input mask")]
        public virtual string SerialInputMask { get; set; }

        [Display(Name = "Reference")]
        public virtual ProductModel UnderlyingProductModel { get; set; }

        [Display(Name = "Reference")]
        public virtual Guid UnderlyingProductModelId { get; set; }

        public virtual IList<GTSWorkInstruction> WorkInstructions
        {
            get { return _workInstructions.Where(x => x.IsArchived == false).OrderBy(x => x.SequenceOrder).ToList(); }
            set { _workInstructions = value; }
        }

        public virtual ProductComponent Clone()
        {
            // clone component
            var clonedComponent = (ProductComponent)this.MemberwiseClone();
            clonedComponent.Id = Guid.NewGuid();
            clonedComponent.NHVersion = 0;

            // clone work instructions
            var workInstructionList = new List<GTSWorkInstruction>();

            GTSWorkInstruction placeHolder;
            WorkInstructions.ToList().ForEach(x =>
            {
                placeHolder = x.Clone();
                placeHolder.ProductComponent = clonedComponent;
                workInstructionList.Add(placeHolder);
            });

            // assign clone child collection to cloned model
            clonedComponent.WorkInstructions = workInstructionList;

            // clone product model configurations
            var productModelConfigurations = new List<ProductModelConfiguration>();
            ProductModelConfiguration configPlaceholder;
            ProductModelConfigurations.ToList().ForEach(x =>
            {
                configPlaceholder = new ProductModelConfiguration();
                configPlaceholder.Id = Guid.NewGuid();
                configPlaceholder.Name = x.Name;
                configPlaceholder.ConfigIndex = x.ConfigIndex;
                configPlaceholder.Color = x.Color;

                productModelConfigurations.Add(configPlaceholder);
            });

            // assign collection
            clonedComponent.ProductModelConfigurations = productModelConfigurations;

            return clonedComponent;
        }
    }
}
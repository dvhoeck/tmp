using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

/// <summary>
///
/// </summary>
namespace Gatewing.ProductionTools.BLL
{
    public class ProductModel : ArchivableDomainObject
    {
        [Display(Name = "Product components")]
        private IList<ProductComponent> _productComponents = new List<ProductComponent>();

        private IList<AssemblyTool> _tooling = new List<AssemblyTool>();

        public virtual Guid BaseModelId { get; set; }

        [Required]
        public virtual string Comment { get; set; }

        [Required]
        public virtual DateTime Date { get; set; }

        [Display(Name = "Id format")]
        public virtual string IdMask { get; set; }

        public virtual bool IsReleased { get; set; }

        [Display(Name = "Model type")]
        public virtual ModelType ModelType { get; set; }

        [Required]
        public virtual string Name { get; set; }

        public virtual IList<PartNumber> PartNumbers { get; set; }

        public virtual string PrettyName => string.Format("{0} v{1}", Name, Version);

        public virtual IList<ProductComponent> ProductComponents
        {
            get { return _productComponents.Where(x => x.IsArchived == false).OrderBy(x => x.SequenceOrder).ToList(); }
            set { _productComponents = value; }
        }

        public virtual IList<ProductModelConfiguration> ProductModelConfigurations { get; set; }

        [Display(Name = "Public id format")]
        public virtual string PublicIdMask { get; set; }

        public virtual string ReleaseComment { get; set; }

        public virtual DateTime? ReleaseDate { get; set; }

        public virtual IList<AssemblyTool> Tooling
        {
            get { return _tooling; }
            set { _tooling = value; }
        }

        [Required]
        public virtual int Version { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public virtual ProductModel Clone(bool createVersion = true, string modelName = "", string idMask = "", string description = "")
        {
            // clone item (without child collections), increase version
            var clonedModel = (ProductModel)this.MemberwiseClone();
            clonedModel.Id = Guid.NewGuid();
            clonedModel.IsReleased = false;
            clonedModel.IsArchived = false;

            if (createVersion)
            {
                // create a new version
                clonedModel.Version = Version + 1;
            }
            else
            {
                if (string.IsNullOrEmpty(modelName) || string.IsNullOrEmpty(idMask) || string.IsNullOrEmpty(description))
                    throw new ArgumentNullException("Pass a valid name, id mask and description if a model needs to be cloned.");

                // or create a new model with new name, 0 version and new base model id
                clonedModel.Name = modelName;
                clonedModel.Version = 0;
                clonedModel.BaseModelId = Guid.NewGuid();
                clonedModel.IdMask = IdMask;
                clonedModel.Comment = description;
            }

            clonedModel.NHVersion = 0;

            // clone each child element and assign cloned item as parent
            var productComponentList = new List<ProductComponent>();
            ProductComponent placeHolder;
            ProductComponents.ToList().ForEach(x =>
            {
                placeHolder = x.Clone();
                placeHolder.ProductModel = clonedModel;
                productComponentList.Add(placeHolder);
            });

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

            // assign collection to ProductModelConfigurations property
            clonedModel.ProductModelConfigurations = productModelConfigurations;

            // clone product part numbers
            var productModelPartNrs = new List<PartNumber>();
            PartNumber placeholder;
            PartNumbers.ToList().ForEach(x =>
            {
                placeholder = new PartNumber();
                placeholder.Id = Guid.NewGuid();
                placeholder.Number = x.Number;
                placeholder.ProductModel = clonedModel;

                productModelPartNrs.Add(placeholder);
            });

            //assign part numbers to part nrs property
            clonedModel.PartNumbers = productModelPartNrs;

            // create a new list object or a "Shared collection" exception will be thrown
            clonedModel.Tooling = new List<AssemblyTool>();
            ((List<AssemblyTool>)clonedModel.Tooling).AddRange(Tooling);

            // assign clone child collection to cloned model
            clonedModel.ProductComponents = productComponentList;

            return clonedModel;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
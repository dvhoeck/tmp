using Gatewing.ProductionTools.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
///
/// </summary>
namespace Gatewing.ProductionTools.BLL
{
    public class ProductAssembly : DomainObject
    {
        public virtual IList<ComponentAssembly> ComponentAssemblies { get; set; }
        public virtual int CurrentStateIndex { get; set; } = 0;
        public virtual DateTime EndDate { get; set; }

        /// <summary>
        /// Gets the evaluation.
        /// </summary>
        /// <value>
        /// The evaluation.
        /// </value>
        public virtual string Evaluation
        {
            get
            {
                //return Enum.GetName(typeof(EvaluationState), EvaluationState).ToString();

                return EvaluationState.ToFriendlyName();
            }
        }

        /// <summary>
        /// Gets the state of the evaluation.
        /// </summary>
        /// <value>
        /// The state of the evaluation.
        /// </value>
        public virtual EvaluationState EvaluationState
        {
            get
            {
                if (Progress >= 100 || ProductModelState.Name.ToLower() == "delivered" || ProductModelState.Name.ToLower() == "scrapped")
                {
                    if (ProductModelState.Name.ToLower() == "scrapped")
                        return EvaluationState.Rejected;

                    if (RemarkSymptoms.Count == 0)
                        return EvaluationState.AcceptedWithoutRemarks;

                    if (RemarkSymptoms.All(rs => rs.IsArchived == true || rs.Resolved == true))
                        return EvaluationState.AcceptedWithRemarks;

                    if (RemarkSymptoms.Where(rs => rs.IsArchived == false && rs.Resolved == false).Count() > 0)
                        return EvaluationState.BlockedByRemarks;

                    return EvaluationState.Rejected;
                }
                else
                {
                    return EvaluationState.InProgress;
                }
            }
        }

        public virtual ProductModel ProductModel { get; set; }
        public virtual IList<ProductModelConfiguration> ProductModelConfigurations { get; set; }
        public virtual ProductModelState ProductModelState { get; set; }
        public virtual string ProductSerial { get; set; }

        public virtual int Progress
        {
            get
            {
                if (ComponentAssemblies.Count == 0)
                    return 100;

                Tmp(ComponentAssemblies);

                var completed = ComponentAssemblies.Count(e =>
                    e.IsCompleted != null &&
                    e.IsCompleted == true &&
                    e.ProductComponent.ProductModelConfigurations.Where(config => this.ProductModelConfigurations.Where(currentConfig => currentConfig.Name == config.Name || config.Name == "default").Count() > 0).Count() > 0);

                var total = ComponentAssemblies.Count(e =>
                    e.IsCompleted != null &&
                    e.ProductComponent.ProductModelConfigurations.Where(config => this.ProductModelConfigurations.Where(currentConfig => currentConfig.Name == config.Name || config.Name == "default").Count() > 0).Count() > 0);

                if (total == 0 || completed == 0)
                    return 0;

                return Convert.ToInt32((double)completed / (double)total * 100);
            }
        }

        public virtual string PublicProductSerial { get; set; }

        public virtual IList<RemarkSymptom> RemarkSymptoms { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual string StartedBy { get; set; }

        private void Tmp(IList<ComponentAssembly> componentAssemblies)
        {
            foreach (var item in componentAssemblies)
            {
                foreach (var subItem1 in item.ProductComponent.ProductModelConfigurations)
                {
                    var tmp2 = this.ProductModelConfigurations.Where(currentConfig => currentConfig.Name == subItem1.Name).Count() > 0;
                }

                var tmp = item.ProductComponent.ProductModelConfigurations.Where(config => this.ProductModelConfigurations.Contains(config)).Count() > 0;
            }
        }
    }
}
using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.DAL;
using Gatewing.ProductionTools.ExtensionMethods;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Gatewing.ProductionTools.GTS.Controllers
{
    [Authorize]
    [OutputCache(VaryByParam = "*", Duration = 0, NoStore = true)] // disable caching for all actions
    public class ReportsController : BaseController
    {
        private DateTime _finalEndDate = new DateTime(9999, 12, 29, 23, 59, 59, 0);
        private DateTime _minDate = new DateTime(1980, 1, 1); // DateTime.MinValue => causes an sql datetime overflow

        /*
        public ActionResult GetAssembliesBetweenDates()
        {
            Console.WriteLine("hier");

            return null;
        }

        [HttpPost]
        public ActionResult GetAssembliesBetweenDates(DateTime? start, DateTime? end, string componentName, string componentValue)
        {
            _logger.LogInfo("User {0} requests a report of Assemblies between dates {1} and {2}.", User.Identity.Name, start != null ? start.ToString() : "not-entered", end != null ? end.ToString() : "not-entered");

            // handle caps: put everything in lower case, even component names
            if (!string.IsNullOrEmpty(componentName))
            {
                componentName = componentName.ToLower();

                // handle wildcards
                componentName = (componentName ?? "").Replace('*', '%');
            }
            if (!string.IsNullOrEmpty(componentValue))
                componentValue = componentValue.ToLower();

            try
            {
                // create a criteria set to list ProductAssemblies
                var assemblies = UnitOfWorkSession.CreateCriteria<ProductAssembly>();

                // add start date to criteria, if available
                if (start != null)
                    assemblies = assemblies.Add(Expression.Gt("StartDate", start ?? _minDate));

                // add end date to criteria, if available and take enddate near max date into account
                if (end != null)
                    assemblies = assemblies.Add(Expression.Lt("EndDate", end) || Expression.Eq("EndDate", _finalEndDate));
                else
                    assemblies = assemblies.Add(Expression.Eq("EndDate", end));

                // if a component name is specified
                if (!string.IsNullOrEmpty(componentName))
                {
                    // filter on ComponentAssemblies
                    assemblies = assemblies.CreateCriteria("ComponentAssemblies");

                    // use component value to filter, if needed
                    if (!string.IsNullOrEmpty(componentValue))
                        assemblies = assemblies.Add(Expression.Like("Revision", componentValue));

                    // take start and end dates into account, if available
                    assemblies = assemblies
                        .Add(Expression.Gt("AssemblyDateTime", start ?? _minDate))
                        .Add(Expression.Lt("AssemblyDateTime", end) || Expression.Eq("AssemblyDateTime", _finalEndDate))
                        .CreateCriteria("ProductComponent")
                        .Add(Expression.Like("ComponentName", componentName));
                }

                // using criteria, retrieve result
                var result = assemblies.List<ProductAssembly>();

                var componentNameWithoutWildcards = componentName?.Replace("%", "");

                // simplify into DTO objects
                var simplifiedData = result.Select(x => new
                {
                    Id = x.Id,
                    Name = x.ProductModel.Name,
                    ModelVersion = x.ProductModel.Version,
                    ProductSerial = x.ProductSerial,
                    StartDate = x.StartDate,
                    State = x.ProductModelState.Name,
                    PercentCompleted = x.Progress,
                    EditedBy = x.EditedBy,
                    ComponentName = string.IsNullOrEmpty(componentNameWithoutWildcards) ? "" : x.ComponentAssemblies.Where(y => y.ProductComponent.ComponentName.ToLower().Contains(componentNameWithoutWildcards)).FirstOrDefault()?.ProductComponent.ComponentName,
                    ComponentDataRevision = string.IsNullOrEmpty(componentNameWithoutWildcards) ? "" : x.ComponentAssemblies.Where(y => y.ProductComponent.ComponentName.ToLower().Contains(componentNameWithoutWildcards)).FirstOrDefault()?.Revision,
                    ComponentDataSerial = string.IsNullOrEmpty(componentNameWithoutWildcards) ? "" : x.ComponentAssemblies.Where(y => y.ProductComponent.ComponentName.ToLower().Contains(componentNameWithoutWildcards)).FirstOrDefault()?.Serial,
                    ComponentChangeDate = string.IsNullOrEmpty(componentNameWithoutWildcards) ? "" : x.ComponentAssemblies.Where(y => y.ProductComponent.ComponentName.ToLower().Contains(componentNameWithoutWildcards)).FirstOrDefault()?.AssemblyDateTime.ToString(),
                }).ToList();

                // sort data
                simplifiedData = simplifiedData.OrderByDescending(x => x.StartDate).ToList();

                return SerializeForWeb(simplifiedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        [HttpPost]
        public ActionResult GetAssemblyUsage(string serial, bool includeRevision, bool includeSerial)
        {
            _logger.LogInfo("User {0} requests a report detailing where an assembly is used.", User.Identity.Name, serial);

            try
            {
                // fix wildcards
                serial = (serial ?? "").Replace('*', '%');

                var assemblies = GTSDataRepository.GetListByQuery<ProductAssembly>("FROM ProductAssembly pa WHERE pa.ProductSerial like '" + serial + "'").ToList();
                List<ComponentAssembly> containingAssemblies = null;

                if (includeRevision && !includeSerial)
                    containingAssemblies = GTSDataRepository.GetListByQuery<ComponentAssembly>("FROM ComponentAssembly ca WHERE ca.Serial like '" + serial + "'").ToList();

                if (includeRevision && includeSerial)
                {
                    containingAssemblies = GTSDataRepository.GetListByQuery<ComponentAssembly>("FROM ComponentAssembly ca WHERE ca.Serial like '" + serial + "' OR ca.Revision like '" + serial + "'").ToList();
                    containingAssemblies.AddRange(GTSDataRepository.GetListByQuery<ComponentAssembly>("FROM ComponentAssembly ca WHERE ca.Serial like '" + serial + "' AND ca.Revision like '" + serial + "'").ToList());
                    containingAssemblies = containingAssemblies.Distinct<ComponentAssembly>().ToList();
                }

                if (!includeRevision && includeSerial)
                    containingAssemblies = GTSDataRepository.GetListByQuery<ComponentAssembly>("FROM ComponentAssembly ca WHERE ca.Revision like '" + serial + "'").ToList();

                if (!includeRevision && !includeSerial)
                    containingAssemblies = new List<ComponentAssembly>();

                var simplifiedData = new
                {
                    Assemblies =
                        assemblies.Select(x => new
                        {
                            Id = x.Id,
                            StartDate = x.StartDate,
                            LastUpdate = x.ComponentAssemblies.ToList().OrderByDescending(ca => ca.AssemblyDateTime).FirstOrDefault()?.AssemblyDateTime,
                            Name = x.ProductModel.Name,
                            ProductSerial = x.ProductSerial,
                            State = x.ProductModelState.Name,
                            Progress = x.Progress,
                            FinalState = x.Evaluation,
                            ModelVersion = x.ProductModel.Version
                        }).OrderBy(x => x.StartDate),
                    ContainingAssemblies =
                        containingAssemblies.Select(z => new
                        {
                            Id = z.ProductAssembly.Id,
                            StartDate = z.ProductAssembly.StartDate,
                            ProductName = z.ProductAssembly.ProductModel.Name,
                            ProductSerial = z.ProductAssembly.ProductSerial,
                            ComponentName = z.ProductComponent.ComponentName,
                            State = z.ProductAssembly.ProductModelState.Name,
                            Progress = z.ProductAssembly.Progress,
                            ComponentSerial = z.Serial,
                            ComponentRevision = z.Revision
                        }).OrderBy(x => x.StartDate)
                };

                return SerializeForWeb(simplifiedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        */

        [HttpPost]
        public ActionResult GetRemarksBetweenDates(DateTime? start, DateTime? end, bool onlyOpenRemarks)
        {
            _logger.LogInfo("User {0} requests report of Remarks between dates {1} and {2}.", User.Identity.Name, start != null ? start.ToString() : "Not-entered", end != null ? end.ToString() : "Not-entered");

            try
            {
                var finalEndDate = new DateTime(9999, 12, 31, 23, 59, 59, 0);

                // create a criteria set to list RemarkSymptoms
                var remarks = UnitOfWorkSession.CreateCriteria<RemarkSymptom>();

                // if requested, create criteria to only show open remarks
                if (onlyOpenRemarks)
                    remarks = remarks.Add(Expression.Eq("Resolved", false));

                // add start date to criteria, if available
                if (start != null)
                    remarks = remarks.Add(Expression.Gt("CreationDate", ((DateTime)start).Date));

                // add end date to criteria, if available and take enddate near max date into account
                if (end != null)
                    remarks = remarks.Add(Expression.Le("ResolutionDate", ((DateTime)end).Date) || Expression.Eq("ResolutionDate", finalEndDate) || Expression.IsNull("ResolutionDate"));

                var data = remarks.List<RemarkSymptom>();

                var simplifiedData = data.Select(x => new
                {
                    Id = x.Id,
                    Name = x.ProductAssembly.ProductModel.Name,
                    ProductSerial = x.ProductAssembly.ProductSerial,
                    PublicProductSerial = x.ProductAssembly.PublicProductSerial,
                    TypeOfProduct = x.ProductAssembly.ProductModel.ModelType.ToString(),
                    Configurations = CreateModelConfigurationString(x.ProductAssembly.ProductModelConfigurations),
                    CreationDate = x.CreationDate,
                    EditedBy = x.EditedBy,
                    Description = TrimLinebreaks(x.Description),
                    IsArchived = x.IsArchived ? "yes" : "no",
                    Cause = x.RemarkSymptomCauses.Count > 0 ? x.RemarkSymptomCauses[0].CauseType.Name + (x.RemarkSymptomCauses.Count > 1 ? " (+" + (x.RemarkSymptomCauses.Count - 1) + ")" : "") : "None",
                    CauseDescription = x.RemarkSymptomCauses.Count > 0 ? TrimLinebreaks(x.RemarkSymptomCauses[0].Description) : "",
                    CauseDate = x.RemarkSymptomCauses.Count > 0 ? x.RemarkSymptomCauses[0].CauseDate.ToString() : "",
                    Solution = x.RemarkSymptomCauses.Count > 0 && x.RemarkSymptomCauses[0].RemarkSymptomSolution != null ? x.RemarkSymptomCauses[0].RemarkSymptomSolution.RemarkSymptomSolutionType.Name : "None",
                    Costs = x.RemarkSymptomCauses.Count > 0 ? x.RemarkSymptomCauses[0].MaterialCost.ToString() : "0",
                    Resolved = x.Resolved,
                    ResolvedBy = x.Resolved ? x.RemarkSymptomCauses.Count > 0 && x.RemarkSymptomCauses[0].RemarkSymptomSolution != null ? x.RemarkSymptomCauses[0].RemarkSymptomSolution.EditedBy : "None" : "",
                    ResolutionDate = x.Resolved ? x.RemarkSymptomCauses.Count > 0 && x.RemarkSymptomCauses[0].RemarkSymptomSolution != null ? x.RemarkSymptomCauses[0].RemarkSymptomSolution.SolutionDate.ToString() : "None" : "",
                });

                simplifiedData = simplifiedData.OrderByDescending(x => x.CreationDate).ToList();

                return SerializeForWeb(simplifiedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the usage of a serial in another assembly.
        /// </summary>
        /// <param name="serial">The serial.</param>
        /// <returns></returns>
        public ActionResult GetSerialUsage(string serial)
        {
            _logger.LogInfo("User {0} attempts to determine if serial {1} is used in any assemblies", User.Identity.Name, serial);

            try
            {
                serial = serial.Replace('*', '%');
                var serialUsageCriteria = UnitOfWorkSession.CreateCriteria<ComponentAssembly>()
                    .Add(Expression.Like("Serial", serial))
                    .Add(Expression.Eq("IsArchived", false));

                var serialCriteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>()
                    .Add(Expression.Like("ProductSerial", serial));

                var assembliesWithThisSerial = serialCriteria.List<ProductAssembly>();

                var assembliesContainingThisSerial = serialUsageCriteria.List<ComponentAssembly>();
                var dtoResult = assembliesContainingThisSerial.Select(componentAssembly => new
                {
                    Id = componentAssembly.Id,
                    Name = componentAssembly.ProductAssembly.ProductModel.Name,
                    Serial = componentAssembly.ProductAssembly.ProductSerial,
                    PublicSerial = componentAssembly.ProductAssembly.PublicProductSerial,
                    Version = "v" + componentAssembly.ProductAssembly.ProductModel.Version,
                    State = componentAssembly.ProductAssembly.ProductModelState.Name,
                    ComponentName = componentAssembly.ProductComponent.ComponentName,
                    ComponentSerial = componentAssembly.Serial,
                    ComponentVersion = "v" + assembliesWithThisSerial.Where(assembly => assembly.ProductSerial == componentAssembly.Serial && assembly.ProductModel.BaseModelId == componentAssembly.ProductComponent.UnderlyingProductModel.BaseModelId).Single().ProductModel.Version
                });

                return SerializeForWeb(dtoResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        // GET: Report
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Initializes the list of model component names which is requested when the user makes selection in the list of model versions.
        /// </summary>
        /// <param name="versionNr">The version nr.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns></returns>
        public ActionResult InitModelComponentNamesData(int versionNr, Guid modelId)
        {
            _logger.LogInfo("User {0} attempts to request a list of component names for a particular model with id {1}", User.Identity.Name, modelId);

            try
            {
                var searchCriteria = UnitOfWorkSession.CreateCriteria<ProductModel>()
                    .Add(Expression.Eq("Id", modelId))
                    .Add(Expression.Eq("Version", versionNr));

                var modelComponents = searchCriteria.List<ProductModel>()
                    .Single()
                    .ProductComponents
                    .Select(component => new { Id = component.Id, Name = component.ComponentName, SequenceOrder = component.SequenceOrder }).ToList();

                return SerializeForWeb(modelComponents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Initializes the model version nr collection which is requested after the user makes a selection in the model select list.
        /// </summary>
        /// <param name="baseModelId">The base model identifier.</param>
        /// <returns></returns>
        public ActionResult InitModelVersionData(Guid baseModelId)
        {
            _logger.LogInfo("User {0} attempts to generate a list of versions for a model with base model id {1}", User.Identity.Name, baseModelId);

            try
            {
                var searchCriteria = UnitOfWorkSession.CreateCriteria<ProductModel>()
                    //.Add(Expression.Eq("IsArchived", false))
                    .Add(Expression.Eq("BaseModelId", baseModelId));

                var versions = searchCriteria.List<ProductModel>().Select(model => new
                {
                    Id = model.Id,
                    VersionNr = model.Version
                }).ToList();

                return SerializeForWeb(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Initializes the reporting filter data.
        /// </summary>
        /// <returns></returns>
        public ActionResult InitReportingFilterData()
        {
            _logger.LogInfo("User {0} attempts to initialize needed filter data to generate reports.", User.Identity.Name);

            try
            {
                var searchCriteria = UnitOfWorkSession.CreateCriteria<ProductModel>()
                    .Add(Expression.Eq("IsArchived", false))
                    .Add(Expression.Eq("IsReleased", true));

                var models = searchCriteria.List<ProductModel>().ToList<ProductModel>().DistinctBy(model => model.BaseModelId).Select(model => new
                {
                    Id = model.Id,
                    Name = model.Name,
                    BaseModelId = model.BaseModelId
                }).ToList();

                var result = new
                {
                    StartDate = DateTime.Now.AddDays(-7),

                    EndDate = DateTime.Now,
                    Models = models,
                };

                return SerializeForWeb(result);
                //return new HttpStatusCodeResult(200, "Operation completed succesfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Reports the on assemblies.
        /// </summary>
        /// <param name="baseModelId">The base model identifier.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="componentName">Name of the component.</param>
        /// <param name="componentNameFreeInput">The component name free input.</param>
        /// <param name="componentValue">The component value.</param>
        /// <param name="productSerial">The product serial.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns></returns>
        public ActionResult ReportOnAssemblies(Guid? baseModelId, Guid? modelId, Guid? componentId, string componentNameFreeInput, string componentValue, string productSerial, DateTime? startDate, DateTime endDate)
        {
            _logger.LogInfo("User {0} attempts to generate a report on assemblies. User filter params: baseModelId = '{1}', modelId = '{2}', componentId = '{3}',  componentNameFreeInput = '{4}', componentValue = '{5}', productSerial = '{6}', start date = '{7}', end data = '{8}'",
                User.Identity.Name,     // 0
                baseModelId,            // 1
                modelId,                // 2
                componentId,            // 3
                componentNameFreeInput, // 4
                componentValue,         // 5
                productSerial,          // 6
                startDate,              // 7
                endDate);               // 8

            try
            {
                var searchCriteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>();

                // check for product serial user input
                if (!string.IsNullOrEmpty(productSerial))
                {
                    productSerial = productSerial.Replace('*', '%');
                    searchCriteria = searchCriteria.Add(Expression.Like("ProductSerial", productSerial));
                }

                // start date
                if (startDate != null && startDate != DateTime.MinValue && startDate != DateTime.MaxValue)
                    searchCriteria = searchCriteria.Add(Expression.Gt("StartDate", startDate));
                else
                    searchCriteria = searchCriteria.Add(Expression.Gt("StartDate", _minDate));

                // end date
                if (endDate != null && endDate != DateTime.MinValue && endDate != DateTime.MaxValue)
                    searchCriteria = searchCriteria.Add(Expression.Lt("EndDate", endDate) || Expression.Eq("EndDate", _finalEndDate)); //searchCriteria = searchCriteria.Add(Expression.Lt("EndDate", endDate) || Expression.Lt("EndDate", _finalEndDate));
                else
                    searchCriteria = searchCriteria.Add(Expression.Lt("EndDate", _finalEndDate));

                // check for component user input
                if (!string.IsNullOrEmpty(componentValue))
                {
                    componentValue = componentValue.Replace('*', '%');
                    componentNameFreeInput = componentNameFreeInput?.Replace('*', '%');

                    // filter on ComponentAssemblies
                    searchCriteria = searchCriteria.CreateCriteria("ComponentAssemblies");

                    // use component value to filter, if needed
                    searchCriteria = searchCriteria.Add(Expression.Like("Revision", componentValue) || Expression.Like("Serial", componentValue));

                    // check component id vs component name entries
                    searchCriteria = searchCriteria.CreateCriteria("ProductComponent");
                    if (componentId != null && componentId != Guid.Empty)
                    {
                        searchCriteria = searchCriteria.Add(Expression.Eq("Id", componentId));
                    }
                    else if (!string.IsNullOrEmpty(componentNameFreeInput))
                    {
                        searchCriteria = searchCriteria.Add(Expression.Like("ComponentName", componentNameFreeInput));
                    }
                }

                // check model selection
                if (modelId != null && modelId != Guid.Empty)
                {
                    searchCriteria = searchCriteria.CreateCriteria("ProductModel").Add(Expression.Eq("Id", modelId));
                }
                else if (baseModelId != null && baseModelId != Guid.Empty)
                {
                    searchCriteria = searchCriteria.CreateCriteria("ProductModel").Add(Expression.Eq("BaseModelId", baseModelId));
                }

                // retrieve results
                var results = searchCriteria.List<ProductAssembly>();

                IEnumerable<dynamic> assemblies = null;

                // check if we need to include a component value (= if a component value is entered by the user)
                if (!string.IsNullOrEmpty(componentValue))
                {
                    var tmpAssemblyList = new List<dynamic>();
                    results.ToList<ProductAssembly>().ForEach(x =>
                    {
                        string componentName, ComponentDataRevision, ComponentDataSerial = null;
                        ComponentAssembly componentAssembly = null;

                        // use either the component id input parameter or the free text entry component namee
                        if (componentId != null && componentId != Guid.Empty)
                            componentAssembly = x.ComponentAssemblies.Where(componentAssy => componentAssy.ProductComponent.Id == componentId).Single();
                        else
                            componentAssembly = x.ComponentAssemblies.Where(componentAssy => componentAssy.ProductComponent.ComponentName.Contains(componentNameFreeInput.Remove(componentNameFreeInput.IndexOf('%'), 1))).Single();

                        // use the retrieved component assembly
                        componentName = componentAssembly?.ProductComponent.ComponentName;
                        ComponentDataRevision = componentAssembly?.Revision;
                        ComponentDataSerial = componentAssembly?.Serial;
                        var ComponentChangeDate = componentAssembly?.AssemblyDateTime;

                        var assy = new
                        {
                            Id = x.Id,
                            StartDate = x.StartDate,
                            LastUpdate = x.ComponentAssemblies.ToList().OrderByDescending(ca => ca.AssemblyDateTime).FirstOrDefault()?.AssemblyDateTime,
                            Name = x.ProductModel.Name,
                            ProductSerial = x.ProductSerial,
                            PublicProductSerial = x.PublicProductSerial,
                            Configurations = CreateModelConfigurationString(x.ProductModelConfigurations),
                            ModelType = x.ProductModel.ModelType.ToString(),
                            State = x.ProductModelState.Name,
                            Progress = x.Progress,
                            FinalState = x.Evaluation,
                            ModelVersion = x.ProductModel.Version,
                            ComponentName = componentName,
                            ComponentDataRevision = ComponentDataRevision,
                            ComponentDataSerial = ComponentDataSerial,
                            ComponentChangeDate = ComponentChangeDate
                        };

                        tmpAssemblyList.Add(assy);

                        assemblies = tmpAssemblyList.OrderBy(y => y.StartDate);
                    });
                }
                else
                    assemblies = results.Select(x => new
                    {
                        Id = x.Id,
                        StartDate = x.StartDate,
                        LastUpdate = x.ComponentAssemblies.ToList().OrderByDescending(ca => ca.AssemblyDateTime).FirstOrDefault()?.AssemblyDateTime,
                        Name = x.ProductModel.Name,
                        ProductSerial = x.ProductSerial,
                        PublicProductSerial = x.PublicProductSerial,
                        Configurations = CreateModelConfigurationString(x.ProductModelConfigurations),
                        ModelType = x.ProductModel.ModelType.ToString(),
                        State = x.ProductModelState.Name,
                        Progress = x.Progress,
                        FinalState = x.Evaluation,
                        ModelVersion = x.ProductModel.Version
                    }).OrderBy(x => x.StartDate);

                return SerializeForWeb(assemblies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a string by combining model configuration names.
        /// </summary>
        /// <param name="productModelConfigurations">The product model configurations.</param>
        /// <returns></returns>
        private object CreateModelConfigurationString(IList<ProductModelConfiguration> productModelConfigurations)
        {
            var sb = new StringBuilder();

            productModelConfigurations.ToList().ForEach(config =>
            {
                sb.Append(config.Name);
                sb.Append(", ");
            });

            return (sb.Length > 2 && productModelConfigurations.Count > 1) ? sb.Remove(sb.Length - 2, 2).ToString() : sb.ToString();
        }

        /// <summary>
        /// Removes all types of line breaks
        /// </summary>
        /// <param name="inputString">The input string.</param>
        /// <returns></returns>
        private string TrimLinebreaks(string inputString)
        {
            try
            {
                return Regex.Replace(inputString ?? "", @"\r\n?|\n", "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }
    }
}
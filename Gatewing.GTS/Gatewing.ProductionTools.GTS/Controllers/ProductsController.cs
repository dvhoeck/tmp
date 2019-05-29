using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.DAL;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

/// <summary>
/// Controller for products and assemblies
/// </summary>
namespace Gatewing.ProductionTools.GTS.Controllers
{
    [Authorize]
    [OutputCache(VaryByParam = "*", Duration = 0, NoStore = true)] // disable caching for all actions
    public class ProductsController : BaseController
    {
        private const string STR_MODEL_NAME = "Model name and version";
        private const string STR_SERIAL = "Product serial";

        /// <summary>
        /// Indexes this product list view
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        #region Scan input

        /// <summary>
        /// Adds a text note to an assembly.
        /// </summary>
        /// <param name="assemblyId">The assembly identifier.</param>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public ActionResult AddNote(Guid assemblyId, string text)
        {
            _logger.LogInfo("User {0} is trying to add a note '{1}' to assembly with Id '{2}'", User.Identity.Name, text, assemblyId);
            try
            {
                var note = new Note
                {
                    Id = Guid.NewGuid(),
                    Text = text,
                    ProductAssembly = GTSDataRepository.GetById<ProductAssembly>(assemblyId),
                    EditedBy = User.Identity.Name,
                    CreationDate = DateTime.Now
                };

                GTSDataRepository.Create<Note>(note);

                return SerializeForWeb(GetNotesForAssembly(assemblyId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Returns the assembly view
        /// </summary>
        /// <returns></returns>
        public ActionResult Assembly(Guid id)
        {
            return View();
        }

        /// <summary>
        /// Creates the product assembly by product model.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="serial">The serial.</param>
        /// <returns></returns>
        public ActionResult CreateProductAssemblyByProductModel(Guid modelId, string serial)
        {
            try
            {
                var model = GTSDataRepository.GetById<ProductModel>(modelId);

                if (Regex.Match(serial, model.IdMask).Length != serial.Length)
                    throw new InvalidOperationException("Serial does not match id mask length");

                var assembly = CreateProductAssemblyByProductModel(model, serial);

                return SerializeForWeb(new { Id = assembly.Id, Serial = assembly.ProductSerial });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Sets a product assembly's state to "Delivered".
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NullReferenceException">
        /// Could not find assembly with id: " + id
        /// or
        /// Could not find state with name: 'Delivered'
        /// </exception>
        public ActionResult Deliver(Guid id)
        {
            _logger.LogInfo("User {0} has requested to set assembly with ID {1}'s state to 'Delivered'", User.Identity.Name, id);

            try
            {
                var prodCrit = UnitOfWorkSession.CreateCriteria<ProductAssembly>();
                prodCrit = prodCrit.Add(Expression.Eq("Id", id));
                var productAssembly = prodCrit.List<ProductAssembly>()[0];

                if (productAssembly == null)
                    throw new NullReferenceException("Could not find assembly with id: " + id);

                var stateCrit = UnitOfWorkSession.CreateCriteria<ProductModelState>();
                stateCrit = stateCrit.Add(Expression.Eq("Name", "Delivered"));
                var state = stateCrit.List<ProductModelState>()[0];

                if (state == null)
                    throw new NullReferenceException("Could not find state with name: 'Delivered'");

                productAssembly.ProductModelState = state;

                GTSDataRepository.Update<ProductAssembly>(productAssembly);

                _logger.LogInfo("Updated product assembly with id: {0} ({1}), set its state to 'delivered'", productAssembly.Id, productAssembly.ProductModel.Name);

                return new HttpStatusCodeResult(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Deports an assembly to a different state
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="toNextState">if set to <c>true</c> [to next state].</param>
        /// <param name="lastComponentAssembly">The last component assembly.</param>
        /// <param name="batchModeItems">The batch mode items.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">You must fill in all fields before deporting to the next state.</exception>
        public ActionResult DeportToState(Guid id, bool toNextState, string batchModeItems)
        {
            // get main assembly
            var assembly = GTSDataRepository.GetById<ProductAssembly>(id);

            // create a list to hold the main assembly and any batch mode items
            var assemblyList = new List<ProductAssembly> { assembly };

            try
            {
                // deserialize the json string
                var dynamicList = JsonConvert.DeserializeObject<dynamic>(batchModeItems);

                // loop over all items
                foreach (var batchModeItem in dynamicList)
                {
                    // get the serial
                    var serial = batchModeItem.serial.ToString();

                    // use the serials and the base model Id from the main assembly to retrieve the correct assemblies
                    assemblyList.Add(GTSDataRepository.GetByProductSerial<ProductAssembly>(serial, assembly.ProductModel.BaseModelId));
                }

                // deport all assemblies to next or previous state
                assemblyList.ForEach(assy =>
                {
                    DoDeportToState(assy, toNextState);
                });

                return new HttpStatusCodeResult(200, assemblyList.Count.ToString() + " assemblies deported to " + (toNextState ? "next" : "previous") + " state");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public ActionResult DoDeportToState(ProductAssembly assembly, bool toNextState)
        {
            _logger.LogInfo("User {0} attempts to deport assembly {1} ({2}) to the {3} state.", User.Identity.Name, assembly.ProductSerial, assembly.ProductModel.Name, toNextState ? "next" : "previous");

            var deliveredState = GTSDataRepository.GetListByQuery<ProductModelState>("FROM ProductModelState pms WHERE pms.Name = 'Delivered'").FirstOrDefault();
            var assemblyState = GTSDataRepository.GetListByQuery<ProductModelState>("FROM ProductModelState pms WHERE pms.Name = 'Assembly'").FirstOrDefault();

            if (toNextState)
            {
                assembly.ComponentAssemblies = assembly.ComponentAssemblies.OrderBy(x => x.ProductComponent.SequenceOrder).ToList();
                List<System.Tuple<ProductModelState, List<ComponentAssembly>>> grouped = GroupAssemblyPrevious(assembly);
                if (assembly.CurrentStateIndex >= grouped.Count)
                    assembly.CurrentStateIndex = grouped.Count - 1;
                var assembliesToCheck = grouped[assembly.CurrentStateIndex].Item2;

                var allFieldsCompleted = true;
                assembliesToCheck.ForEach(x =>
                {
                    var isInConfig = false;
                    x.ProductComponent.ProductModelConfigurations.ForEach(currentConfig =>
                    {
                        if (assembly.ProductModelConfigurations.Where(config => config.Name == currentConfig.Name).Count() > 0)
                            isInConfig = true;
                    });

                    if (!x.IsArchived && isInConfig)
                    {
                        switch (x.ProductComponent.ComponentRequirement)
                        {
                            case ComponentRequirement.Both:
                                if (string.IsNullOrEmpty(x.Revision) || string.IsNullOrEmpty(x.Serial))
                                    allFieldsCompleted = false;
                                break;

                            case ComponentRequirement.Revision:
                                if (string.IsNullOrEmpty(x.Revision))
                                    allFieldsCompleted = false;
                                break;

                            case ComponentRequirement.Serial:
                                if (string.IsNullOrEmpty(x.Serial))
                                    allFieldsCompleted = false;
                                break;

                            default:
                                break;
                        }

                        // do an additional check for QC models
                        if (x.ProductComponent.UnderlyingProductModel?.ModelType != null && x.ProductComponent.UnderlyingProductModel.ModelType == ModelType.QualityCheck)
                        {
                            var qcAssembly = UnitOfWorkSession.CreateCriteria<ProductAssembly>().Add(Expression.Eq("ProductSerial", x.Serial)).UniqueResult<ProductAssembly>();
                            if (qcAssembly.Progress < 100)
                                throw new InvalidOperationException(string.Format("The last QC check for component {0} wasn't finished. Start a new one.", x.ProductComponent.ComponentName));
                        }
                    }
                });

                if (!allFieldsCompleted)
                    throw new InvalidOperationException("You must fill in all fields before deporting to the next state.");
            }

            var states = FilterOutDoubleSequentialStates(assembly.ProductModel.ProductComponents);

            var maxStateIndex = states.Count();

            if (toNextState)
            {
                if (assembly.CurrentStateIndex + 1 == maxStateIndex)
                {
                    //return new HttpStatusCodeResult(200, "This assembly is already in its last state.");
                    assembly.CurrentStateIndex = maxStateIndex;
                    assembly.ProductModelState = deliveredState;
                    assembly.EndDate = DateTime.Now;

                    _logger.LogInfo("Assembly with ID '{0}' and serial '{1}' was deported to it's final state and marked as 'Delivered'", assembly.Id, assembly.ProductSerial);
                }
                if (assembly.CurrentStateIndex + 1 < maxStateIndex)
                {
                    assembly.ProductModelState = states[assembly.CurrentStateIndex + 1];
                    assembly.CurrentStateIndex = assembly.CurrentStateIndex + 1;
                }
            }
            else
            {
                if (assembly.CurrentStateIndex == 0)
                {
                    if (assembly.ProductModelState != deliveredState)
                        return new HttpStatusCodeResult(200, "This assembly is already in its first state.");
                    else
                    {
                        assembly.ProductModelState = assemblyState;
                        assembly.CurrentStateIndex = 0;
                    }
                }

                if (assembly.CurrentStateIndex - 1 >= 0)
                {
                    assembly.ProductModelState = states[assembly.CurrentStateIndex - 1];
                    assembly.CurrentStateIndex = assembly.CurrentStateIndex - 1;
                }
            }

            _logger.LogInfo("User {0} has deported assembly {1} ({2}) to state {3}.", User.Identity.Name, assembly.ProductSerial, assembly.ProductModel.Name, assembly.ProductModelState);

            GTSDataRepository.Update(assembly);

            return new HttpStatusCodeResult(200, "Assembly deported to " + assembly.ProductModelState.Name);
        }

        /// <summary>
        /// Gets the models for serial format json.
        /// </summary>
        /// <param name="serial">The serial.</param>
        /// <returns></returns>
        public ActionResult GetModelsForSerialFormatJson(string serial, int serialType)
        {
            _logger.LogInfo("User " + User.Identity.Name + " is trying to find models for serial format: " + serial);

            try
            {
                var serialOut = string.Empty;

                var hits = TryToMatchSerialToModels(serial, out serialOut, serialType);

                return SerializeForWeb(hits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Returns the product assembly as a json string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult GetProductAssemblyJson(Guid id)
        {
            _logger.LogInfo("User {0} has requested assembly with ID {1}", User.Identity.Name, id);

            var productAssembly = GTSDataRepository.GetById<ProductAssembly>(id);

            // do check for existing configurations
            DoProductAssemblyConfigCheck(productAssembly);

            productAssembly.ComponentAssemblies = productAssembly.ComponentAssemblies.OrderBy(x => x.ProductComponent.SequenceOrder).ToList();

            // create a criteria set to list RemarkSymptom
            var remarkCriteria = UnitOfWorkSession.CreateCriteria<RemarkSymptom>();

            // add Resolved = false criteria
            remarkCriteria.Add(Expression.Eq("Resolved", false));

            // add IsArchived = false criteria
            remarkCriteria.Add(Expression.Eq("IsArchived", false));

            remarkCriteria = remarkCriteria.CreateCriteria("ProductAssembly");
            remarkCriteria = remarkCriteria.Add(Expression.Eq("Id", id));

            var remarks = remarkCriteria.List<RemarkSymptom>();

            productAssembly.ProductModel.ProductComponents = productAssembly.ProductModel.ProductComponents.OrderBy(x => x.SequenceOrder).ToList();

            var c = 0;
            var states = new List<object>();
            FilterOutDoubleSequentialStates(productAssembly.ProductModel.ProductComponents).ForEach(state =>
            {
                states.Add(new { Index = c, state.Name });
                c++;
            });

            List<System.Tuple<ProductModelState, List<object>>> grouped = GroupAssembly(productAssembly);

            var optimizedTooling = new List<object>();
            productAssembly.ProductModel.Tooling.ForEach(x => optimizedTooling.Add(new { Id = x.Id, Name = x.Name, ToolCode = x.ToolCode, Setting = x.Setting, Description = x.Description }));

            var archivedPublicProductSerialCriteria = UnitOfWorkSession.CreateCriteria<ArchivedPublicProductSerial>()
                .CreateCriteria("ProductAssembly")
                .Add(Expression.Eq("Id", productAssembly.Id));

            var archivedPublicProductSerials = archivedPublicProductSerialCriteria.List<ArchivedPublicProductSerial>();

            var optimizedResult = new
            {
                // assembly
                Assembly = new
                {
                    Id = productAssembly.Id,
                    BaseModelId = productAssembly.ProductModel.BaseModelId,
                    ProductModelName = productAssembly.ProductModel.Name,
                    ProductModelVersion = productAssembly.ProductModel.Version,
                    ProductSerial = productAssembly.ProductSerial,
                    PublicProductSerial = productAssembly.PublicProductSerial,
                    ProductModelType = productAssembly.ProductModel.ModelType,
                    Progress = productAssembly.Progress,
                    StartedBy = productAssembly.StartedBy,
                    StartDate = productAssembly.StartDate,
                    EditedBy = productAssembly.EditedBy,
                    NHVersion = productAssembly.NHVersion,
                    Tooling = optimizedTooling,
                    ProductModelState = productAssembly.ProductModelState,
                    CurrentStateIndex = productAssembly.CurrentStateIndex,
                    IdMask = productAssembly.ProductModel.IdMask,
                    PublicIdMask = productAssembly.ProductModel.PublicIdMask,
                    EvaluationState = productAssembly.Evaluation,
                    AvailableProductModelConfigurations = productAssembly.ProductModel.ProductModelConfigurations,
                    SelectedProductModelConfigurations = productAssembly.ProductModelConfigurations
                },

                ArchivedPublicProductSerials = archivedPublicProductSerials.Select(serial => new
                {
                    PublicProductSerial = serial.PublicProductSerial,
                    ArchivedBy = serial.ArchivedBy,
                    ArchivalDate = serial.ArchivalDate
                }),

                Remarks = remarks.Select(x => new { Name = x.RemarkSymptomType.Name, Description = x.Description }),

                Grouped = grouped.Select(e => new
                {
                    state = e.Item1,
                    components = e.Item2
                }),

                Notes = GetNotesForAssembly(productAssembly.Id),

                States = states
            };

            var serialized = SerializeForWeb(optimizedResult);

            return serialized;
        }

        /// <summary>
        /// Parses  scan input and takes action. Typically this means either retrieve product assembly or create new one if not found.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>An url for the product page with an ID appended</returns>
        public ActionResult ParseScanInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            // handle potential caps lock
            input = input.ToUpper();

            var serial = string.Empty;

            var hits = TryToMatchSerialToModels(input, out serial);

            if (hits.Count == 0)
                return new HttpStatusCodeResult(500, "Could not find a model for input: " + input);

            return SerializeForWeb(new { count = hits.Count, serial = input, hits = hits });
        }

        /// <summary>
        /// Rolls assembly state back to a particular state.
        /// </summary>
        /// <param name="assemblyId">The assembly identifier.</param>
        /// <param name="stateId">The state identifier.</param>
        /// <returns></returns>
        public ActionResult RollBackToState(Guid assemblyId, int stateIndex)
        {
            _logger.LogInfo("User {0} attempts to roll back an assembly with Id {1} to a state with index {2}", User.Identity.Name, assemblyId, stateIndex);
            try
            {
                // get assembly
                var assy = GTSDataRepository.GetById<ProductAssembly>(assemblyId);

                // get all states from component collection
                var c = 0;
                var states = new Dictionary<int, string>();
                FilterOutDoubleSequentialStates(assy.ProductModel.ProductComponents).ForEach(state =>
                {
                    states.Add(c, state.Name);
                    c++;
                });

                // get all states from a certain state index on
                var filteredModelStates = states.Where(keyValuePair => keyValuePair.Key <= stateIndex);

                var setNewState = false;
                var tmpStateList = new Dictionary<int, string>();
                c = 0;
                assy.ComponentAssemblies
                    .OrderBy(ca => ca.ProductComponent.SequenceOrder)
                    .ForEach(ca =>
                    {
                        if (tmpStateList.Count == 0 || tmpStateList.Last().Value != ca.ProductComponent.ProductModelState.Name)
                        {
                            tmpStateList.Add(c, ca.ProductComponent.ProductModelState.Name);
                            c++;
                        }

                        if (stateIndex <= tmpStateList.Keys.Last())
                        {
                            ca.Serial = ca.Revision = string.Empty;
                            if (!setNewState)
                            {
                                assy.ProductModelState = ca.ProductComponent.ProductModelState;
                                assy.CurrentStateIndex = stateIndex;
                                setNewState = true;
                            }
                        }
                    }
                );

                GTSDataRepository.UpdateAll<ComponentAssembly>(assy.ComponentAssemblies);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Sets the public serial.
        /// </summary>
        /// <param name="assyId">The assy identifier.</param>
        /// <param name="publicSerial">The public serial.</param>
        /// <returns></returns>
        public ActionResult SetPublicSerial(Guid assyId, string publicSerial)
        {
            _logger.LogInfo("User {0} attempts to set the public serial ({2}) for a product with Id {1}", User.Identity.Name, assyId, publicSerial);

            if (string.IsNullOrEmpty(publicSerial))
                throw new ArgumentNullException("publicSerial", "publicSerial input parameter was null or empty.");

            try
            {
                var assy = GTSDataRepository.GetById<ProductAssembly>(assyId);

                if (assy.PublicProductSerial != publicSerial && Regex.IsMatch(publicSerial.ToUpper(), assy.ProductModel.PublicIdMask)) // check if the current model's id mask matches a part of the scan input
                {
                    // check if this serial already occurs for the same modeltype
                    var publicSerialOccurenceCriteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>()
                        .Add(Expression.Not(Expression.Eq("Id", assy.Id)))
                        .Add(Expression.Eq("PublicProductSerial", publicSerial))
                        .CreateCriteria("ProductModel")
                        .Add(Expression.Eq("BaseModelId", assy.ProductModel.BaseModelId));

                    var resultList = publicSerialOccurenceCriteria.List<ProductAssembly>();

                    if (resultList.Count > 0)
                        throw new InvalidOperationException(string.Format("Public serial {0} is already used in a product with the same base model id and internal serial {1} / Id {2}", publicSerial, resultList[0].ProductSerial, resultList[0].Id));

                    // archive the previous public serial as a ArchivedPublicProductSerial entity
                    if (!string.IsNullOrEmpty(assy.PublicProductSerial))
                    {
                        var newArchivedPublicProductSerial = new ArchivedPublicProductSerial
                        {
                            Id = Guid.NewGuid(),
                            ArchivalDate = DateTime.Now,
                            ArchivedBy = User.Identity.Name,
                            IsArchived = true,
                            EditedBy = User.Identity.Name,
                            InternalProductSerial = assy.ProductSerial,
                            ProductAssembly = assy,
                            PublicProductSerial = assy.PublicProductSerial
                        };

                        GTSDataRepository.Create<ArchivedPublicProductSerial>(newArchivedPublicProductSerial);
                    }

                    // not used yet, assign public serial and save Assembly
                    assy.PublicProductSerial = publicSerial;
                    GTSDataRepository.Update<ProductAssembly>(assy);

                    return new HttpStatusCodeResult(200, "Public serial succesfully set");
                }

                return new HttpStatusCodeResult(200, "Public serial for this assembly was already set to " + publicSerial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Updates component assemblies.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        public ActionResult UpdateComponentAssemblies(string objectAsString)
        {
            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(objectAsString);
            var assemblySerials = dynamicObject.assemblySerials;
            var componentAssembly = dynamicObject.componentAssembly;
            var productComponentId = dynamicObject.componentAssembly.ProductComponentId;

            var serialList = new List<string>();
            foreach (var item in assemblySerials)
            {
                Console.WriteLine(item);
                serialList.Add(item.serial.ToString());
            }

            // create criteria to
            var batchModeCriteria = UnitOfWorkSession.CreateCriteria<ComponentAssembly>();
            batchModeCriteria = batchModeCriteria.CreateAlias("ProductAssembly", "pa");
            batchModeCriteria = batchModeCriteria.Add(Expression.In("pa.ProductSerial", serialList));
            batchModeCriteria = batchModeCriteria.CreateAlias("ProductComponent", "pc", JoinType.InnerJoin);
            batchModeCriteria = batchModeCriteria.Add(Restrictions.Eq("pc.Id", new Guid(productComponentId.ToString())));

            var result = batchModeCriteria.List<ComponentAssembly>();
            result.ForEach(ca =>
            {
                var revision = componentAssembly.Revision?.ToString();
                var serial = componentAssembly.Serial?.ToString();

                UpdateComponentAssembly(ca, revision, serial);
            });

            return null;
        }

        /// <summary>
        /// Updates the assembly component.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        public ActionResult UpdateComponentAssembly(string objectAsString)
        {
            var componentAssemblyFromUI = JsonConvert.DeserializeObject<ComponentAssembly>(objectAsString);
            var existingComponentAssembly = GTSDataRepository.GetById<ComponentAssembly>(componentAssemblyFromUI.Id);

            // check for stale data
            if (componentAssemblyFromUI.NHVersion != existingComponentAssembly.NHVersion && existingComponentAssembly.Id == componentAssemblyFromUI.Id)
                throw new StaleObjectStateException("Assembly Component", existingComponentAssembly.Id);

            return UpdateComponentAssembly(existingComponentAssembly, componentAssemblyFromUI.Revision, componentAssemblyFromUI.Serial);
        }

        /// <summary>
        /// Creates the product assembly by product model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="productSerial">The product serial.</param>
        /// <returns></returns>
        private ProductAssembly CreateProductAssemblyByProductModel(ProductModel model, string productSerial)
        {
            _logger.LogInfo("User {0} requests to retrieve or create an instance (assembly) of product model {2} with serial {1}.", User.Identity.Name, productSerial, model.Name);

            // try to retrieve an existing assembly
            var existingAssembly = GTSDataRepository.GetListByQuery<ProductAssembly>("FROM ProductAssembly pa WHERE pa.ProductSerial = :serial AND pa.ProductModel.BaseModelId = :modelId",
                new Dictionary<string, object> { { "serial", productSerial }, { "modelId", model.BaseModelId } }).FirstOrDefault();
            if (existingAssembly != null)
                return existingAssembly;

            var firstState = GTSDataRepository.GetListByQuery<ProductModelState>("FROM  ProductModelState pms WHERE pms.Name = 'Delivered'").FirstOrDefault();
            if (model.ProductComponents != null && model.ProductComponents.Count > 0)
                firstState = model.ProductComponents.OrderBy(x => x.SequenceOrder).FirstOrDefault().ProductModelState;

            // no existing assembly found, create new one
            var assembly = new ProductAssembly
            {
                ProductModel = model,
                StartDate = DateTime.Now,
                StartedBy = User.Identity.Name,
                EditedBy = User.Identity.Name,
                EndDate = DateTime.MaxValue.AddDays(-2),
                ProductSerial = productSerial,
                ProductModelState = firstState,
                Id = Guid.NewGuid(),
                ComponentAssemblies = new List<ComponentAssembly>(),
            };

            GTSDataRepository.Create<ProductAssembly>(assembly);

            model.ProductComponents.ForEach(x =>
            {
                var componentAssembly = new ComponentAssembly { Id = Guid.NewGuid(), ProductComponent = x, ProductAssembly = assembly, AssemblyDateTime = DateTime.Now };
                assembly.ComponentAssemblies.Add(componentAssembly);
                GTSDataRepository.Create(componentAssembly);
            }
            );

            GTSDataRepository.Update<ProductAssembly>(assembly);

            return assembly;
        }

        /// <summary>
        /// Do a check to see if default configurations are assigned.
        /// </summary>
        /// <param name="productAssembly">The product assembly.</param>
        private void DoProductAssemblyConfigCheck(ProductAssembly productAssembly)
        {
            var doCreateConfig = false;

            // try to retrieve default config from either the model or the assembly, create it if that's impossible (but don't set ID yet)
            ProductModelConfiguration defaultConfig =
                productAssembly.ProductModel.ProductModelConfigurations.Where(config => config.Name == "default").SingleOrDefault() ??
                productAssembly.ProductModelConfigurations.Where(config => config.Name == "default").SingleOrDefault() ?? new ProductModelConfiguration { Name = "default", Color = "#9d9d9d", ConfigIndex = 0 };

            // check if model contains default config
            if (productAssembly.ProductModel.ProductModelConfigurations.Where(config => config.Name == "default").SingleOrDefault() == null && productAssembly.ProductModel.ProductModelConfigurations.Count() == 0)
            {
                doCreateConfig = true;
                productAssembly.ProductModel.ProductModelConfigurations.Add(defaultConfig);
                GTSDataRepository.Update<ProductModel>(productAssembly.ProductModel);
            }

            // check product model components and assembly components
            productAssembly.ComponentAssemblies.ForEach(componentAssembly =>
            {
                if (componentAssembly.ProductComponent.ProductModelConfigurations.Where(currentConfig => currentConfig.Name == "default").Count() <= 0 && componentAssembly.ProductComponent.ProductModelConfigurations.Count() == 0)
                {
                    doCreateConfig = true;
                    componentAssembly.ProductComponent.ProductModelConfigurations.Add(defaultConfig);
                    GTSDataRepository.Update<ProductComponent>(componentAssembly.ProductComponent);
                }
            });

            // check if product assembly contains default config
            if (productAssembly.ProductModelConfigurations.Where(config => config.Name == "default").SingleOrDefault() == null && productAssembly.ProductModelConfigurations.Count() == 0)
            {
                doCreateConfig = true;
                productAssembly.ProductModelConfigurations.Add(defaultConfig);
                GTSDataRepository.Update<ProductAssembly>(productAssembly);
            }

            // if needed save a new default config to the database
            if (doCreateConfig)
            {
                if (defaultConfig.Id == Guid.Empty)
                {
                    defaultConfig.Id = Guid.NewGuid();
                    GTSDataRepository.Create<ProductModelConfiguration>(defaultConfig);
                }
                else
                {
                    GTSDataRepository.Update<ProductModelConfiguration>(defaultConfig);
                }
            }
        }

        /// <summary>
        /// Filters the out double sequential states.
        /// </summary>
        /// <param name="components">A list of components.</param>
        /// <returns></returns>
        private List<ProductModelState> FilterOutDoubleSequentialStates(IList<ProductComponent> components)
        {
            List<ProductModelState> states = new List<ProductModelState>();
            foreach (var component in components)
            {
                var last = states.LastOrDefault();
                if (last == null || !last.Equals(component.ProductModelState))
                {
                    states.Add(component.ProductModelState);
                }
            }

            return states;
        }

        /// <summary>
        /// Gets the notes of an assembly.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private object GetNotesForAssembly(Guid id)
        {
            return GTSDataRepository.GetListByQuery<Note>("FROM Note nt WHERE nt.ProductAssembly.Id = :id", new Dictionary<string, object> { { "id", id } })
                .OrderBy(x => x.CreationDate)
                .Select(x => new { Id = x.Id, Text = x.Text, EditedBy = x.EditedBy, CreationDate = x.CreationDate.ToString("u") }).ToList();
        }

        private List<System.Tuple<ProductModelState, List<object>>> GroupAssembly(ProductAssembly productAssembly)
        {
            List<System.Tuple<ProductModelState, List<object>>> grouped = new List<System.Tuple<ProductModelState, List<object>>>();

            var componentAssemblies = productAssembly.ComponentAssemblies.Where(ca => ca.IsArchived == false).ToList();

            foreach (var componentEntity in componentAssemblies)
            {
                var WorkInstructions = new List<object>();

                componentEntity.ProductComponent.WorkInstructions.ForEach(x => WorkInstructions.Add(
                    new
                    {
                        Id = x.Id,
                        Title = x.Title,
                        Description = x.Description,
                        SequenceOrder = x.SequenceOrder,
                        RelativeImagePath = x.RelativeImagePath
                    }
                    ));

                var component = new
                {
                    Id = componentEntity.Id,
                    ProductComponentId = componentEntity.ProductComponent.Id,
                    NHVersion = componentEntity.NHVersion,
                    Revision = componentEntity.Revision,
                    Serial = componentEntity.Serial,
                    AssemblyDateTime = componentEntity.AssemblyDateTime,
                    ProductModelType = componentEntity.ProductComponent.UnderlyingProductModel == null ? 0 : componentEntity.ProductComponent.UnderlyingProductModel.ModelType,
                    ProductModelBaseId = componentEntity.ProductComponent.UnderlyingProductModel == null ? Guid.Empty : componentEntity.ProductComponent.UnderlyingProductModel.BaseModelId,
                    SequenceOrder = componentEntity.ProductComponent.SequenceOrder,
                    ComponentRequirement = componentEntity.ProductComponent.ComponentRequirement,
                    ComponentName = componentEntity.ProductComponent.ComponentName,
                    RevisionInputMask = componentEntity.ProductComponent.RevisionInputMask,
                    SerialInputMask = componentEntity.ProductComponent.SerialInputMask,
                    ProductModelState = componentEntity.ProductComponent.ProductModelState,
                    WorkInstructions = WorkInstructions,
                    UnderlyingProductAssemblyId = GTSDataRepository.GetByProductSerial<ProductAssembly>(componentEntity.Serial, componentEntity.ProductComponent.UnderlyingProductModel == null ? Guid.Empty : componentEntity.ProductComponent.UnderlyingProductModel.BaseModelId)?.Id,
                    ProductModelConfigurations = componentEntity.ProductComponent.ProductModelConfigurations
                };

                var last = grouped.LastOrDefault();
                if (last == null || !last.Item1.Equals(component.ProductModelState))
                {
                    grouped.Add(new System.Tuple<ProductModelState, List<object>>(component.ProductModelState, new List<object> { component }));
                }
                else
                {
                    last.Item2.Add(component);
                }
            }

            return grouped;
        }

        private List<System.Tuple<ProductModelState, List<ComponentAssembly>>> GroupAssemblyPrevious(ProductAssembly productAssembly)
        {
            List<System.Tuple<ProductModelState, List<ComponentAssembly>>> grouped = new List<System.Tuple<ProductModelState, List<ComponentAssembly>>>();
            foreach (var component in productAssembly.ComponentAssemblies)
            {
                var last = grouped.LastOrDefault();
                if (last == null || !last.Item1.Equals(component.ProductComponent.ProductModelState))
                {
                    grouped.Add(new System.Tuple<ProductModelState, List<ComponentAssembly>>(component.ProductComponent.ProductModelState, new List<ComponentAssembly> { component }));
                }
                else
                {
                    last.Item2.Add(component);
                }
            }
            return grouped;
        }

        /// <summary>
        /// Tries to match a serial to models' serial format.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="serial">The serial.</param>
        /// <returns></returns>
        private List<dynamic> TryToMatchSerialToModels(string input, out string serial, int serialType = 0)
        {
            var serialTmp = string.Empty;

            // try ot get the part of the input that matches one of the released products' id mask
            var matchedModels = new List<ProductModel>();

            var modelCriteria = UnitOfWorkSession.CreateCriteria<ProductModel>();

            modelCriteria.Add(Expression.Eq("IsReleased", true)).Add(Expression.Eq("IsArchived", false));

            modelCriteria.List<ProductModel>().ForEach(x => // iterate of all models
            {
                if (Regex.IsMatch(input, x.IdMask.ToUpper())) // check if the current model's id mask matches a part of the scan input
                {
                    serialTmp = Regex.Match(input, x.IdMask.ToUpper()).Value; // we have a match, retrieve it from the input
                    matchedModels.Add(x); // store the model in a temp var, we need the name later
                                          // modelList.Add(x);
                }
            });

            serial = serialTmp != string.Empty ? serialTmp : input;

            var hits = new List<dynamic>();

            matchedModels.ForEach(modelAndMatch =>
            {
                var result = new
                {
                    Id = modelAndMatch.Id,
                    ModelName = modelAndMatch.Name,
                };

                hits.Add(result);
            });

            return hits;
        }

        private ActionResult UpdateComponentAssembly(ComponentAssembly componentAssembly, string revision, string serial)
        {
            // don't update assembly if no changes were made (otherwise simply viewing assembly would cause user to be marked as EditedBy)
            if (revision == componentAssembly.Revision && serial == componentAssembly.Serial)
                return SerializeForWeb(new { Progress = componentAssembly.ProductAssembly.Progress, NHVersion = componentAssembly.NHVersion });

            // if componentAssembly has an underlying product model and a value for its Serial property
            if (componentAssembly.ProductComponent.UnderlyingProductModel != null && !string.IsNullOrEmpty(serial))
            {
                var searchCriteria = UnitOfWorkSession.CreateCriteria<ComponentAssembly>();

                searchCriteria = searchCriteria.Add(Restrictions.Not(Restrictions.Eq("Id", componentAssembly.Id)));
                searchCriteria.Add(Expression.Eq("Serial", serial));
                searchCriteria = searchCriteria.CreateCriteria("ProductComponent")
                    .Add(Expression.Like("UnderlyingProductModel.Id", componentAssembly.ProductComponent.UnderlyingProductModel.BaseModelId));
                searchCriteria.Add(Expression.Eq("IsArchived", false));

                var results = searchCriteria.List();

                // throw error if the product we're trying to reference is in use
                if (results.Count > 0)
                    throw new InvalidOperationException(string.Format("Serial {0} is already used in another component with the same underlying product model.", serial));

                // try to retrieve a product assembly with the value of the current ComponentAssembly's "Serial" property
                ProductAssembly retrievedAssembly = null;
                searchCriteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>()
                    .Add(Expression.Eq("ProductSerial", serial))
                    .CreateCriteria("ProductModel")
                        .Add(Expression.Eq("BaseModelId", componentAssembly.ProductComponent.UnderlyingProductModel.BaseModelId));

                var retrievedAssemblies = searchCriteria.List();

                // we should only have one or zero hits here
                if (retrievedAssemblies != null && retrievedAssemblies.Count == 1)
                    retrievedAssembly = (ProductAssembly)retrievedAssemblies[0];

                // create a new product if its an AutoCreate product model which doesn't exist yet
                if (componentAssembly.ProductComponent.UnderlyingProductModel.ModelType == ModelType.AutoCreated && retrievedAssembly == null && !string.IsNullOrEmpty(serial))
                {
                    var datetimeNow = DateTime.Now;
                    var autoCreateAssembly = new ProductAssembly();
                    autoCreateAssembly.Id = Guid.NewGuid();
                    autoCreateAssembly.StartDate = datetimeNow;
                    autoCreateAssembly.StartedBy = ((ProductAssembly)autoCreateAssembly).EditedBy = User.Identity.Name;
                    autoCreateAssembly.EndDate = datetimeNow.AddMinutes(1);
                    autoCreateAssembly.ProductModel = componentAssembly.ProductComponent.UnderlyingProductModel;
                    autoCreateAssembly.ProductModelState = GTSDataRepository.GetListByQuery<ProductModelState>("FROM ProductModelState Where Name = 'Delivered'", null).FirstOrDefault();
                    autoCreateAssembly.ProductSerial = serial;
                    //autoCreateAssembly.NHVersion = 1;

                    GTSDataRepository.Create<ProductAssembly>(autoCreateAssembly);
                }
                else // if it's not an AutoCreate product then we need to check if the product exists, is in the correct state and is not in remark
                {
                    // does it exist?
                    if (retrievedAssembly == null) // nope, it does not exist
                        throw new InvalidOperationException("Could not find assembly (" + componentAssembly.ProductComponent.UnderlyingProductModel.Name + ") with serial (" + serial + ")."); // halt operations if it doesn't
                    else // it does exist
                    {
                        // check for remarks
                        var remarks = GetOpenRemarksForAssembly(retrievedAssembly.Id);
                        if (remarks.Count() > 0)
                            throw new InvalidOperationException("A component refers to an assembly (" + componentAssembly.ProductComponent.UnderlyingProductModel.Name + ") in remark and cannot be used.");

                        // check product model state
                        if (retrievedAssembly.ProductModelState.Name != "Delivered")
                            throw new InvalidOperationException("You are trying to enter a serial of a product that isn't finished yet (not yet 'delivered').");
                    }
                }
            }

            // compose a value to be able to log the full component update
            var value = "";
            switch (componentAssembly.ProductComponent.ComponentRequirement)
            {
                case ComponentRequirement.Revision:
                    value = string.IsNullOrEmpty(revision) ? "'empty' value (revision)" : "'" + revision + "' (revision)";
                    break;

                case ComponentRequirement.Serial:
                    value = string.IsNullOrEmpty(serial) ? "'empty' value (serial)" : "'" + serial + "' (serial)";
                    break;

                case ComponentRequirement.Both:
                    value = string.Format("'{0}' (revision) / '{1}' (serial)", string.IsNullOrEmpty(revision) ? "empty value" : revision, string.IsNullOrEmpty(serial) ? "empty value" : serial);
                    break;
            }

            _logger.LogInfo("User {0} is attempting to update component assembly with name '{1}' and id '{2}' for Assembly with serial '{3}' (public serial '{4}'), value to update field with: {5}", User.Identity.Name, componentAssembly.ProductComponent.ComponentName, componentAssembly.Id, componentAssembly.ProductAssembly.ProductSerial, string.IsNullOrEmpty(componentAssembly.ProductAssembly.PublicProductSerial) ? "none" : componentAssembly.ProductAssembly.PublicProductSerial, value);

            componentAssembly.EditedBy = componentAssembly.ProductAssembly.EditedBy = User.Identity.Name;
            componentAssembly.AssemblyDateTime = DateTime.Now;
            componentAssembly.Revision = revision;
            componentAssembly.Serial = serial;

            try
            {
                GTSDataRepository.Update<ComponentAssembly>(componentAssembly);
                GTSDataRepository.Update<ProductAssembly>(componentAssembly.ProductAssembly);
            }
            catch (InvalidOperationException ivOex)
            {
                componentAssembly.Serial = string.Empty;
                componentAssembly.Revision = string.Empty;
                _logger.LogError(ivOex);
                throw;
            }

            return SerializeForWeb(new { Progress = componentAssembly.ProductAssembly.Progress, NHVersion = componentAssembly.NHVersion + 1 });
        }

        /*
        /// <summary>
        /// Gets an ordered collection of model states from an assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        private List<ProductModelState> GetOrderedModelStatesFromAssembly(ProductAssembly assembly)
        {
            var states = new List<ProductModelState>();
            ProductModelState previousState = null;
            assembly.ProductModel.ProductComponents
                .OrderByDescending(component => component.SequenceOrder)
                .ForEach(component =>
                {
                    if (component.ProductModelState != previousState)
                    {
                        states.Add(component.ProductModelState);
                        previousState = component.ProductModelState;
                    }
                });

            return states;
        }*/

        #endregion Scan input

        #region Tools

        /// <summary>
        /// Gets the tools as a json list.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetToolsJson()
        {
            _logger.LogInfo("User {0} attempts to display the master tool list.", User.Identity.Name);

            var tools = GTSDataRepository.GetListByQuery<AssemblyTool>("FROM AssemblyTool at WHERE at.IsArchived = false").OrderBy(x => x.Name);

            var toolsIdArray = tools.Select(x => x.Id).ToArray();

            var associatedModelsList = GetModelsAssociatedWithTools(toolsIdArray);

            var simplifiedList = tools.Select(tool => new
            {
                Id = tool.Id,
                Name = tool.Name,
                Description = tool.Description,
                ToolCode = tool.ToolCode,
                Setting = tool.Setting,
                NHVersion = tool.NHVersion,
                SimpleModelList = associatedModelsList.Where(model => model.Tooling.Where(modelTool => modelTool.Id == tool.Id).Count() > 0).Select(model => new { Id = model.Id, Name = model.Name, Version = model.Version }).OrderBy(y => y.Version).ToList()
            });

            return SerializeForWeb(simplifiedList);
        }

        /// <summary>
        /// Releases the associated models.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="releaseComment">The release comment.</param>
        /// <returns></returns>
        public ActionResult ReleaseAssociatedModels(Guid id, string releaseComment)
        {
            _logger.LogInfo("User {0} attempts to release a number of associated models.", User.Identity.Name);
            try
            {
                // throw exception if a release comment wasn't included
                if (string.IsNullOrEmpty(releaseComment))
                    throw new ArgumentNullException("You cannot release a model without entering a release comment");

                // retrieve model by use of its Id
                var model = GTSDataRepository.GetById<ProductModel>(id);

                // get other models that refer to the model in their components and add current model to that collection
                var associatedModels = GetAssociatedProductModels(model.BaseModelId);
                associatedModels.Add(model);

                // release the current model and all related models
                associatedModels.ForEach(x => ReleaseProduct(x, releaseComment));

                return new HttpStatusCodeResult(200, "Products released.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Returns the page to list tools
        /// </summary>
        /// <returns></returns>
        public ActionResult Tools()
        {
            return View();
        }

        private List<ProductModel> GetModelsAssociatedWithTools(Guid[] tool)
        {
            var associatedModelsCriteria = UnitOfWorkSession.CreateCriteria<ProductModel>();
            associatedModelsCriteria.Add(Expression.Eq("IsArchived", false)).Add(Expression.Eq("IsReleased", false));
            associatedModelsCriteria = associatedModelsCriteria.CreateCriteria("Tooling");
            associatedModelsCriteria.Add(Expression.In("Id", tool));

            return associatedModelsCriteria.List<ProductModel>().ToList();
        }

        /*
        private List<ProductModel> GetDistinctLatestVersionToolList(AssemblyTool tool)
        {
            var result = new List<ProductModel>();

            var associatedModels = GTSDataRepository.GetListByQuery<ProductModel>("FROM ProductModel pm WHERE pm.IsArchived = false AND pm.IsReleased = false").Where(model => model.Tooling.Contains(tool)).ToList();

            var uniqueBaseModelIds = associatedModels.Select(x => x.BaseModelId).Distinct();

            uniqueBaseModelIds.ForEach(x =>
            {
                result.Add(associatedModels.Where(model => model.BaseModelId == x).OrderBy(model => model.Version).LastOrDefault());
            });

            return result;
        }
        */
        /*

        /// <summary>
        /// Release a model and all its related models at once
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <param name="releaseComment">The release comment.</param>
        /// <returns></returns>
        private void ReleaseAssociatedModels(IEnumerable<ProductModel> associatedModels, string releaseComment)
        {
            try
            {
                var newReleases = new List<ProductModel>();

                associatedModels.ForEach(x =>
                {
                    ReleaseProduct(x.Id.ToString(), releaseComment, x.Version);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }
        */

        #endregion Tools

        #region Product Models

        /// <summary>
        /// Returns a view to add or edit a product.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult AddEditProductModel()
        {
            return View();
        }

        public ActionResult AddOrRemoveProductModelConfigurationFromAssembly(Guid assemblyId, Guid configId, bool doAdd)
        {
            _logger.LogInfo("User {0} attempts to {3} a product model configuration with Id {1} to an assembly with Id {2}.", User.Identity.Name, configId, assemblyId, doAdd ? "add" : "remove");
            try
            {
                // get entities
                var assembly = GTSDataRepository.GetById<ProductAssembly>(assemblyId);
                var config = GTSDataRepository.GetById<ProductModelConfiguration>(configId);

                // add or remove config
                if (doAdd)
                {
                    if (!assembly.ProductModelConfigurations.Contains(config))
                        assembly.ProductModelConfigurations.Add(config);
                }
                else
                {
                    if (assembly.ProductModelConfigurations.Where(currentConfig => currentConfig.Name == config.Name).Count() > 0)
                        assembly.ProductModelConfigurations.Remove(assembly.ProductModelConfigurations.Where(currentConfig => currentConfig.Name == config.Name).Single());
                }

                // update entities
                GTSDataRepository.Update<ProductAssembly>(assembly);
                GTSDataRepository.Update<ProductModelConfiguration>(config);

                return new HttpStatusCodeResult(200, "Config " + (doAdd ? "added" : "removed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public ActionResult AddOrRemoveProductModelConfigurationFromProductComponent(Guid componentId, Guid configId, bool doAdd)
        {
            _logger.LogInfo("User {0} attempts to {3} a product model configuration with Id {1} to a product component with Id {2}.", User.Identity.Name, configId, componentId, doAdd ? "add" : "remove");
            try
            {
                // get entities
                var component = GTSDataRepository.GetById<ProductComponent>(componentId);
                var config = GTSDataRepository.GetById<ProductModelConfiguration>(configId);

                // add or remove config
                if (doAdd)
                {
                    if (component.ProductModelConfigurations.Where(currentConfig => currentConfig.Name.ToLower() == config.Name.ToLower()).Count() == 0)
                        component.ProductModelConfigurations.Add(config);
                }
                else
                {
                    if (component.ProductModelConfigurations.Where(currentConfig => currentConfig.Name.ToLower() == config.Name.ToLower()).Count() > 0)
                        component.ProductModelConfigurations.Remove(component.ProductModelConfigurations.Where(currentConfig => currentConfig.Name == config.Name).Single());
                }

                // update entities
                GTSDataRepository.Update<ProductComponent>(component);
                GTSDataRepository.Update<ProductModelConfiguration>(config);

                return new HttpStatusCodeResult(200, "Config " + (doAdd ? "added" : "removed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Adds a product model configuration to a product model.
        /// </summary>
        /// <param name="productModelId">The product model identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">You cannot create a configuration called 'default'</exception>
        public ActionResult AddProductModelConfiguration(Guid productModelId, string name, string color, int index)
        {
            _logger.LogInfo("User {0} attempts to add a product model configuration with the name {1} to a product with Id {2}.", User.Identity.Name, name, productModelId);

            try
            {
                // get model to assign new config to
                var model = GTSDataRepository.GetById<ProductModel>(productModelId);

                // check for actual values passed
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException("No value for 'Name' passed.");

                // check for "default" name
                if (name.ToLower() == "default")
                    throw new InvalidOperationException("You cannot create a configuration called 'default'");

                // create a new Product Model Config
                var newProductModelConfiguration = new ProductModelConfiguration { Id = Guid.NewGuid(), Name = name, Color = color, ConfigIndex = index };

                // persist it
                GTSDataRepository.Create<ProductModelConfiguration>(newProductModelConfiguration);

                // a new config to model's collection of configs
                model.ProductModelConfigurations.Add(newProductModelConfiguration);

                // update model
                GTSDataRepository.Update<ProductModel>(model);

                return new HttpStatusCodeResult(200, "Config created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Returns a page to display existing assemblies
        /// </summary>
        /// <returns></returns>
        public ActionResult Assemblies()
        {
            return View();
        }

        /// <summary>
        /// Changes the name of the product model configuration.
        /// </summary>
        /// <param name="productModelConfigurationId">The product model configuration identifier.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">You cannot change a configuration's name to 'default'</exception>
        public ActionResult ChangeProductModelConfigurationName(Guid productModelConfigurationId, string name)
        {
            _logger.LogInfo("User {0} attempts to change the name of a product model configuration with Id {1}. New name: {2}.", User.Identity.Name, productModelConfigurationId, name);
            try
            {
                // check for actual values passed
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException("No value for 'Name' passed.");

                // check for "default" name
                if (name.ToLower() == "default")
                    throw new InvalidOperationException("You cannot change a configuration's name to 'default'");

                // get config to delete
                var productModelConfiguration = GTSDataRepository.GetById<ProductModelConfiguration>(productModelConfigurationId);

                // change name
                productModelConfiguration.Name = name;

                // update model
                GTSDataRepository.Update<ProductModelConfiguration>(productModelConfiguration);

                return new HttpStatusCodeResult(200, "Config's name updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Clones a model.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="idMask">The identifier mask.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Could not retrieve product model for id " + modelId</exception>
        [HttpGet]
        public ActionResult CloneModelJSON(Guid modelId, string name, string idMask, string description)
        {
            _logger.LogInfo("User {0} attempts to clone product model with id {1}, new model will be called: {2}.", User.Identity.Name, modelId, name);
            try
            {
                var existingModel = GTSDataRepository.GetById<ProductModel>(modelId);
                if (existingModel == null)
                    throw new InvalidOperationException("Could not retrieve product model for id " + modelId);

                // clone to new model
                var newModel = existingModel.Clone(false, name, idMask, description);

                // create entity and its collections
                GTSDataRepository.Create<ProductModel>(newModel);
                GTSDataRepository.CreateAll<ProductComponent>(newModel.ProductComponents);
                foreach (var component in newModel.ProductComponents)
                    GTSDataRepository.CreateAll<GTSWorkInstruction>(component.WorkInstructions);

                _logger.LogInfo("Model, including components and work instructions, is cloned => name: {0}, id: {1}, idmask: {2}, description: {3}.", newModel.Name, newModel.Id, newModel.IdMask, newModel.Comment);

                return new HttpStatusCodeResult(200, "Model successfully cloned.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a qc assembly.
        /// </summary>
        /// <param name="componentAssemblyId">The component assembly identifier.</param>
        /// <param name="qCModelIdq">The q c model id.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">The system could not retrieve a QC model for base model id " + qCModelIdq
        /// or
        /// The system could not create an assembly for a QC model with base model id " + qCModelIdq</exception>
        public ActionResult CreateQCAssembly(Guid componentAssemblyId, Guid qCModelIdq)
        {
            _logger.LogInfo("User {0} attempts to create a QC for component assembly with Id {1} for QC model with Id {2}", User.Identity.Name, componentAssemblyId, qCModelIdq);

            try
            {
                // check if id is usable
                if (qCModelIdq == null || qCModelIdq == Guid.Empty)
                    throw new InvalidOperationException("QC model ID passed was invalid: " + qCModelIdq);

                // Get the corresponding product model
                var qcAssyModelCrits = UnitOfWorkSession.CreateCriteria<ProductModel>();
                qcAssyModelCrits = qcAssyModelCrits.Add(Expression.Eq("IsArchived", false));
                qcAssyModelCrits = qcAssyModelCrits.Add(Expression.Eq("IsReleased", true));
                qcAssyModelCrits = qcAssyModelCrits.Add(Expression.Eq("BaseModelId", qCModelIdq));
                var qcProductModel = qcAssyModelCrits.UniqueResult<ProductModel>();

                // released nmodel is required
                if (qcProductModel == null && qCModelIdq != null && qCModelIdq != Guid.Empty)
                    throw new InvalidOperationException("The system could not retrieve a released QC model for base model id " + qCModelIdq);

                // retrieve the calling component assembly
                var callingComponentAssembly = GTSDataRepository.GetById<ComponentAssembly>(componentAssemblyId);

                // put the containing product assembly into a variable
                var containingProductAssembly = callingComponentAssembly.ProductAssembly;

                // create a new QC assembly to perform a Quality Check
                var newQCAssy = CreateProductAssemblyByProductModel(qcProductModel, Guid.NewGuid().ToString());
                if (newQCAssy == null)
                    throw new InvalidOperationException("The system could not create an assembly for a QC model with base model id " + qCModelIdq);

                // a QC assembly should always have fields for the containing product assembly's serial and model name
                var serialField = newQCAssy.ComponentAssemblies.Where(ca => ca.ProductComponent.ComponentName == STR_SERIAL).FirstOrDefault();
                if (serialField == null)
                    throw new InvalidOperationException("Could not find a product serial component in the QC model the system was trying to load.");
                var modelNameAndVersionField = newQCAssy.ComponentAssemblies.Where(ca => ca.ProductComponent.ComponentName == STR_MODEL_NAME).FirstOrDefault();
                if (modelNameAndVersionField == null)
                    throw new InvalidOperationException("Could not find a product model name and version component in the QC model the system was trying to load.");

                // assign values to serialField and modelNameAndVersionField
                serialField.Revision = containingProductAssembly.ProductSerial;
                modelNameAndVersionField.Revision = containingProductAssembly.ProductModel.PrettyName;

                // check if the current component assembly already has a QC assigned
                if (string.IsNullOrEmpty(callingComponentAssembly.Serial))
                {
                    // assign one if not so
                    callingComponentAssembly.Serial = newQCAssy.ProductSerial;
                    callingComponentAssembly.EditedBy = User.Identity.Name;
                    GTSDataRepository.Update<ComponentAssembly>(callingComponentAssembly);
                }
                else
                {
                    // create a new component assembly to replace the existing component assembly, this way we can retrieve archived components at a later time
                    // while instantiating, assign the serial of the Assembly we created in the previous statement
                    var newComponentAssembly = new ComponentAssembly
                    {
                        Id = Guid.NewGuid(),
                        ProductComponent = callingComponentAssembly.ProductComponent,
                        ProductAssembly = callingComponentAssembly.ProductAssembly,
                        AssemblyDateTime = DateTime.Now,
                        EditedBy = User.Identity.Name,
                        Serial = newQCAssy.ProductSerial
                    };

                    containingProductAssembly.ComponentAssemblies.Add(newComponentAssembly);

                    // persist the new ComponentAssembly
                    GTSDataRepository.Create<ComponentAssembly>(newComponentAssembly);
                    GTSDataRepository.Update<ProductAssembly>(containingProductAssembly);

                    // archive the previous component assembly
                    GTSDataRepository.ArchiveById<ComponentAssembly>(callingComponentAssembly.Id, User.Identity.Name);
                }

                // return the ID of the new QC assembly to the UI
                return SerializeForWeb(newQCAssy.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the current model release.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult CurrentModelRelease(string id)
        {
            return View();
        }

        /// <summary>
        /// Gets the current model release as json string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult CurrentModelReleaseJson(string id)
        {
            return SerializeForWeb(GetProductJsonByIdAsString(id));
        }

        /// <summary>
        /// Deletes the assembly.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult DeleteAssembly(Guid id)
        {
            _logger.LogInfo("User {0} is attempting to delete an assembly with id {1}", User.Identity.Name, id);
            try
            {
                var product = GTSDataRepository.GetById<ProductAssembly>(id);
                var productSerial = product.ProductSerial;
                _logger.LogInfo("Assembly with serial {0} will be deleted from the system.", productSerial);

                // start to delete remarks
                product.RemarkSymptoms.ForEach(remark =>
                {
                    // by first deleting solution and cause
                    remark.RemarkSymptomCauses.ForEach(cause =>
                    {
                        Guid solutionId = Guid.Empty;

                        // get solution id
                        if (cause.RemarkSymptomSolution != null)
                            solutionId = cause.RemarkSymptomSolution.Id;

                        // delete all cause images
                        cause.RemarkImages.ForEach(image => GTSDataRepository.DeleteById<RemarkImage>(image.Id));

                        // delete cause
                        GTSDataRepository.DeleteById<RemarkSymptomCause>(cause.Id);

                        if (solutionId != Guid.Empty)
                        {
                            // delete all solution images
                            cause.RemarkSymptomSolution.RemarkImages.ForEach(image => GTSDataRepository.DeleteById<RemarkImage>(image.Id));

                            // delete solution
                            GTSDataRepository.DeleteById<RemarkSymptomSolution>(solutionId);
                        }
                    });

                    // delete all remark images
                    remark.RemarkImages.ForEach(image => GTSDataRepository.DeleteById<RemarkImage>(image.Id));

                    // delete remark
                    GTSDataRepository.DeleteById<RemarkSymptom>(remark.Id);
                });

                // delete components delete selected product model configurations
                product.ComponentAssemblies.ForEach(component => GTSDataRepository.DeleteById<ComponentAssembly>(component.Id));

                // delete selected product model configurations
                product.ProductModelConfigurations = null;
                GTSDataRepository.Update<ProductAssembly>(product);
                GTSDataRepository.ExecuteUpdateQuery("DELETE FROM ProductModelConfigurationsToProductAssembly WHERE ProductAssembly_id = '" + product.Id.ToString() + "'",
                    null, true);

                // delete associated notes
                GTSDataRepository.ExecuteUpdateQuery("DELETE FROM Note nt WHERE nt.ProductAssembly.Id = :id", new Dictionary<string, object> { { "id", product.Id } });

                // delete associated QC's: retrieve component assemblies with
                // current assy's product serial as revision and QC as model type
                var qcComponentAssembliesForProductSerial = UnitOfWorkSession.CreateCriteria<ComponentAssembly>()
                    .Add(Expression.Eq("Revision", productSerial))
                    .CreateCriteria("ProductAssembly")
                    .CreateCriteria("ProductModel")
                    .Add(Expression.Eq("ModelType", ModelType.QualityCheck))
                    .List<ComponentAssembly>();

                // recursively delete all assemblies
                qcComponentAssembliesForProductSerial
                    .Select(ca => ca.ProductAssembly.Id)
                    .ToList<Guid>().Distinct()
                    .ForEach(currentId => DeleteAssembly(currentId));

                GTSDataRepository.DeleteById<ProductAssembly>(id);

                return new HttpStatusCodeResult(200, "Assembly with serial " + productSerial + " deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes a product model.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public HttpStatusCodeResult DeleteProductModel(Guid id)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts to delete a product model.");

            var model = GTSDataRepository.GetById<ProductModel>(id);
            if (model == null)
                throw new InvalidOperationException("Error finding model that corresponds to id: " + id);

            // archive current versions and all previous too
            GTSDataRepository.ArchiveByBaseModelId<ProductModel>(model.BaseModelId, User.Identity.Name);

            _logger.LogInfo("User {0} has deleted (archived) model with ID '{1}'.", User.Identity.Name, id);

            return new HttpStatusCodeResult(200, "Product deleted.");
        }

        /// <summary>
        /// Deletes the product model configuration.
        /// </summary>
        /// <param name="productModelId">The product model identifier.</param>
        /// <param name="productModelConfigurationId">The product model configuration identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">You cannot delete the default configuration</exception>
        public ActionResult DeleteProductModelConfiguration(Guid productModelId, Guid productModelConfigurationId)
        {
            _logger.LogInfo("User {0} attempts to delete a product model configuration with the Id {1} for model with Id {2}.", User.Identity.Name, productModelId, productModelConfigurationId);

            try
            {
                // get model to remove config from
                var model = GTSDataRepository.GetById<ProductModel>(productModelId);

                // get config to delete
                var productModelConfiguration = GTSDataRepository.GetById<ProductModelConfiguration>(productModelConfigurationId);

                // don't allow "default" to be deleted
                if (productModelConfiguration.Name.ToLower() == "default")
                    throw new InvalidOperationException("You cannot delete the default configuration");

                // remove config from model
                model.ProductModelConfigurations.Remove(productModelConfiguration);

                // remove config from components
                model.ProductComponents.ForEach(component =>
                {
                    component.ProductModelConfigurations.Remove(productModelConfiguration);
                });

                // update model & components
                GTSDataRepository.Update<ProductModel>(model);
                GTSDataRepository.UpdateAll<ProductComponent>(model.ProductComponents);

                // archive config from
                GTSDataRepository.ArchiveById<ProductModelConfiguration>(productModelConfigurationId, User.Identity.Name);

                return new HttpStatusCodeResult(200, "Config removed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the assemblies as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetAssembliesJson(string start, string end, string dataType, int modelType)
        {
            _logger.LogInfo("User {0} is requesting to see all Assemblies", User.Identity.Name);

            try
            {
                //var maxSqlDateString =
                var startDate = !string.IsNullOrEmpty(start) ? DateTime.Parse(start) : new DateTime(1900, 1, 1);
                var endDate = !string.IsNullOrEmpty(end) ? DateTime.Parse(end).AddDays(1) : DateTime.MaxValue;
                var maxDateString = DateTime.MaxValue.ToString("yyyy-MM-29 HH:mm:ss.000");

                // get all assemblies that have been started on or after the startdate parameter...
                var assemblyList = new List<ProductAssembly>();

                var criteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>();
                criteria = criteria.Add(Expression.Ge("StartDate", startDate));
                if (modelType != 0)
                {
                    var type = ((ModelType)modelType);
                    criteria = criteria.CreateCriteria("ProductModel");
                    criteria = criteria.Add(Expression.Eq("ModelType", type));
                }
                var hits = criteria.List<ProductAssembly>();

                hits.ForEach(assembly =>
                {
                    // ... and that have component assemblies with dates before or on the enddate parameter
                    if (assembly.ComponentAssemblies.Count > 0 && assembly.ComponentAssemblies.OrderBy(componentAssembly => componentAssembly.AssemblyDateTime).LastOrDefault().AssemblyDateTime <= endDate)
                    {
                        assemblyList.Add(assembly);
                    }

                    // ... or assemblies without components (no end date to check)
                    if (assembly.ComponentAssemblies.Count == 0)
                    {
                        assemblyList.Add(assembly);
                    }
                });

                // simplify the entities
                var trimmedAssemblyList = assemblyList.OrderByDescending(assembly => assembly.StartDate)
                    .Select(assembly => new
                    {
                        Id = assembly.Id,
                        Name = assembly.ProductModel.Name,
                        ProductSerial = assembly.ProductSerial,
                        PublicProductSerial = assembly.PublicProductSerial,
                        StartDate = assembly.StartDate,
                        EndDate = assembly.EndDate.Year != 9999 ? assembly.EndDate : (
                        assembly.ComponentAssemblies.Count == 0 ? DateTime.MaxValue : assembly.ComponentAssemblies.OrderBy(component => component.AssemblyDateTime).LastOrDefault().AssemblyDateTime),
                        Progress = assembly.ComponentAssemblies.Count == 0 ? 100 : assembly.Progress,
                        StartedBy = assembly.StartedBy,
                        EditedBy = assembly.EditedBy,
                        ProductModelConfigurations = assembly.ProductModelConfigurations,
                        HasOpenRemark = assembly.RemarkSymptoms.Where(x => !x.Resolved && x.IsArchived == false).Count() > 0
                    })
                    .ToList();

                // further filter the list according to the radiobutton selection on form
                if (dataType != "all")
                {
                    if (dataType == "unfinished")
                        trimmedAssemblyList = trimmedAssemblyList.Where(assembly => assembly.Progress < 100).ToList();

                    if (dataType == "finished")
                        trimmedAssemblyList = trimmedAssemblyList.Where(assembly => assembly.Progress == 100).ToList();

                    if (dataType == "remark")
                        trimmedAssemblyList = trimmedAssemblyList.Where(assembly => assembly.HasOpenRemark).ToList();
                }

                return SerializeForWeb(trimmedAssemblyList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the assembly that references an assembly with a particular id.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult GetAssemblyThatReferencesThisJson(Guid id)
        {
            _logger.LogInfo("User is checking if assembly with id: " + id + " is referenced in component assemblies");

            try
            {
                // get assembly
                var product = GTSDataRepository.GetById<ProductAssembly>(id);
                var productSerial = product.ProductSerial;

                // get component assemblies that have this product's serial
                var productThatReferencesThisAssembly = GTSDataRepository.GetListByQuery<ComponentAssembly>("FROM ComponentAssembly ca WHERE ca.Serial = :serial", new Dictionary<string, object> { { "serial", productSerial } })
                    .Select(componentAssembly => new { Id = componentAssembly.ProductAssembly.Id, ProductSerial = componentAssembly.ProductAssembly.ProductSerial, ModelName = componentAssembly.ProductAssembly.ProductModel.Name })
                    .FirstOrDefault();

                return SerializeForWeb(productThatReferencesThisAssembly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the model changes and returns them as a json string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult GetModelChangesJson(Guid id)
        {
            try
            {
                var changes = new List<EntityChange>();
                var model = GTSDataRepository.GetById<ProductModel>(id);

                // get model changes
                changes.AddRange(GTSDataRepository.GetListByQuery<EntityChange>("FROM EntityChange ec WHERE ec.ChangedEntityId = :id", new Dictionary<string, object> { { "id", id } }));

                model.ProductComponents.ForEach(component => changes.AddRange(
                        GTSDataRepository.GetListByQuery<EntityChange>("FROM EntityChange ec WHERE ec.ChangedEntityId = :id", new Dictionary<string, object> { { "id", component.Id } })
                    ));

                return SerializeForWeb(changes.OrderByDescending(change => change.ChangeDate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a product as a json string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetProductJson(string id)
        {
            _logger.LogInfo("User {0} attempts to retrieve a single product model.", User.Identity.Name);

            return SerializeForWeb(GetProductJsonByIdAsString(id));
        }

        /// <summary>
        /// Return model states as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetProductModelStatesForProductsJson()
        {
            _logger.LogInfo("User {0} requests a list of product model states.", User.Identity.Name);
            var states = Get<ProductModelState>().Where(e => !e.Name.Equals("Scrapped") && !e.Name.Equals("Delivered")).Select(e => new { Id = e.Id, Name = e.Name }).ToArray();

            return Json(states, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets the products as string list.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetProductsAsStringList()
        {
            _logger.LogInfo("User {0} attemptsto retrieve a list of product models and return them as a json string.", User.Identity.Name);

            return Json(GTSDataRepository.GetAll<ProductModel>().Select(x => new { id = x.Id, name = x.Name }).ToList(), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets the products as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetProductsJson()
        {
            _logger.LogInfo("User {0} attempts to retrieve product models and return them as a json string.", User.Identity.Name);

            var models = GTSDataRepository.GetListByQuery<ProductModel>("FROM ProductModel pm WHERE pm.IsArchived = false AND pm.IsReleased = false ")
                .Select(model => new { Id = model.Id, Name = model.Name, IdMask = model.IdMask, Comment = model.Comment, Version = model.Version, BaseModelId = model.BaseModelId }).OrderBy(x => x.Name).ToList();

            // serialize without triggering circular dependencies
            //var list = JsonConvert.SerializeObject(model, Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore });

            return SerializeForWeb(models);
        }

        /// <summary>
        /// Retrieves QCs by their base mdoel Id
        /// </summary>
        /// <param name="productComponentId">The product component identifier.</param>
        /// <param name="productAssemblyId">The product assembly identifier.</param>
        /// <returns></returns>
        public ActionResult GetQCsForBaseModelId(Guid productComponentId, Guid productAssemblyId)
        {
            _logger.LogInfo("User {0} attempts to get a list of QC's for assembly with Id {1}'s component assemblies based on product component with Id {2}", User.Identity.Name, productAssemblyId, productComponentId);
            try
            {
                // retrieve the common product component
                var productModelComponent = GTSDataRepository.GetById<ProductComponent>(productComponentId);

                // retrieve archived component assemblies for a product assembly based on the common component
                var crits = UnitOfWorkSession.CreateCriteria<ComponentAssembly>();
                crits = crits.Add(Expression.Eq("ProductAssembly.Id", productAssemblyId));
                crits = crits.Add(Expression.Eq("ProductComponent", productModelComponent));
                var componentAssembliesWithCommonProductModelComponent = crits.List<ComponentAssembly>();

                // get all serials from the component assemblies Serial field, these serials are the ID's of the QC product assemblies and create an array of them
                var qcSerialForTheseAssemblyComponents = componentAssembliesWithCommonProductModelComponent.Select(x => x.Serial).ToList();
                var assemblyIds = qcSerialForTheseAssemblyComponents.ToArray<string>();

                // create criteria to retrieve the ProductAssemblies themselves
                var productAssemblyCrits = UnitOfWorkSession.CreateCriteria<ProductAssembly>();
                productAssemblyCrits = productAssemblyCrits.Add(Expression.In("ProductSerial", assemblyIds));

                /*

                //crits = crits.Add(Expression.Eq("IsArchived", true));
                crits = crits.Add(Expression.Eq("ProductAssembly.Id", productAssemblyId));
                //crits = crits.Add(Expression.Eq("ProductComponent.ProductModel.ModelType", ModelType.QualityCheck));
                var result = crits.List<ComponentAssembly>();

                // create a list of product assembly id's from the component assemblies' Serial values
                var productAssemblyQueryIds = result.ToList().Select(ca => ca.Serial);

                //  create criteria to retrieve the actual assemblies
                var productAssemblyCrits = UnitOfWorkSession.CreateCriteria<ProductAssembly>();
                productAssemblyCrits = productAssemblyCrits.Add(Expression.In("ProductSerial", productAssemblyQueryIds.ToArray<string>()));
                */

                // execute retrieval and order list by StartDate DESC
                var productAssemblies = productAssemblyCrits.List<ProductAssembly>().OrderByDescending(pa => pa.StartDate)
                    .Select(x => new
                    {
                        Progress = x.Progress,
                        StartDate = x.StartDate,
                        EditedBy = x.EditedBy,
                        ComponentAssemblies = x.ComponentAssemblies.Select(y => new
                        {
                            ComponentName = y.ProductComponent.ComponentName,
                            AssemblyDateTime = y.AssemblyDateTime,
                            Revision = y.Revision,
                            SequenceOrder = y.ProductComponent.SequenceOrder
                        })
                    })
                    .ToList();

                return SerializeForWeb(productAssemblies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of releases.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult GetReleaseListJson(Guid id)
        {
            _logger.LogInfo("User {0} attempts to retrieve a list of releases of a model with id: " + id, User.Identity.Name);

            try
            {
                var baseModelId = GTSDataRepository.GetById<ProductModel>(id).BaseModelId;

                var releaseList = GTSDataRepository
                   .GetListByQuery<ProductModel>("From ProductModel pm WHERE pm.IsReleased = true AND pm.BaseModelId = :baseModelId",
                   new Dictionary<string, object>() { { "baseModelId", baseModelId } })
                   .OrderByDescending(x => x.Version)
                   .Select(x => new { Id = x.Id, Version = x.Version, ReleaseComment = x.ReleaseComment, ReleaseDate = x.ReleaseDate, ProductModelConfigurations = x.ProductModelConfigurations })
                   .ToList();

                return SerializeForWeb(releaseList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public ActionResult Instructions()
        {
            _logger.LogInfo("User {0} attempts to view the work instructions display page", User.Identity.Name);

            return View();
        }

        /// <summary>
        /// Returns Work Instructions by model and assembly ids.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="assemblyId">The assembly identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Could not find corresponding model</exception>
        public ActionResult InstructionsByModelAndAssemblyIds(Guid modelId, Guid assemblyId)
        {
            _logger.LogInfo("User attempts to view the work instructions for a model with id {0} and  / or assembly with id {1}", User.Identity.Name, modelId, assemblyId);
            try
            {
                ProductModel model = null;

                if (modelId == Guid.Empty)
                    model = GTSDataRepository.GetById<ProductAssembly>(assemblyId).ProductModel;
                else if (assemblyId == Guid.Empty)
                {
                    // get latest release according to base model
                    var searchCrits = UnitOfWorkSession.CreateCriteria<ProductModel>()
                        .Add(Expression.Eq("BaseModelId", modelId))
                        .Add(Expression.Eq("IsArchived", false))
                        .Add(Expression.Eq("IsReleased", true));

                    model = searchCrits.UniqueResult<ProductModel>();
                }

                if (model == null)
                    throw new InvalidOperationException("Could not find corresponding model");

                var dto = new
                {
                    Name = model.Name,
                    Type = model.ModelType.ToString(),
                    Version = model.Version,
                    ReleaseDate = model.ReleaseDate,
                    ProductModelConfigurations = model.ProductModelConfigurations,
                    Components = model.ProductComponents.Select(pc => new
                    {
                        Name = pc.ComponentName,
                        ProductModelConfigurations = pc.ProductModelConfigurations,
                        Instructions = pc.WorkInstructions.Select(wi => new
                        {
                            Title = wi.Title,
                            Description = wi.Description,
                            Image = wi.RelativeImagePath,
                            SequenceOrder = wi.SequenceOrder
                        })
                    })
                };

                return SerializeForWeb(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Saves the product passed as a json string.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveProductJson(string objectAsString)
        {
            _logger.LogInfo("User {0} attempts to save a product model to the DB.", User.Identity.Name);

            try
            {
                // model as submitted by form
                var productModel = new JavaScriptSerializer().Deserialize<ProductModel>(objectAsString);

                var toolList = productModel.Tooling;

                // retrieve existing model (can be null if newly created)
                var existingModel = GTSDataRepository.GetById<ProductModel>(productModel.Id);
                if (existingModel == null)
                {
                    existingModel = productModel;
                }
                else
                {
                    // check for stale data
                    if (productModel.NHVersion != existingModel.NHVersion)
                        throw new StaleObjectStateException("Product Model", productModel.Id);

                    // a released model cannot be saved by this method, the user only encounters this when trying to update stale data
                    if (existingModel.IsReleased)
                        throw new InvalidOperationException("This model is released, no updates can be done on it. Are you trying to update stale data?");

                    // track changes (other side of if statement denotes a new entity, so no changes to track)
                    TrackChanges<ProductModel>(existingModel, productModel);
                }

                // create a valid ID and set baseModelId (at this time we'ree creating the model)
                if (existingModel.Id == Guid.Empty)
                {
                    existingModel.Id = Guid.NewGuid();
                    existingModel.BaseModelId = productModel.Id;
                }

                // for existing model without baseModelId we have these two lines
                if (existingModel.BaseModelId == Guid.Empty)
                    existingModel.BaseModelId = existingModel.Id;

                // update existing model
                existingModel.Name = productModel.Name;
                existingModel.Version = productModel.Version;
                existingModel.Date = productModel.Date;
                existingModel.IdMask = productModel.IdMask?.Trim();
                existingModel.PublicIdMask = productModel.PublicIdMask?.Trim();
                existingModel.Comment = productModel.Comment;
                existingModel.ModelType = productModel.ModelType;

                // use a fixed id mask for Quality Checks (GUID format)
                if (existingModel.ModelType == ModelType.QualityCheck)
                    existingModel.IdMask = GuidRegEx;

                existingModel.EditedBy = User.Identity.Name;

                foreach (var tool in toolList)
                {
                    if (existingModel.Tooling.Where(x => x.Id == tool.Id).Count() == 0)
                    {
                        existingModel.Tooling.Add(tool);
                        //tool.EditedBy = User.Identity.Name;
                        TrackAddition(tool, existingModel, "Tooling");
                    }
                }

                if (existingModel.NHVersion == 0)
                {
                    GTSDataRepository.Create<ProductModel>(existingModel);

                    existingModel.PartNumbers.ForEach(partNr => { partNr.ProductModel = existingModel; });

                    GTSDataRepository.CreateAll(existingModel.PartNumbers);

                    // add components for ModelType.QualityCheck
                    if (existingModel.ModelType == ModelType.QualityCheck)
                    {
                        // component to contain product serial
                        var serialComponent = new ProductComponent
                        {
                            Id = Guid.NewGuid(),
                            ComponentName = STR_SERIAL,
                            ProductModelState = GTSDataRepository.GetListByQuery<ProductModelState>("FROM ProductModelState pms WHERE pms.Name = 'QC'").FirstOrDefault(),
                            RevisionInputMask = @"^[0-9a-zA-Z ]{3,256}$", // regex has a space in the list of possible characters
                            ComponentRequirement = ComponentRequirement.Revision,
                            ProductModel = existingModel,
                            SequenceOrder = 1
                        };
                        GTSDataRepository.Create<ProductComponent>(serialComponent);

                        // component to contain model name and version
                        var modelNameAndSerialComponent = new ProductComponent
                        {
                            Id = Guid.NewGuid(),
                            ComponentName = STR_MODEL_NAME,
                            ProductModelState = GTSDataRepository.GetListByQuery<ProductModelState>("FROM ProductModelState pms WHERE pms.Name = 'QC'").FirstOrDefault(),
                            RevisionInputMask = @"^[0-9a-zA-Z ]{3,256}$", // regex has a space in the list of possible characters
                            ComponentRequirement = ComponentRequirement.Revision,
                            ProductModel = existingModel,
                            SequenceOrder = 2
                        };
                        GTSDataRepository.Create<ProductComponent>(modelNameAndSerialComponent);

                        // add components to model
                        existingModel.ProductComponents.Add(serialComponent);
                        existingModel.ProductComponents.Add(modelNameAndSerialComponent);
                    }
                }
                else
                {
                    GTSDataRepository.Update<ProductModel>(existingModel);
                }

                // check part nrs: recreate collection
                if (existingModel.NHVersion > 0)
                {
                    existingModel.PartNumbers.ForEach(x => GTSDataRepository.Delete<PartNumber>(x.Id));
                    //existingModel.PartNumbers = null;
                    GTSDataRepository.Update<ProductModel>(existingModel);

                    // check part nrs: assign collection next
                    existingModel.PartNumbers = productModel.PartNumbers ?? new List<PartNumber>();

                    // check part nrs: create Id's
                    existingModel.PartNumbers.ForEach(nr =>
                    {
                        nr.Id = Guid.NewGuid();
                        nr.NHVersion = 0;
                        nr.ProductModel = existingModel;
                        GTSDataRepository.Create(nr);
                    });
                }

                GTSDataRepository.Update<ProductModel>(existingModel);

                _logger.LogInfo("User {0} has saved / edited model '{1}'.", User.Identity.Name, existingModel.Name);

                //return new HttpStatusCodeResult(200, "Save executed without errors");
                return SerializeForWeb(existingModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);

                throw;
            }
        }

        /// <summary>
        /// Searches product models and returns results as a json string.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult SearchProductAssemblyJson(string searchString, int serialType)
        {
            _logger.LogInfo("User {0} attempts to search for a product assembly using search term: " + searchString, User.Identity.Name);

            try
            {
                searchString = string.Format("{0}{1}{0}", "%", searchString);

                ICriteria rawSearchResults;
                if (serialType == 1)
                    rawSearchResults = UnitOfWorkSession.CreateCriteria<ProductAssembly>()
                        .Add(Expression.Like("ProductSerial", searchString));
                else
                    rawSearchResults = UnitOfWorkSession.CreateCriteria<ProductAssembly>()
                        .Add(Expression.Like("PublicProductSerial", searchString));

                var searchResults = rawSearchResults
                    .List<ProductAssembly>()
                    .Select(x => new { Id = x.Id, Serial = x.ProductSerial, PublicSerial = x.PublicProductSerial, ModelName = x.ProductModel.Name })
                    .ToList();

                /*
                searchResults.AddRange(GTSDataRepository
                   .GetListByQuery<ProductAssembly>("From ProductAssembly pa WHERE pa.ProductSerial like :searchString",
                   new Dictionary<string, object>() { { "searchString", string.Format("%{0}%", searchString) } })
                   .Select(x => new { Id = x.Id, Serial = x.ProductSerial, ModelName = x.ProductModel.Name })
                   .ToList());
                */
                return SerializeForWeb(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Searches product models and returns results as a json string.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult SearchProductModelJson(Guid productId, string searchString)
        {
            _logger.LogInfo("User {0} attempts to search for a product component using search term: " + searchString, User.Identity.Name);

            // we need to reference the base model instead of a particular version
            var searchResults = GTSDataRepository
                   //.GetListByQuery<ProductModel>($"From ProductModel pm WHERE pm.IsArchived = false AND pm.IsReleased = true AND pm.Name like :searchString AND pm.Id != :productId",
                   .GetListByQuery<ProductModel>($"From ProductModel pm WHERE pm.IsArchived = false AND pm.Name like :searchString AND pm.IsReleased = false AND pm.Id != :productId",
                   new Dictionary<string, object>() { { "searchString", string.Format("%{0}%", searchString) }, { "productId", productId } })
                   .Select(x => new { Id = x.Id, Value = x.BaseModelId, Name = x.Name, IdMask = x.IdMask })
                   .ToList();

            return Json(searchResults, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Removes the Scrapped state from an Assembly.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult UnScrap(Guid id)
        {
            _logger.LogInfo("User {0} is attempting to unscrap an assembly with id {1} by setting its state back to the first empty assembly component's state", User.Identity.Name, id);
            try
            {
                var assembly = GTSDataRepository.GetById<ProductAssembly>(id);

                ProductModelState state = null;

                // going back from the last component, take the state of the first component that has no serial or revision (last empty field)
                assembly.ComponentAssemblies.OrderByDescending(component => component.ProductComponent.SequenceOrder).ForEach(component =>
                {
                    if (component.ProductComponent.ComponentRequirement != ComponentRequirement.None && (string.IsNullOrEmpty(component.Serial) && string.IsNullOrEmpty(component.Revision)))
                    {
                        if (state == null)
                            state = component.ProductComponent.ProductModelState;
                    }
                });

                // if no state is determined yet, take the state of the first component
                if (state == null)
                    state = assembly.ComponentAssemblies.FirstOrDefault().ProductComponent.ProductModelState;

                assembly.ProductModelState = state;
                GTSDataRepository.Update(assembly);

                return new HttpStatusCodeResult(200, "Assembly no longer scrapped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Updates a serial field in a component assembly of a product used in batch mode.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        public ActionResult UpdateBatchSerial(string productSerial, Guid baseModelId, Guid componentId, string value)
        {
            _logger.LogInfo("User {0} attempts to update a serial input value for an item used in batchmode. Product serial: {1}, base model id: {4}, component id: {2}, input value: {3}", User.Identity.Name, productSerial, componentId, value, baseModelId);

            try
            {
                var componentAssembly = GTSDataRepository.GetByProductSerial<ProductAssembly>(productSerial, baseModelId).ComponentAssemblies.Where(ca => ca.ProductComponent.Id == componentId).First();

                UpdateComponentAssembly(componentAssembly, componentAssembly.Revision, value);

                return new HttpStatusCodeResult(200, "Component serial updated for " + productSerial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public ActionResult ValidateBatchSerials(string mainProductSerial, Guid baseModelId, string serialsAsString)
        {
            _logger.LogInfo("User {0} is attempting to start batch mode and validate a number of assembly serials: {1}", User.Identity.Name, serialsAsString);
            try
            {
                if (string.IsNullOrEmpty(serialsAsString) || serialsAsString == "[]")
                    return new HttpStatusCodeResult(200, "No serials passed");

                //var serials = JsonConvert.DeserializeObject(serialsAsString);
                List<dynamic> parsedSerials = JsonConvert.DeserializeObject<List<dynamic>>(serialsAsString);

                var mainProduct = GTSDataRepository.GetByProductSerial<ProductAssembly>(mainProductSerial, baseModelId);
                if (mainProduct == null)
                    throw new InvalidOperationException(string.Format("No product found for serial {0} and base model id {1}", mainProduct, baseModelId));

                var errorMessages = new List<string>();

                var productModelVersion = mainProduct.ProductModel.Version;

                var currentAssemblyStateIndex = mainProduct.CurrentStateIndex;

                var resultList = new List<dynamic>();

                foreach (var item in parsedSerials)
                {
                    Console.WriteLine(item.serial);
                    var serial = item.serial.ToString();
                    ProductAssembly product = GTSDataRepository.GetByProductSerial<ProductAssembly>(serial, baseModelId);

                    var tmpDTO = new { serial = serial, serialInputs = new List<dynamic>() };
                    product.ComponentAssemblies.ForEach(ca =>
                    {
                        //if (!string.IsNullOrEmpty(ca.Serial))
                        if (ca.ProductComponent.ComponentRequirement == ComponentRequirement.Serial || ca.ProductComponent.ComponentRequirement == ComponentRequirement.Both)
                            tmpDTO.serialInputs.Add(new { componentId = ca.ProductComponent.Id, value = ca.Serial });
                    });

                    resultList.Add(tmpDTO);

                    if (product == null)
                    {
                        errorMessages.Add(string.Format("No product found for serial {0} and base model id {1}", serial, baseModelId));
                        break;
                    }

                    if (currentAssemblyStateIndex != product.CurrentStateIndex)
                        errorMessages.Add(string.Format("Product with serial {0} and base model id {1} is in the wrong state.", serial, baseModelId));

                    if (productModelVersion != product.ProductModel.Version)
                        errorMessages.Add(string.Format("Product with serial {0} and base model id {1} has the wrong product model version.", serial, baseModelId));
                }

                if (errorMessages.Count > 0)
                    throw new InvalidOperationException(String.Join(String.Empty, errorMessages.ToArray()));

                return SerializeForWeb(resultList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        private T Cast<T>(object source, T target)
        {
            return (T)source;
        }

        private List<ProductModel> GetAssociatedProductModels(Guid baseModelId)
        {
            var session = UnitOfWorkSession;

            var productModelCriteria = session.CreateCriteria<ProductModel>();
            productModelCriteria = productModelCriteria.Add(Expression.Eq("IsArchived", false));
            productModelCriteria = productModelCriteria.Add(Expression.Eq("IsReleased", true));
            productModelCriteria = productModelCriteria.CreateAlias("ProductComponents", "pcs");
            productModelCriteria = productModelCriteria.CreateAlias("pcs.UnderlyingProductModel", "upm");
            productModelCriteria = productModelCriteria.Add(Expression.Eq("upm.BaseModelId", baseModelId));

            var typedAssociatedProductModelList = productModelCriteria.List<ProductModel>().ToList();
            typedAssociatedProductModelList = typedAssociatedProductModelList.GroupBy(p => p.Id).Select(g => g.First()).ToList();

            return typedAssociatedProductModelList;
        }

        /// <summary>
        /// Gets the product by its identifier and returns result as json string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private object GetProductJsonByIdAsString(string id)
        {
            _logger.LogInfo("User {0} attempts to retrieve product model with id {1} and return it as a json string.", User.Identity.Name, id);

            try
            {
                var guidId = Guid.NewGuid();
                var productModel = new ProductModel { Date = DateTime.Now, ModelType = ModelType.Standard };
                var releaseId = string.Empty;
                DateTime? releaseDate = null;
                var releaseComment = string.Empty;
                var releaseVersion = string.Empty;
                List<object> simplifiedAssociatedModelList = null;

                if (Guid.TryParse(id, out guidId))
                {
                    productModel = GTSDataRepository.GetById<ProductModel>(guidId);

                    if (productModel != null && productModel.BaseModelId != null && productModel.BaseModelId != Guid.Empty)
                    {
                        // check for existing releases
                        var typedAssociatedProductModelList = GetAssociatedProductModels(productModel.BaseModelId);

                        simplifiedAssociatedModelList = typedAssociatedProductModelList.Select(x => new { Name = x.Name, Id = x.Id, Version = x.Version }).Distinct().ToList<object>();

                        var potentialReleasedModels = GTSDataRepository.GetListByQuery<ProductModel>("FROM ProductModel pm WHERE pm.BaseModelId = :id AND pm.IsReleased = true AND pm.IsArchived = false",
                                new Dictionary<string, object>() { { "id", productModel.BaseModelId } });

                        if (potentialReleasedModels == null || potentialReleasedModels.Count() > 1)
                            throw new InvalidDataException("This model has more then one released version.");

                        var releasedModel = potentialReleasedModels.FirstOrDefault();

                        // determine id of current releases
                        if (releasedModel != null)
                        {
                            releaseId = releasedModel.Id.ToString();
                            releaseDate = releasedModel.ReleaseDate;
                            releaseComment = releasedModel.ReleaseComment;
                            releaseVersion = "v" + releasedModel.Version;
                        }
                    }
                }

                if (productModel == null)
                    throw new InvalidOperationException("Cannot find product model with id: " + guidId);

                var defaultConfig = new ProductModelConfiguration { Id = Guid.NewGuid(), Name = "default", Color = "#9d9d9d", ConfigIndex = 0 };

                // check if at least the "default" configuration exists
                if (productModel.ProductModelConfigurations != null && productModel.ProductModelConfigurations.Where(config => config.Name.ToLower() == "default").Count() == 0)
                {
                    GTSDataRepository.Create<ProductModelConfiguration>(defaultConfig);

                    productModel.ProductModelConfigurations.Add(defaultConfig);

                    GTSDataRepository.Update<ProductModel>(productModel);
                }
                /*
                // do component check next
                productModel.ProductComponents.ForEach(component =>
                {
                    if (component.ProductModelConfigurations != null && component.ProductModelConfigurations.Where(config => config.Color == defaultConfig.Color && config.Name == defaultConfig.Name).Count() == 0)
                    {
                        component.ProductModelConfigurations.Add(defaultConfig);
                        GTSDataRepository.Update<ProductComponent>(component);
                        GTSDataRepository.Update<ProductModelConfiguration>(defaultConfig);
                    }
                });
                */
                //return productModelForUi;*/
                return new { Model = productModel, ReleaseId = releaseId, ReleaseDate = releaseDate, ReleaseComment = releaseComment, ReleaseVersion = releaseVersion, AssociatedProductModelList = simplifiedAssociatedModelList };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Releases the product.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="releaseComment">The release comment.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        private void ReleaseProduct(ProductModel model, string releaseComment) //, int version)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts  to release a product for use.");

            // get the currently released model
            var latestReleasedModel = GTSDataRepository.GetListByQuery<ProductModel>("FROM ProductModel pm WHERE pm.BaseModelId = :baseModelId AND pm.IsArchived = false AND pm.IsReleased = true",
                 new Dictionary<string, object>() { { "baseModelId", model.BaseModelId } }).OrderByDescending(x => x.Version).FirstOrDefault();

            // get most recent version of models with same base model id
            var latestUnreleasedReleasedModel = GTSDataRepository.GetListByQuery<ProductModel>("FROM ProductModel pm WHERE pm.BaseModelId = :baseModelId AND pm.IsReleased = false",
                new Dictionary<string, object>() { { "baseModelId", model.BaseModelId } }).OrderByDescending(x => x.Version).FirstOrDefault();

            // archive currently released model
            if (latestReleasedModel != null)
            {
                //currentlyReleasedModel.IsArchived = true;
                latestReleasedModel.ArchivedBy = User.Identity.Name;
                GTSDataRepository.ArchiveById<ProductModel>(latestReleasedModel.Id, User.Identity.Name);
            }

            // create unreleased clone
            var clonedModel = latestUnreleasedReleasedModel.Clone();

            clonedModel.EditedBy = User.Identity.Name;
            GTSDataRepository.Create<ProductModel>(clonedModel);
            GTSDataRepository.CreateAll<ProductModelConfiguration>(clonedModel.ProductModelConfigurations);
            GTSDataRepository.CreateAll<PartNumber>(clonedModel.PartNumbers);

            GTSDataRepository.CreateAll<ProductComponent>(clonedModel.ProductComponents);
            foreach (var component in clonedModel.ProductComponents)
            {
                GTSDataRepository.CreateAll<GTSWorkInstruction>(component.WorkInstructions);
                GTSDataRepository.CreateAll<ProductModelConfiguration>(component.ProductModelConfigurations);
            }

            GTSDataRepository.UpdateAll<AssemblyTool>(clonedModel.Tooling);

            // release current model
            latestUnreleasedReleasedModel.IsReleased = true;
            latestUnreleasedReleasedModel.EditedBy = User.Identity.Name;
            latestUnreleasedReleasedModel.ReleaseComment = releaseComment;
            latestUnreleasedReleasedModel.ReleaseDate = DateTime.Now;
            GTSDataRepository.Update<ProductModel>(latestUnreleasedReleasedModel);

            _logger.LogInfo("User {0} has released version {1} of  model '{2}'.", User.Identity.Name, model.Version, model.Name);
        }

        #endregion Product Models

        #region Product Components

        /// <summary>
        /// Adds the new component.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <returns></returns>
        public ActionResult AddNewComponent(Guid id)
        {
            try
            {
                _logger.LogInfo("User {0} is attempting to create a new component for model with id {1}", User.Identity.Name, id);

                var model = GTSDataRepository.GetById<ProductModel>(id);

                var states = GTSDataRepository.GetAll<ProductModelState>();
                var state = states.Where(x => x.Name == "Assembly").FirstOrDefault();

                var component = new ProductComponent
                {
                    ProductModel = model,
                    WorkInstructions = new List<GTSWorkInstruction>(),
                    ProductModelState = state
                };

                model.ProductComponents.Add(component);

                GTSDataRepository.Update(model);
                GTSDataRepository.Create(component);

                return SerializeForWeb(component);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Archives the instruction.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult ArchiveInstruction(Guid id)
        {
            try
            {
                _logger.LogInfo("User {0} attempts to archive instruction with id {1}.", User.Identity.Name, id);

                /*
                // remove image, if needed
                var instruction = GTSDataRepository.Get<GTSWorkInstruction>(id);
                if (!string.IsNullOrEmpty(instruction.RelativeImagePath) && !instruction.RelativeImagePath.Contains("placeholder"))
                    RemoveImage(id.ToString());
                    */

                var instruction = GTSDataRepository.GetById<GTSWorkInstruction>(id);

                // archive object
                GTSDataRepository.ArchiveById<GTSWorkInstruction>(id, User.Identity.Name);

                TrackRemoval<GTSWorkInstruction, ProductComponent>(instruction, instruction.ProductComponent, "WorkInstructions");

                return new HttpStatusCodeResult(200, "Instruction archived.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Archives the instruction.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult ArchiveTool(Guid id, string releaseComment)
        {
            try
            {
                _logger.LogInfo("User {0} attempts to archive assembly tool with id {1}.", User.Identity.Name, id);

                //var modelsToRemoveFromTool = new List<ProductModel>();

                var tool = GTSDataRepository.GetById<AssemblyTool>(id);

                var modelsToRemoveFromTool = GTSDataRepository.GetListByQuery<ProductModel>("FROM ProductModel pm WHERE pm.IsArchived = false AND pm.IsReleased = false")
                    .Where(model => model.Tooling.Contains(tool)).ToList();

                /*
                tool.ProductModels.ForEach(model =>
                {
                    model.Tooling.Remove(tool);
                    GTSDataRepository.Save<ProductModel>(model);
                    modelsToRemoveFromTool.Add(model);
                });*/

                modelsToRemoveFromTool.ForEach(modelToRemoveFromTool =>
                {
                    modelToRemoveFromTool.Tooling.Remove(tool);

                    TrackRemoval<AssemblyTool, ProductModel>(tool, modelToRemoveFromTool, "Tooling");

                    GTSDataRepository.Update<ProductModel>(modelToRemoveFromTool);

                    //tool.ProductModels.Remove(modelToRemoveFromTool);
                    ReleaseProduct(modelToRemoveFromTool, releaseComment);
                });

                // archive object
                GTSDataRepository.ArchiveById<AssemblyTool>(id, User.Identity.Name);

                return new HttpStatusCodeResult(200, "Instruction archived.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                throw;
            }
        }

        /// <summary>
        /// Displays a page to edit components work instructions.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult ComponentWorkInstructions(Guid id)
        {
            return View();
        }

        /// <summary>
        /// Deletes a product model.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public HttpStatusCodeResult DeleteProductComponent(Guid id)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts to delete a product component.");

            var component = GTSDataRepository.GetById<ProductComponent>(id);
            TrackRemoval(component, component.ProductModel, "ProductComponents");
            GTSDataRepository.ArchiveById<ProductComponent>(id, User.Identity.Name);

            _logger.LogInfo("User {0} has deleted (archived) model with ID '{1}'.", User.Identity.Name, id);

            return new HttpStatusCodeResult(200, "Product deleted.");
        }

        /// <summary>
        /// Gets a product component as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetProductComponentJson(Guid id)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts to retrieve a single product component for a particular product model and return it as a json string.");

            var component = GTSDataRepository.GetById<ProductComponent>(id);

            return SerializeForWeb(component);
        }

        /// <summary>
        /// Gets the product components as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetProductComponentsJson(string id)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts to retrieve product components for a particular product model and return them as a json string.");

            var guidId = Guid.Empty;

            if (!Guid.TryParse(id, out guidId))
                return Json(null, JsonRequestBehavior.AllowGet);

            var model = GTSDataRepository.GetById<ProductModel>(guidId);

            var components = model.ProductComponents
                .Where(x => x.IsArchived == false)
                .OrderBy(x => x.SequenceOrder)
                .ToList();

            return SerializeForWeb(components);
        }

        /// <summary>
        /// Removes an image.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult RemoveImage(string id)
        {
            Guid instructionId;
            _logger.LogDebug("User {0} attempts to remove an instruction image", User.Identity.Name);
            try
            {
                if (Guid.TryParse(id, out instructionId))
                {
                    // get component
                    var instruction = GTSDataRepository.GetById<GTSWorkInstruction>(instructionId);

                    if (instruction.RelativeImagePath.Contains("placeholder"))
                        throw new InvalidOperationException("You cannot delete the placeholder image.");

                    var imagePath = Path.Combine(Server.MapPath("~/Content/Images/WorkInstructions"), instruction.RelativeImagePath);
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);

                    // set image property to empty
                    instruction.RelativeImagePath = "placeholder.png";

                    instruction.EditedBy = User.Identity.Name;

                    instruction.EditedBy = User.Identity.Name;

                    // save component again
                    GTSDataRepository.Update<GTSWorkInstruction>(instruction);

                    //return new HttpStatusCodeResult(200, "Image removed");
                    return SerializeForWeb(instruction);
                }
                else
                    return new HttpStatusCodeResult(500, "Id parameter was invalid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);

                return new HttpStatusCodeResult(500, ex.Message);
            }
        }

        /// <summary>
        /// Removes the tool from a models' tool list.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns></returns>
        public ActionResult RemoveToolFromToolList(Guid id, Guid modelId)
        {
            _logger.LogInfo("User {0} is attempting to remove a tool with id {1} from the model with id {2}", User.Identity.Name, id, modelId);
            try
            {
                var tool = GTSDataRepository.GetById<AssemblyTool>(id);
                var model = GTSDataRepository.GetById<ProductModel>(modelId);
                model.Tooling.Remove(tool);
                //tool.ProductModels.Remove(model);
                TrackRemoval<AssemblyTool, ProductModel>(tool, model, "Tooling");

                GTSDataRepository.Update(model);
                //GTSDataRepository.Save(tool);

                return new HttpStatusCodeResult(200, tool.Name + " removed from model");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Reorders the components.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="idString">The identifier string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ReorderComponents(string modelId, string idString)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts to reorder a collection of product components.");

            try
            {
                var idList = idString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                var guidId = Guid.Empty;
                if (!Guid.TryParse(modelId, out guidId))
                    throw new InvalidOperationException("Invalid Id passed as parameter.");

                var model = GTSDataRepository.GetById<ProductModel>(guidId);
                var i = 1;
                foreach (var id in idList)
                {
                    Guid componentId;
                    Guid.TryParse(id, out componentId);

                    var component = model.ProductComponents.Where(x => x.Id == componentId).FirstOrDefault();

                    if (i != component.SequenceOrder)
                        TrackProperty(component, "SequenceOrder", component.SequenceOrder.ToString(), i.ToString());

                    component.SequenceOrder = i;

                    i++;
                }

                GTSDataRepository.UpdateAll<ProductComponent>(model.ProductComponents);

                _logger.LogInfo("User {0} has reordered product components for model '{1}'.", User.Identity.Name, model.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }

            return new HttpStatusCodeResult(200, "Components reordered.");
        }

        /// <summary>
        /// Reorders the component work instructions.
        /// </summary>
        /// <param name="componentId">The component identifier.</param>
        /// <param name="idString">The identifier string.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Invalid Id passed as parameter.</exception>
        [HttpPost]
        public ActionResult ReorderInstructions(string componentId, string idString)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts to reorder a collection of product component work instructions.");

            try
            {
                var idList = idString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                var guidId = Guid.Empty;
                if (!Guid.TryParse(componentId, out guidId))
                    throw new InvalidOperationException("Invalid Id passed as parameter.");

                var component = GTSDataRepository.GetById<ProductComponent>(guidId);
                var i = 1;
                foreach (var id in idList)
                {
                    Guid instructionId;
                    Guid.TryParse(id, out instructionId);
                    var instruction = component.WorkInstructions.Where(x => x.Id == instructionId).FirstOrDefault();
                    instruction.SequenceOrder = i;

                    i++;
                }

                GTSDataRepository.UpdateAll<GTSWorkInstruction>(component.WorkInstructions);

                _logger.LogInfo("User {0} has reordered product component work instructions for model '{1}'.", User.Identity.Name, component.ComponentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }

            return new HttpStatusCodeResult(200, "Work instructions reordered.");
        }

        /// <summary>
        /// Saves the instruction.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <param name="parentObjectAsString">The parent object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveInstructionJson(string objectAsString, string parentObjectAsString)
        {
            _logger.LogInfo("User {0} is attempting to add / edit a work instruction.", User.Identity.Name);

            try
            {
                var productComponentFromUi = JsonConvert.DeserializeObject<ProductComponent>(parentObjectAsString);
                var productComponent = GTSDataRepository.GetById<ProductComponent>(productComponentFromUi.Id);

                GTSWorkInstruction instruction = null;
                GTSWorkInstruction existingInstruction = null;
                var isNew = false;

                var instructionFromUi = JsonConvert.DeserializeObject<GTSWorkInstruction>(objectAsString);
                if (instructionFromUi.Id == Guid.Empty)
                {
                    instruction = new GTSWorkInstruction();
                    instruction.Id = Guid.NewGuid();
                    instruction.CreationDate = DateTime.Now;
                    instruction.ProductComponent = productComponent;
                    instruction.SequenceOrder = productComponent.WorkInstructions.Count + 1;
                    productComponent.WorkInstructions.Add(instruction);

                    isNew = true;
                }
                else
                {
                    existingInstruction = GTSDataRepository.GetById<GTSWorkInstruction>(instructionFromUi.Id);
                    instruction = new GTSWorkInstruction();
                }

                instruction.Title = instructionFromUi.Title;
                instruction.Description = instructionFromUi.Description;

                if (!instructionFromUi.RelativeImagePath.Contains("No files selected to upload"))
                    instruction.RelativeImagePath = instructionFromUi.RelativeImagePath;

                instruction.ModificationDate = DateTime.Now;
                instruction.EditedBy = User.Identity.Name;
                instruction.ProductComponent = productComponent;

                productComponent.EditedBy = User.Identity.Name;

                if (!isNew)
                {
                    if (instructionFromUi.NHVersion != existingInstruction.NHVersion)
                        throw new StaleObjectStateException("Work Instruction", existingInstruction.Id);

                    instruction.SequenceOrder = existingInstruction.SequenceOrder;
                    instruction.Id = existingInstruction.Id;
                    instruction.CreationDate = existingInstruction.CreationDate;
                    /*
                    instruction.CreationDate = existingInstruction.CreationDate;
                    instruction.SequenceOrder = existingInstruction.SequenceOrder;
                    instruction.Id = existingInstruction.Id;

                    */
                    TrackChanges(existingInstruction, instruction);

                    // NEEDS REFACTORING
                    existingInstruction.Title = instruction.Title;

                    existingInstruction.Description = instruction.Description;
                    existingInstruction.SequenceOrder = instructionFromUi.SequenceOrder;
                    existingInstruction.RelativeImagePath = instruction.RelativeImagePath;
                    GTSDataRepository.Update<GTSWorkInstruction>(existingInstruction);
                }
                else
                {
                    productComponent.WorkInstructions.Add(instruction);
                    TrackAddition<GTSWorkInstruction, ProductComponent>(instruction, productComponent, "WorkInstructions");
                    GTSDataRepository.Create<GTSWorkInstruction>(instruction);
                    GTSDataRepository.Update<ProductComponent>(productComponent);
                }

                return new HttpStatusCodeResult(200, "Work instruction saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Saves the product component json.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveProductComponentJson(string objectAsString, string modelIdAsString)
        {
            try
            {
                _logger.LogInfo("User " + User.Identity.Name + " attempts to save a product component (" + objectAsString + ")");

                var formCollection2 = (Dictionary<string, object>)new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(objectAsString);

                // determine ID and try to retrieve existing item
                Guid componentId;
                if (formCollection2.ContainsKey("Id"))
                    componentId = formCollection2["Id"].ToString() == Guid.Empty.ToString() ? Guid.NewGuid() : Guid.Parse(formCollection2["Id"].ToString());
                else
                    componentId = Guid.NewGuid();

                var isNewComponent = false;

                // init enity
                ProductComponent component = new ProductComponent();
                ProductComponent existingComponent = GTSDataRepository.GetById<ProductComponent>(componentId);
                if (existingComponent == null)
                {
                    component.Id = Guid.NewGuid();
                    isNewComponent = true;
                }
                else
                {
                    // update entity with form collection
                    component.Id = componentId;
                }
                component.ComponentName = formCollection2["ComponentName"].ToString();

                // determine state
                var stateId = Guid.Parse(((Dictionary<string, object>)formCollection2["ProductModelState"])["Id"].ToString());
                var state = GTSDataRepository.GetById<ProductModelState>(stateId);
                component.ProductModelState = state;

                // convert requirement string to requirement enumeration
                var selectedRequirement = ComponentRequirement.None;
                if (formCollection2.ContainsKey("ComponentRequirement"))
                {
                    selectedRequirement = (ComponentRequirement)int.Parse(formCollection2["ComponentRequirement"].ToString());
                }
                component.ComponentRequirement = selectedRequirement;

                if (formCollection2.ContainsKey("DeviceKeyword") && formCollection2["DeviceKeyword"] != null)
                    component.DeviceKeyword = formCollection2["DeviceKeyword"].ToString();

                // retrieve revision and serial input masks (if any)
                component.RevisionInputMask = formCollection2.ContainsKey("RevisionInputMask") ? formCollection2["RevisionInputMask"] == null ? string.Empty : formCollection2["RevisionInputMask"].ToString().Trim() : string.Empty;
                component.SerialInputMask = formCollection2.ContainsKey("SerialInputMask") ? formCollection2["SerialInputMask"] == null ? string.Empty : formCollection2["SerialInputMask"].ToString().Trim() : string.Empty;

                // (re)assign configurations
                component.ProductModelConfigurations = existingComponent?.ProductModelConfigurations;

                // determine parent model
                var modelId = Guid.Parse(modelIdAsString);
                ProductModel model = GTSDataRepository.GetById<ProductModel>(modelId);
                if (model != null)
                {
                    component.ProductModel = model;
                    model.ProductComponents.Add(component);
                    GTSDataRepository.Update<ProductModel>(model);

                    if (isNewComponent)
                    {
                        var config = model.ProductModelConfigurations.Where(currentConfig => currentConfig.Name.ToLower() == "default").Single();

                        if (component.ProductModelConfigurations == null)
                            component.ProductModelConfigurations = new List<ProductModelConfiguration>();

                        component.ProductModelConfigurations.Add(config);

                        GTSDataRepository.Update<ProductModelConfiguration>(config);
                    }
                }

                // set sequence order
                component.SequenceOrder = model.ProductComponents != null ? model.ProductComponents.Count + 1 : 1;
                if (formCollection2.ContainsKey("SequenceOrder"))
                    component.SequenceOrder = (int)formCollection2["SequenceOrder"];

                // if any, set underlying product model (reference)
                if (formCollection2.ContainsKey("UnderlyingProductModel"))
                {
                    var underlyingModelIdAsString = string.Empty;
                    if (formCollection2["UnderlyingProductModel"] != null)
                    {
                        // the object in the form collection can either be a fully fledged model ...
                        if (((Dictionary<string, object>)formCollection2["UnderlyingProductModel"]).Count() > 3)
                            underlyingModelIdAsString = ((Dictionary<string, object>)formCollection2["UnderlyingProductModel"])["Id"].ToString();
                        else // ... or it can be autocomplete object
                            underlyingModelIdAsString = ((Dictionary<string, object>)formCollection2["UnderlyingProductModel"]).ToList()[0].Value.ToString();

                        var underlyingModel = GTSDataRepository.GetById<ProductModel>(Guid.Parse(underlyingModelIdAsString));

                        if (underlyingModel.ModelType == ModelType.QualityCheck)
                        {
                            var underlyingModelReleaseCriteria = UnitOfWorkSession.CreateCriteria<ProductModel>()
                                .Add(Expression.Eq("BaseModelId", underlyingModel.BaseModelId))
                                .Add(Expression.Eq("IsReleased", true))
                                .Add(Expression.Eq("IsArchived", false));

                            var underlyingModelHasRelease = underlyingModelReleaseCriteria.List().Count > 0;

                            if (!underlyingModelHasRelease)
                                throw new InvalidOperationException("You cannot reference an unreleased Quality Check model");
                        }

                        component.UnderlyingProductModel = underlyingModel;
                    }
                    else
                    {
                        // allow clearing of the underlying model
                        component.UnderlyingProductModel = null;
                    }
                }

                component.EditedBy = User.Identity.Name;
                if (formCollection2.ContainsKey("NHVersion"))
                    component.NHVersion = Int32.Parse(formCollection2["NHVersion"].ToString());
                else
                    component.NHVersion = 0;

                if (isNewComponent)
                    TrackAddition<ProductComponent, ProductModel>(component, model, "ProductComponents");
                else
                    TrackChanges<ProductComponent>(existingComponent, component);

                if (existingComponent != null)
                    UnitOfWorkSession.Merge(component);
                else
                    GTSDataRepository.Create(component);

                _logger.LogInfo("User {0} has created / edited product component '{1}'.", User.Identity.Name, component);

                //return new HttpStatusCodeResult(200, component.Id.ToString());
                return SerializeForWeb(component);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Saves an assembly tool.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="code">The code.</param>
        /// <param name="setting">The setting.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveTool(Guid modelId, Guid id, string name, string code, string setting, string description)
        {
            var model = GTSDataRepository.GetById<ProductModel>(modelId);
            var tool = new AssemblyTool { Id = Guid.NewGuid() };
            if (id != Guid.Empty)
                tool = GTSDataRepository.GetById<AssemblyTool>(id);

            tool.Name = name;
            tool.ToolCode = code;
            tool.Setting = setting;
            tool.Description = description;
            tool.EditedBy = User.Identity.Name;

            // associate with parent object
            model.Tooling.Add(tool);
            //tool.ProductModels.Add(model);

            GTSDataRepository.Create<AssemblyTool>(tool);
            GTSDataRepository.Update<ProductModel>(model);

            return new HttpStatusCodeResult(200, "Assembly tool saved.");
        }

        /// <summary>
        /// Saves the assembly tool.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SaveToolJson(string objectAsString)
        {
            _logger.LogInfo("User {0} is attempting to add / edit a tool.", User.Identity.Name);

            // deserialize tool from UI
            AssemblyTool tool = JsonConvert.DeserializeObject<AssemblyTool>(objectAsString);
            dynamic objectParsedFromFormCollection = JsonConvert.DeserializeObject(objectAsString);

            var modelsWithThisTool = new List<ProductModel>();

            // retrieve existing tool and archive it
            var existingTool = GTSDataRepository.GetById<AssemblyTool>(tool.Id);
            var previousNHVersion = 0;
            if (existingTool != null)
            {
                var session = UnitOfWorkSession;

                if (existingTool.IsArchived)
                    throw new StaleObjectStateException("AssemblyTool", tool.Id);

                previousNHVersion = existingTool.NHVersion;

                var modelsWithThisToolCriteria = session.CreateCriteria<ProductModel>();
                modelsWithThisToolCriteria = modelsWithThisToolCriteria.Add(Expression.Eq("IsArchived", false));
                modelsWithThisToolCriteria = modelsWithThisToolCriteria.Add(Expression.Eq("IsReleased", false));
                modelsWithThisToolCriteria = modelsWithThisToolCriteria.CreateAlias("Tooling", "tls");
                modelsWithThisToolCriteria = modelsWithThisToolCriteria.Add(Expression.Eq("tls.Id", tool.Id));

                modelsWithThisTool.AddRange(modelsWithThisToolCriteria.List<ProductModel>());

                GTSDataRepository.ArchiveById<AssemblyTool>(existingTool.Id, User.Identity.Name);
            }

            // save new tool
            tool.Id = Guid.NewGuid();
            tool.EditedBy = User.Identity.Name;
            tool.NHVersion = 0;

            // fix associations
            modelsWithThisTool.ForEach(model =>
            {
                //x.Tooling.Add(tool);
                if (existingTool != null)
                {
                    model.Tooling.Remove(existingTool);
                    TrackRemoval<AssemblyTool, ProductModel>(existingTool, model, "Tooling");

                    model.Tooling.Add(tool);
                    TrackAddition<AssemblyTool, ProductModel>(tool, model, "Tooling");

                    //existingTool.ProductModels.Remove(model);
                    GTSDataRepository.Update<ProductModel>(model);
                    //GTSDataRepository.Save<AssemblyTool>(existingTool);
                }
            });

            tool.NHVersion = previousNHVersion + 1;
            GTSDataRepository.Create<AssemblyTool>(tool);

            if (modelsWithThisTool.Count > 0)
            {
                var newReleaseComment = objectParsedFromFormCollection["releaseComment"].Value;

                if (string.IsNullOrEmpty(newReleaseComment))
                    throw new ArgumentException("Could not find a release comment to release associated models.");

                modelsWithThisTool.ForEach(x => ReleaseProduct(x, newReleaseComment));
            }
            //UnitOfWorkSession.Transaction.Commit();
            return new HttpStatusCodeResult(200, "Assembly tool saved.");
        }

        /// <summary>
        /// Scraps the specified assembly.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult Scrap(Guid id)
        {
            _logger.LogInfo("User {0} attempts to scrap assembly with id: {1}", User.Identity.Name, id);
            try
            {
                var assembly = GTSDataRepository.GetById<ProductAssembly>(id);
                assembly.ProductModelState = GTSDataRepository.GetListByQuery<ProductModelState>("FROM ProductModelState pms WHERE pms.Name = 'Scrapped'").FirstOrDefault();

                if (assembly.ProductModelState == null)
                    throw new InvalidOperationException("Could not assign state 'Scrapped' to assembly");

                GTSDataRepository.Update<ProductAssembly>(assembly);

                return new HttpStatusCodeResult(200, "Assembly with id " + id + " is scrapped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Uploads an image.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UploadImage()
        {
            //  Get all files from Request object
            HttpFileCollectionBase files = Request.Files;

            if (files.Count <= 0)
                return new HttpStatusCodeResult(200, "No files selected to upload.");

            var fileName = string.Empty;
            var extension = string.Empty;

            _logger.LogInfo("User {0} is attempting to upload files to server.", User.Identity.Name);
            try
            {
                HttpPostedFileBase file = files[0];
                string imagePath;

                // Checking for Internet Explorer
                if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                {
                    string[] testfiles = file.FileName.Split(new char[] { '\\' });
                    imagePath = testfiles[testfiles.Length - 1];
                }
                else
                    imagePath = file.FileName;

                // Get the complete folder path and attempt to store the file inside it.
                imagePath = Path.Combine(Server.MapPath("~/Content/Images/WorkInstructions"), imagePath);

                // rename images: create unique name using a GUID
                FileInfo fileInfo = new FileInfo(imagePath);
                extension = fileInfo.Extension;
                fileName = Guid.NewGuid().ToString() + fileInfo.Extension;

                imagePath = Path.Combine(Server.MapPath("~/Content/Images/WorkInstructions"), fileName);

                file.SaveAs(imagePath);

                _logger.LogInfo("user {0} has uploaded image {1}", User.Identity.Name, fileName);
                return new HttpStatusCodeResult(200, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /*
        /// <summary>
        /// Reorders the component tooling.
        /// </summary>
        /// <param name="componentId">The component identifier.</param>
        /// <param name="idString">The identifier string.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Invalid Id passed as parameter.</exception>
        [HttpPost]
        public ActionResult ReorderTools(string componentId, string idString)
        {
            _logger.LogInfo("User " + User.Identity.Name + " attempts to reorder a collection of product component tools.");

            try
            {
                var idList = idString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                var guidId = Guid.Empty;
                if (!Guid.TryParse(componentId, out guidId))
                    throw new InvalidOperationException("Invalid Id passed as parameter.");

                var component = GTSDataRepository.Get<ProductComponent>(guidId);
                var i = 1;
                foreach (var id in idList)
                {
                    Guid toolingId;
                    Guid.TryParse(id, out toolingId);
                    //var tool = component.Tooling.Where(x => x.Id == toolingId).FirstOrDefault();
                    tool.SequenceOrder = i;

                    i++;
                }

                GTSDataRepository.UpdateAll<AssemblyTool>(component.Tooling);

                _logger.LogInfo("User {0} has reordered product component tooling for model '{1}'.", User.Identity.Name, component.ComponentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }

            return new HttpStatusCodeResult(200, "Tooling reordered.");
        }
        */

        #endregion Product Components

        /// <summary>
        /// Retrieves an instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T[] Get<T>() where T : DomainObject
        {
            var type = typeof(T);
            var result = GTSDataRepository.GetListByQuery<T>(string.Format("FROM {0} pms WHERE pms.IsArchived = false ORDER BY pms.Name", type)).ToArray();

            return result;
        }
    }
}
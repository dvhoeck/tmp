using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.DAL;
using Newtonsoft.Json;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Script.Serialization;

/// <summary>
///
/// </summary>
namespace Gatewing.ProductionTools.GTS.Controllers
{
    [Authorize]
    [OutputCache(VaryByParam = "*", Duration = 0, NoStore = true)] // disable caching for all actions
    public class RemarkController : BaseController
    {
        // GET: Remark
        public ActionResult Index()
        {
            return View();
        }

        #region Remark flow

        /// <summary>
        /// Adds a cause and solution to a remark.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult AddCauseAndSolutionToRemarkJson(Guid assemblyId, Guid remarkId)
        {
            // get remark
            var remark = GTSDataRepository.GetById<RemarkSymptom>(remarkId);

            var remarkSolutionTmp = new RemarkSymptomSolution
            {
                Id = Guid.NewGuid(),
                Description = string.Empty,
                SolutionDate = DateTime.Now,
                Successful = false,
                RemarkSymptomSolutionType = GTSDataRepository.GetListByQuery<RemarkSymptomSolutionType>("FROM RemarkSymptomSolutionType WHERE Name = 'Replace'").FirstOrDefault(),
                EditedBy = User.Identity.Name
                //CostFactors = new Dictionary<object, string> { { 0, "" } }
            };

            var remarkCauseTmp = new RemarkSymptomCause
            {
                Id = Guid.NewGuid(),
                CauseDate = DateTime.Now,
                CauseType = GTSDataRepository.GetListByQuery<RemarkSymptomCauseType>("FROM RemarkSymptomCauseType WHERE Name = 'Production Error'").FirstOrDefault(),
                Description = string.Empty,
                RemarkSymptomSolution = remarkSolutionTmp,
                RemarkSymptom = remark,
                EditedBy = User.Identity.Name,
                //CostFactors = new Dictionary<object, string> { { "0", "" } }
            };

            // add newly created cause to remark's causes collection
            remark.RemarkSymptomCauses.Add(remarkCauseTmp);

            // save solution
            GTSDataRepository.Create(remarkSolutionTmp);

            // save cause and remark
            GTSDataRepository.Create(remarkCauseTmp);
            GTSDataRepository.Update(remark);

            var list = GetRemarksForAssembly(assemblyId);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Adds a cause to a remark symptom.
        /// </summary>
        /// <param name="id">The remark symptom identifier.</param>
        /// <returns></returns>
        public ActionResult AddCauseToSymptom(Guid id)
        {
            _logger.LogInfo("User {0} attempts to add Remark Symptom Cause to Remark Symptom with id {1}", User.Identity.Name, id);

            // get remark
            var remark = GTSDataRepository.GetById<RemarkSymptom>(id);

            // create new cause
            var remarkCauseTmp = new RemarkSymptomCause
            {
                Id = Guid.NewGuid(),
                CauseDate = DateTime.Now,
                CauseType = GTSDataRepository.GetListByQuery<RemarkSymptomCauseType>("FROM RemarkSymptomCauseType WHERE Name = 'Empty'").FirstOrDefault(),
                Description = string.Empty,
                EditedBy = User.Identity.Name,
                RemarkSymptom = remark
            };

            // save cause and remark
            GTSDataRepository.Create(remarkCauseTmp);
            GTSDataRepository.Update(remark);

            // return simplified object to avoid circular depency errors while serializing
            return Json(
                new
                {
                    Id = remarkCauseTmp.Id,
                    CauseDate = remarkCauseTmp.CauseDate,
                    CauseType = remarkCauseTmp.CauseType,
                    MaterialCost = remarkCauseTmp.MaterialCost,
                    TimeSpent = remarkCauseTmp.TimeSpent,
                    Description = string.Empty,
                }
                , JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Adds a remark to an assembly.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Could not retrieve assembly to add a remark to.</exception>
        public ActionResult AddRemarkToAssembly(Guid id)
        {
            try
            {
                _logger.LogInfo("User {0} attempts to add Remark Symptom to assembly with id {1}", User.Identity.Name, id);

                // get existing product assembly
                var assembly = GTSDataRepository.GetById<ProductAssembly>(id);
                if (assembly == null)
                    throw new ArgumentException("Could not retrieve assembly to add a remark to.");

                var newRemark = new RemarkSymptom
                {
                    Id = Guid.NewGuid(),
                    CreationDate = DateTime.Now,
                    ResolutionDate = DateTime.MaxValue,
                    ProductAssembly = assembly,
                    EditedBy = User.Identity.Name,
                    RemarkSymptomType = GTSDataRepository.GetListByQuery<RemarkSymptomType>("FROM RemarkSymptomType WHERE Name = 'Empty'").FirstOrDefault(),
                };
                GTSDataRepository.Create<RemarkSymptom>(newRemark);

                return Json(GetRemarksForAssembly(id), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Adds a cause to a remark symptom.
        /// </summary>
        /// <param name="id">The remark symptom identifier.</param>
        /// <returns></returns>
        public ActionResult AddSolutionToCause(Guid id)
        {
            _logger.LogInfo("User {0} attempts to add Remark Symptom Soution to Remark Symptom Cause with id {1}", User.Identity.Name, id);

            // get cause
            var cause = GTSDataRepository.GetById<RemarkSymptomCause>(id);

            // create new solution
            var remarkSolutionTmp = new RemarkSymptomSolution
            {
                Id = Guid.NewGuid(),
                SolutionDate = DateTime.Now,
                RemarkSymptomSolutionType = GTSDataRepository.GetListByQuery<RemarkSymptomSolutionType>("FROM RemarkSymptomSolutionType WHERE Name = 'Empty'").FirstOrDefault(),
                Description = string.Empty,
                EditedBy = User.Identity.Name,
            };

            // assign solution to cause
            cause.RemarkSymptomSolution = remarkSolutionTmp;

            // save cause and remark
            GTSDataRepository.Create(remarkSolutionTmp);
            GTSDataRepository.Update(cause);

            // return simplified object to avoid circular depency errors while serializing
            return Json(
                new
                {
                    Id = remarkSolutionTmp.Id,
                    SolutionDate = remarkSolutionTmp.SolutionDate,
                    RemarkSymptomSolutionType = remarkSolutionTmp.RemarkSymptomSolutionType,
                    MaterialCost = remarkSolutionTmp.MaterialCost,
                    TimeSpent = remarkSolutionTmp.TimeSpent,
                    Description = string.Empty,
                }
                , JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Archives the remark symptom.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult ArchiveRemarkCause(Guid id)
        {
            _logger.LogInfo("User {0} attempts to archive Remark Symptom Cause with id {1}", User.Identity.Name, id);
            var cause = GTSDataRepository.GetById<RemarkSymptomCause>(id);
            cause.ArchivalDate = DateTime.Now;
            cause.IsArchived = true;
            cause.ArchivedBy = User.Identity.Name;

            // archive solution as well
            if (cause.RemarkSymptomSolution != null)
            {
                cause.RemarkSymptomSolution.ArchivalDate = DateTime.Now;
                cause.RemarkSymptomSolution.IsArchived = true;
                cause.RemarkSymptomSolution.ArchivedBy = User.Identity.Name;
                GTSDataRepository.Update<RemarkSymptomSolution>(cause.RemarkSymptomSolution);
            }

            GTSDataRepository.Update<RemarkSymptomCause>(cause);

            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Archives the remark symptom.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult ArchiveRemarkSolution(Guid id)
        {
            _logger.LogInfo("User {0} attempts to archive Remark Symptom Solution with id {1}", User.Identity.Name, id);
            var solution = GTSDataRepository.GetById<RemarkSymptomSolution>(id);
            solution.ArchivalDate = DateTime.Now;
            solution.IsArchived = true;
            solution.ArchivedBy = User.Identity.Name;
            GTSDataRepository.Update<RemarkSymptomSolution>(solution);

            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Archives the remark symptom.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult ArchiveRemarkSymptom(Guid id)
        {
            _logger.LogInfo("User {0} attempts to archive Remark Symptom with id {1}", User.Identity.Name, id);
            GTSDataRepository.ArchiveById<RemarkSymptom>(id, User.Identity.Name);
            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Displays the remark history.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult DisplayRemarkHistory(Guid id)
        {
            return View();
        }

        /// <summary>
        /// Displays remarks for a particular model as a json string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult DisplayRemarkHistoryJson(Guid id)
        {
            try
            {
                _logger.LogInfo("User {0} attempts to display all remarks for assembly with id {1}", User.Identity.Name, id);
                var results = GetRemarksForAssembly(id, true);
                //return Json(results, JsonRequestBehavior.AllowGet);

                return SerializeForWeb(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Displays remarks for a particular model as a json string.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult DisplayRemarkJson(Guid id)
        {
            try
            {
                _logger.LogInfo("User {0} attempts to display non archived remarks for assembly with id {1}", User.Identity.Name, id);

                var results = GetRemarksForAssembly(id);

                // get remark causes where the current assembly is mentioned in
                var productAssembly = GTSDataRepository.GetById<ProductAssembly>(id);
                var parentRemarkAssemblies = GTSDataRepository.GetListByQuery<RemarkSymptomCause>("FROM RemarkSymptomCause rsc WHERE rsc.ComponentAssembly.Serial = :productSerial AND rsc.ComponentAssembly.ProductComponent.UnderlyingProductModel.BaseModelId = :baseModelId",
                    new Dictionary<string, object> { { "productSerial", productAssembly.ProductSerial }, { "baseModelId", productAssembly.ProductModel.BaseModelId } })
                    .Select(x => new { ProductSerial = x.ComponentAssembly.ProductAssembly.ProductSerial, AssemblyId = x.ComponentAssembly.ProductAssembly.Id }).ToList();

                var resultObj = new { results = results, parentRemarkAssemblies = parentRemarkAssemblies };

                return Json(resultObj, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a list of unarchived, unresolved remarks.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetRemarkList()
        {
            _logger.LogInfo("User {0} attempts to display the list of open remarks.", User.Identity.Name);
            //return SerializeForWeb(GTSDataRepository.GetListByQuery<RemarkSymptom>("FROM RemarkSymptom rs WHERE rs.IsArchived = false AND rs.Resolved = false").OrderBy(x => x.ProductAssembly.ProductSerial).ToList()); ;

            var remarkList = GTSDataRepository.GetListByQuery<RemarkSymptom>("FROM RemarkSymptom rs WHERE rs.IsArchived = false AND rs.Resolved = false")
                .OrderBy(x => x.CreationDate)
                .Select(x => new
                {
                    Id = x.Id,
                    ProductAssemblyId = x.ProductAssembly.Id,
                    RemarkSymptomTypeName = x.RemarkSymptomType.Name,
                    ProductSerial = x.ProductAssembly.ProductSerial,
                    ProductModelName = x.ProductAssembly.ProductModel.Name,
                    Description = x.Description,
                    CreationDate = x.CreationDate
                })
                .ToList();

            return SerializeForWeb(remarkList); ;
        }

        /// <summary>
        /// Gets the remark stats for a particular assembly.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetRemarkStatsForAssembly(Guid id)
        {
            var remarks = GTSDataRepository.GetListByQuery<RemarkSymptom>("FROM RemarkSymptom rs WHERE rs.ProductAssembly.Id = :id",
                new Dictionary<string, object>() { { "id", id } })
                    .OrderByDescending(x => x.CreationDate).ToList();

            var totalCost = 0m;
            var totalTime = 0m;

            remarks.ForEach(x => x.RemarkSymptomCauses.ToList().ForEach(y =>
            {
                totalCost += y.MaterialCost;
                totalTime += y.TimeSpent;
                totalCost += y.RemarkSymptomSolution == null ? 0 : y.RemarkSymptomSolution.MaterialCost;
                totalTime += y.RemarkSymptomSolution == null ? 0 : y.RemarkSymptomSolution.TimeSpent;
            }));

            return Json(new
            {
                RemarkCount = remarks.Count(),
                ArchivedRemarkCount = remarks.Where(x => x.IsArchived == true).Count(),
                TotalCost = totalCost,
                TotalTime = totalTime
            },
            JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Returns the remark view
        /// </summary>
        /// <returns></returns>
        public ActionResult Remark()
        {
            return View();
        }

        /// <summary>
        /// Returns a view containing a list of remarks.
        /// </summary>
        /// <returns></returns>
        public ActionResult RemarkList()
        {
            return View();
        }

        /// <summary>
        /// Saves the specified remark collection passed as a string.
        /// </summary>
        /// <param name="remarkCollectionAsString">The remark collection as string.</param>
        /// <returns></returns>
        public ActionResult Save(string remarkCollectionAsString)
        {
            _logger.LogInfo("User {0} attempts to save an entire remark flow => {1}", User.Identity.Name, remarkCollectionAsString);

            // deserialize string into a form collection
            var typedFormCollection = new JavaScriptSerializer().Deserialize<dynamic>(remarkCollectionAsString);

            dynamic typedFormCollectionOrderedByDate = ((object[])typedFormCollection).OrderBy(x => ((Dictionary<string, object>)x)["CreationDate"]);

            foreach (var remarkCollection in typedFormCollectionOrderedByDate)
            {
                // determine ID and try to retrieve existing item
                Guid remarkId = remarkCollection["Id"] == Guid.Empty.ToString() ? Guid.NewGuid() : Guid.Parse(remarkCollection["Id"]);

                // get existing version of the remark collection
                var remark = GTSDataRepository.GetById<RemarkSymptom>(remarkId);
                remark.Resolved = remarkCollection["Resolved"];
                if (remark.Resolved)
                    remark.ResolutionDate = DateTime.Now;
                remark.Description = remarkCollection["Description"];
                remark.EditedBy = User.Identity.Name;

                var images = new List<RemarkImage>();
                if (((Dictionary<string, object>)remarkCollection).Keys.Contains("Images"))
                {
                    foreach (var image in remarkCollection["Images"])
                    {
                        var id = Guid.Parse(image["Id"]);
                        var existing = GTSDataRepository.GetById<RemarkImage>(id);
                        if (existing != null)
                        {
                            existing.ImagePath = image["ImagePath"];
                            existing.Description = image["Description"];
                            existing.EditedBy = User.Identity.Name;
                        }
                        else
                        {
                            existing = new RemarkImage
                            {
                                Id = id,
                                ImagePath = image["ImagePath"],
                                Description = image["Description"],
                                EditedBy = User.Identity.Name
                            };
                        }

                        if (existing.IsValid())
                        {
                            GTSDataRepository.Update<RemarkImage>(existing);
                        }
                        else
                        {
                            remark.Resolved = false;
                            throw new InvalidOperationException(remark.ValidationResults.ToList().FirstOrDefault().ErrorMessage);
                        }
                        images.Add(existing);
                    }
                }
                remark.RemarkImages = images.ToArray();

                var remarkTypeId = remarkCollection["RemarkSymptomType"]["Id"] == Guid.Empty.ToString() ? Guid.NewGuid() : Guid.Parse(remarkCollection["RemarkSymptomType"]["Id"]);
                remark.RemarkSymptomType = GTSDataRepository.GetById<RemarkSymptomType>(remarkTypeId);

                // CAUSES
                foreach (var cause in remark.RemarkSymptomCauses.OrderBy(e => e.CauseDate))
                {
                    //var causeFormCollection = remarkCollection["RemarkCauses"].Where(x => x["Id"] == cause.Id).FirstOrDefault();
                    foreach (var causeFormCollectionEntry in remarkCollection["RemarkSymptomCauses"])
                    {
                        var causeId = causeFormCollectionEntry["Id"] == Guid.Empty.ToString() ? Guid.NewGuid() : Guid.Parse(causeFormCollectionEntry["Id"]);
                        if (cause.Id == causeId)
                        {
                            cause.CauseDate = DateTime.Parse(causeFormCollectionEntry["CauseDate"]);
                            var causeTypeId = causeFormCollectionEntry["CauseType"]["Id"] == Guid.Empty.ToString() ? Guid.NewGuid() : Guid.Parse(causeFormCollectionEntry["CauseType"]["Id"]);
                            cause.CauseType = GTSDataRepository.GetById<RemarkSymptomCauseType>(causeTypeId);
                            cause.EditedBy = User.Identity.Name;
                            cause.MaterialCost = causeFormCollectionEntry["MaterialCost"];
                            cause.TimeSpent = causeFormCollectionEntry["TimeSpent"];

                            images = new List<RemarkImage>();
                            if (((Dictionary<string, object>)causeFormCollectionEntry).Keys.Contains("Images"))
                            {
                                foreach (var image in causeFormCollectionEntry["Images"])
                                {
                                    var id = Guid.Parse(image["Id"]);
                                    var existing = GTSDataRepository.GetById<RemarkImage>(id);
                                    if (existing != null)
                                    {
                                        existing.ImagePath = image["ImagePath"];
                                        existing.Description = image["Description"];
                                        existing.EditedBy = User.Identity.Name;
                                    }
                                    else
                                    {
                                        existing = new RemarkImage
                                        {
                                            Id = id,
                                            ImagePath = image["ImagePath"],
                                            Description = image["Description"],
                                            EditedBy = User.Identity.Name
                                        };
                                    }

                                    if (existing.IsValid())
                                    {
                                        GTSDataRepository.Update<RemarkImage>(existing);
                                    }
                                    else
                                    {
                                        remark.Resolved = false;
                                        throw new InvalidOperationException(remark.ValidationResults.ToList().FirstOrDefault().ErrorMessage);
                                    }

                                    images.Add(existing);
                                }
                            }
                            cause.RemarkImages = images.ToArray();

                            // component assembly selection is optional
                            if (((Dictionary<string, object>)causeFormCollectionEntry).Keys.Contains("ComponentAssembly"))
                            {
                                var componentAssemblyId = causeFormCollectionEntry["ComponentAssembly"].GetType() == typeof(string) ? Guid.Empty : Guid.Parse(causeFormCollectionEntry["ComponentAssembly"]["Id"]);
                                ComponentAssembly component = GTSDataRepository.GetById<ComponentAssembly>(componentAssemblyId);
                                cause.ComponentAssembly = component;

                                // when a component assembly that refers to another product is the cause of a remark, we need to put the underlying reference in remark as well

                                // get type
                                var referenceRemarkSymptomType = GTSDataRepository.GetListByQuery<RemarkSymptomType>("FROM RemarkSymptomType rst WHERE rst.Name = 'Prevents parent assembly'").FirstOrDefault();

                                // get underlying assembly
                                var underLyingAssemblyReference = GTSDataRepository.GetListByQuery<ProductAssembly>("FROM ProductAssembly pa WHERE pa.ProductSerial = :serial", new Dictionary<string, object> { { "serial", component.Serial } }).FirstOrDefault();

                                // if the underlying assembly does not contain an unresolved remark of type "Prevents parent assembly", create one its put in remark as well. We do this check to prevent additional
                                // remark creation when the current remarkt gets updates / saved again
                                if (underLyingAssemblyReference != null && underLyingAssemblyReference.RemarkSymptoms.Where(x => x.RemarkSymptomType == referenceRemarkSymptomType && x.Resolved == false).FirstOrDefault() == null)
                                {
                                    var newRemark = new RemarkSymptom
                                    {
                                        RemarkSymptomType = referenceRemarkSymptomType,
                                        Id = Guid.NewGuid(),
                                        CreationDate = DateTime.Now,
                                        Description = "Automatically put in remark by parent assembly: " + underLyingAssemblyReference.ProductSerial,
                                        EditedBy = User.Identity.Name,
                                        ProductAssembly = underLyingAssemblyReference,
                                    };
                                    GTSDataRepository.Create<RemarkSymptom>(newRemark);
                                }
                            }
                            else
                                cause.ComponentAssembly = null;

                            cause.Description = causeFormCollectionEntry["Description"];

                            // check for an existing solution
                            if (((Dictionary<string, object>)causeFormCollectionEntry).Keys.Contains("RemarkSymptomSolution") && causeFormCollectionEntry["RemarkSymptomSolution"] != null)
                            {
                                var solutionFormCollectionEntry = causeFormCollectionEntry["RemarkSymptomSolution"];
                                cause.RemarkSymptomSolution.Description = solutionFormCollectionEntry["Description"];
                                var solutionTypeId = solutionFormCollectionEntry["RemarkSymptomSolutionType"].GetType() == typeof(string) ? Guid.Empty : Guid.Parse(solutionFormCollectionEntry["RemarkSymptomSolutionType"]["Id"]);
                                cause.RemarkSymptomSolution.RemarkSymptomSolutionType = GTSDataRepository.GetById<RemarkSymptomSolutionType>(solutionTypeId);

                                // for solutions, component assembly selection is optional as well
                                if (((Dictionary<string, object>)solutionFormCollectionEntry).Keys.Contains("ComponentAssembly"))
                                {
                                    var solutionComponentAssemblyId = solutionFormCollectionEntry["ComponentAssembly"].GetType() == typeof(string) ? Guid.Empty : Guid.Parse(solutionFormCollectionEntry["ComponentAssembly"]["Id"]);
                                    cause.RemarkSymptomSolution.ComponentAssembly = GTSDataRepository.GetById<ComponentAssembly>(solutionComponentAssemblyId);

                                    // check for an existing component assembly serial
                                    if (((Dictionary<string, object>)solutionFormCollectionEntry).Keys.Contains("ComponentSerial"))
                                    {
                                        cause.RemarkSymptomSolution.ComponentSerial = solutionFormCollectionEntry["ComponentSerial"];
                                        if (((Dictionary<string, object>)solutionFormCollectionEntry).Keys.Contains("PreviousComponentSerial"))
                                            cause.RemarkSymptomSolution.PreviousComponentSerial = solutionFormCollectionEntry["PreviousComponentSerial"];
                                    }
                                }
                                else
                                    cause.RemarkSymptomSolution.ComponentAssembly = null;

                                images = new List<RemarkImage>();
                                if (((Dictionary<string, object>)solutionFormCollectionEntry).Keys.Contains("Images"))
                                {
                                    foreach (var image in solutionFormCollectionEntry["Images"])
                                    {
                                        var id = Guid.Parse(image["Id"]);
                                        var existing = GTSDataRepository.GetById<RemarkImage>(id);
                                        if (existing != null)
                                        {
                                            existing.ImagePath = image["ImagePath"];
                                            existing.Description = image["Description"];
                                            existing.EditedBy = User.Identity.Name;
                                        }
                                        else
                                        {
                                            existing = new RemarkImage
                                            {
                                                Id = id,
                                                ImagePath = image["ImagePath"],
                                                Description = image["Description"],
                                                EditedBy = User.Identity.Name
                                            };
                                        }

                                        if (existing.IsValid())
                                        {
                                            GTSDataRepository.Update<RemarkImage>(existing);
                                        }
                                        else
                                        {
                                            remark.Resolved = false;
                                            throw new InvalidOperationException(remark.ValidationResults.ToList().FirstOrDefault().ErrorMessage);
                                        }

                                        images.Add(existing);
                                    }
                                }
                                cause.RemarkSymptomSolution.RemarkImages = images.ToArray();

                                cause.RemarkSymptomSolution.SolutionDate = DateTime.Parse(solutionFormCollectionEntry["SolutionDate"]);
                                cause.RemarkSymptomSolution.Successful = ((Dictionary<string, object>)solutionFormCollectionEntry).Keys.Contains("Successful") ? (bool)solutionFormCollectionEntry["Successful"] : false;
                                cause.RemarkSymptomSolution.EditedBy = User.Identity.Name;
                                cause.RemarkSymptomSolution.MaterialCost = solutionFormCollectionEntry["MaterialCost"];
                                cause.RemarkSymptomSolution.TimeSpent = solutionFormCollectionEntry["TimeSpent"];
                            }

                            if (cause.RemarkSymptom.IsValid())
                            {
                                GTSDataRepository.Update<RemarkSymptomCause>(cause);

                                // check for an existing solution
                                if (cause.RemarkSymptomSolution != null)
                                    GTSDataRepository.Update<RemarkSymptomSolution>(cause.RemarkSymptomSolution);
                            }
                            else
                            {
                                remark.Resolved = false;
                                throw new InvalidOperationException(remark.ValidationResults.ToList().FirstOrDefault().ErrorMessage);
                            }
                        }
                    }
                }
                if (remark.IsValid())
                {
                    // DEBUGGING
                    var causes = remark.RemarkSymptomCauses.OrderBy(e => e.CauseDate);
                    var solutions = causes.Select(e => e.RemarkSymptomSolution);
                    var replaceSolutions = solutions.Where(a => a != null && (a.RemarkSymptomSolutionType.Name.Equals("Replace") || a.RemarkSymptomSolutionType.Name.ToLower() == "salvaged")).ToArray();

                    // update assemblies for replaced parts
                    /*var replaceSolutions = remark.RemarkSymptomCauses.OrderBy(e => e.RemarkSymptomSolution.SolutionDate).Select(e => e.RemarkSymptomSolution)
                        .Where(a => a != null && (a.RemarkSymptomSolutionType.Name.Equals("Replace") || a.RemarkSymptomSolutionType.Name.ToLower() == "salvaged")).ToArray();*/
                    foreach (var solution in replaceSolutions)
                    {
                        if (solution.ComponentAssembly == null)
                            continue;

                        var assembly = GTSDataRepository
                            .GetListByQuery<ComponentAssembly>(
                                string.Format("FROM {0} pms WHERE pms.Id LIKE '{1}'", typeof(ComponentAssembly).Name, solution.ComponentAssembly.Id))
                            .FirstOrDefault();
                        if (assembly != null)
                        {
                            // if the solution type is "Salvaged" then we need to clear the serial from this component assembly
                            // if the solution type is "Replace" then we need to update the component assembly with the correct serial
                            assembly.Serial = solution.RemarkSymptomSolutionType.Name.ToLower() == "salvaged" ? string.Empty : solution.ComponentSerial;
                            assembly.EditedBy = User.Identity.Name;

                            if (assembly.IsValid())
                            {
                                GTSDataRepository.Update<ComponentAssembly>(assembly);
                            }
                            else
                            {
                                throw new InvalidOperationException(assembly.ValidationResults.ToList().FirstOrDefault().ErrorMessage);
                            }
                        }
                    }

                    GTSDataRepository.Update<RemarkSymptom>(remark);
                }
                else
                {
                    remark.Resolved = false;
                    throw new InvalidOperationException(remark.ValidationResults.ToList().FirstOrDefault().ErrorMessage);
                }
            }
            return new HttpStatusCodeResult(200);
        }

        /// <summary>
        /// Returns the test remark view
        /// </summary>
        /// <returns></returns>
        public ActionResult TestRemark()
        {
            return View();
        }

        /// <summary>
        /// Gets the remarks for a given assembly.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private object GetRemarksForAssembly(Guid id, bool includeArchived = false)
        {
            _logger.LogInfo("User {0} attempts to retrieve Remark Symptoms for assembly with id {1}", User.Identity.Name, id);

            var type = typeof(RemarkSymptom);

            List<RemarkSymptom> remarks = null;
            if (includeArchived)
                remarks = GTSDataRepository.GetListByQuery<RemarkSymptom>("FROM RemarkSymptom rs WHERE rs.ProductAssembly.Id = :id", new Dictionary<string, object>() { { "id", id } })
                    .OrderByDescending(x => x.CreationDate).ToList();
            else
                remarks = GTSDataRepository.GetListByQuery<RemarkSymptom>(string.Format("FROM {0} rs WHERE rs.ProductAssembly.Id = :id AND rs.IsArchived = false", type),
                    new Dictionary<string, object>() { { "id", id } }).OrderByDescending(x => x.CreationDate).ToList();

            var simplifiedList = remarks.Select(remarkSymptom => new
            {
                Id = remarkSymptom.Id,
                Description = remarkSymptom.Description,
                ProductSerial = remarkSymptom.ProductAssembly.ProductSerial,
                ProductName = remarkSymptom.ProductAssembly.ProductModel.Name,
                ProductAssemblyId = remarkSymptom.ProductAssembly.Id,
                CreationDate = remarkSymptom.CreationDate,
                IsArchived = remarkSymptom.IsArchived,
                CreationDateAsString = remarkSymptom.CreationDate.ToString("dd/MM/yyyy hh:mm"),
                Images = remarkSymptom.RemarkImages?.ToArray() ?? new RemarkImage[0],
                RemarkSymptomCauses = remarkSymptom.RemarkSymptomCauses == null ? null : remarkSymptom.RemarkSymptomCauses.Where(cause => cause.IsArchived == false).Select(remarkCause => new
                {
                    Id = remarkCause.Id,
                    Description = remarkCause.Description,
                    CauseType = remarkCause.CauseType,
                    CauseDate = remarkCause.CauseDate,
                    CauseDateAsString = remarkCause.CauseDate.ToString("dd/MM/yyyy hh:mm"),
                    MaterialCost = remarkCause.MaterialCost,
                    TimeSpent = remarkCause.TimeSpent,
                    ComponentAssembly = remarkCause.ComponentAssembly != null ? new { Id = remarkCause.ComponentAssembly.Id, ComponentName = remarkCause.ComponentAssembly.ProductComponent.ComponentName } : null,
                    Images = remarkCause.RemarkImages?.ToArray() ?? new RemarkImage[0],
                    RemarkSymptomSolution = (remarkCause.RemarkSymptomSolution == null || remarkCause.RemarkSymptomSolution.IsArchived == true) ? null : new
                    {
                        Id = remarkCause.RemarkSymptomSolution.Id,
                        Successful = remarkCause.RemarkSymptomSolution.Successful,
                        RemarkSymptomSolutionType = remarkCause.RemarkSymptomSolution.RemarkSymptomSolutionType,
                        SolutionDate = remarkCause.RemarkSymptomSolution.SolutionDate,
                        SolutionDateAsString = remarkCause.RemarkSymptomSolution.SolutionDate.ToString("dd/MM/yyyy hh:mm"),
                        ComponentAssembly = remarkCause.RemarkSymptomSolution.ComponentAssembly != null ? new { Id = remarkCause.RemarkSymptomSolution.ComponentAssembly.Id, ComponentName = remarkCause.RemarkSymptomSolution.ComponentAssembly.ProductComponent.ComponentName } : null,
                        PreviousComponentSerial = remarkCause.RemarkSymptomSolution.PreviousComponentSerial, //remarkCause.ComponentAssembly != null ? remarkCause.ComponentAssembly.Serial.ToString() : string.Empty,
                        ComponentSerial = remarkCause.RemarkSymptomSolution.ComponentSerial,
                        Description = remarkCause.RemarkSymptomSolution.Description,
                        MaterialCost = remarkCause.RemarkSymptomSolution.MaterialCost,
                        TimeSpent = remarkCause.RemarkSymptomSolution.TimeSpent,
                        Images = remarkCause.RemarkSymptomSolution.RemarkImages?.ToArray() ?? new RemarkImage[0]
                    }
                }),
                RemarkSymptomType = remarkSymptom.RemarkSymptomType,
                ResolutionDate = remarkSymptom.ResolutionDate,
                // resolution date can be a null value or a DateTime.MaxValue. In both cases we do not display it
                ResolutionDateAsString = (remarkSymptom.ResolutionDate == null) ? "Not set" : remarkSymptom.ResolutionDate.Value.ToShortDateString() == "31/12/9999" ? "Not set" : remarkSymptom.ResolutionDate.Value.ToString("dd/MM/yyyy hh:mm"),
                Resolved = remarkSymptom.Resolved,
                ComponentAssemblies = remarkSymptom.ProductAssembly.ComponentAssemblies.Select(z => new { Id = z.Id, Name = z.ProductComponent.ComponentName, ComponentSerial = z.Serial })
            });

            return simplifiedList;
        }

        /*
        /// <summary>
        /// TESTING Creates test remarks  TESTING
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private List<RemarkSymptom> CreateTestRemarks()
        {
            var remarkSolution01 = new RemarkSymptomSolution
            {
                Id = Guid.NewGuid(),
                Description = "Replace broken wingtips",
                SolutionDate = DateTime.Now,
                Successful = true,
                RemarkSymptomSolutionType = GTSDataRepository.GetListByQuery<RemarkSymptomSolutionType>("FROM RemarkSymptomSolutionType WHERE Name = 'Replace'").FirstOrDefault(),
                //CostFactors = new Dictionary<object, string> { { 0, "" } }
            };

            var remarkCause01 = new RemarkSymptomCause
            {
                Id = Guid.NewGuid(),
                CauseDate = DateTime.Now,
                CauseType = GTSDataRepository.GetListByQuery<RemarkSymptomCauseType>("FROM RemarkSymptomCauseType WHERE Name = 'Testing Error'").FirstOrDefault(),
                Description = "Hard impact when landing.",
                RemarkSymptomSolution = remarkSolution01,
                //CostFactors = new Dictionary<object, string> { { "0", "" } }
            };

            var remarkSymptom01 = new RemarkSymptom
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                Description = "Broken wingtips after landing",
                RemarkSymptomType = GTSDataRepository.GetListByQuery<RemarkSymptomType>("FROM RemarkSymptomType WHERE Name = 'Damaged'").FirstOrDefault(),
                RemarkSymptomCauses = new List<RemarkSymptomCause>
                    {
                        remarkCause01
                    },
            };

            //remarkCause01.RemarkSymptom = remarkSymptom01;

            var remarkList = new List<RemarkSymptom>
                {
                    remarkSymptom01
                };

            return remarkList;
        }
        */

        #endregion Remark flow

        #region Remark Types

        /// <summary>
        /// Adds / edit routine to create / update remark symptom types
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddEditRemarkTypeJson(string objectAsString)
        {
            _logger.LogInfo("User {0} is adding / updating a remark symptom type ({1}).", User.Identity.Name, objectAsString);
            return AddEdit<RemarkSymptomType>(objectAsString);
        }

        /// <summary>
        /// Deletes the remark type.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult DeleteRemarkType(Guid id)
        {
            _logger.LogInfo("User {0} is deleting a remark symptom type with Id: {1}.", User.Identity.Name, id);
            return Delete<RemarkSymptomType>(id);
        }

        /// <summary>
        /// Return remark types as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetRemarkTypesJson()
        {
            _logger.LogInfo("User {0} requests a list of remark symptom types.", User.Identity.Name);
            return GetByType<RemarkSymptomType>();
        }

        #endregion Remark Types

        #region Remark Type Causes

        /// <summary>
        /// Adds / edit routine to create / update remark symptom types
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddEditRemarkCauseTypeJson(string objectAsString)
        {
            _logger.LogInfo("User {0} is adding / updating a remark cause type ({1}).", User.Identity.Name, objectAsString);
            return AddEdit<RemarkSymptomCauseType>(objectAsString);
        }

        /// <summary>
        /// Deletes the remark type.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult DeleteRemarkCauseType(Guid id)
        {
            _logger.LogInfo("User {0} is deleting a remark cause type with Id: {1}.", User.Identity.Name, id);
            return Delete<RemarkSymptomCauseType>(id);
        }

        /// <summary>
        /// Return remark types as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetRemarkCauseTypesJson()
        {
            _logger.LogInfo("User {0} requests a list of remark cause types.", User.Identity.Name);
            return GetByType<RemarkSymptomCauseType>();
        }

        #endregion Remark Type Causes

        #region Remark Type Solutions

        /// <summary>
        /// Adds / edit routine to create / update remark symptom types
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddEditRemarkSolutionTypeJson(string objectAsString)
        {
            _logger.LogInfo("User {0} is adding / updating a remark solution type ({1}).", User.Identity.Name, objectAsString);
            return AddEdit<RemarkSymptomSolutionType>(objectAsString);
        }

        /// <summary>
        /// Deletes the remark type.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult DeleteRemarkSolutionType(Guid id)
        {
            _logger.LogInfo("User {0} is deleting a remark solution type with Id: {1}.", User.Identity.Name, id);
            return Delete<RemarkSymptomSolutionType>(id);
        }

        /// <summary>
        /// Return remark types as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetRemarkSolutionTypesJson()
        {
            _logger.LogInfo("User {0} requests a list of remark solution types.", User.Identity.Name);
            return GetByType<RemarkSymptomSolutionType>();
        }

        #endregion Remark Type Solutions

        #region Model States

        /// <summary>
        /// Adds / edit routine to create / update product model states.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddEditProductModelStateJson(string objectAsString)
        {
            _logger.LogInfo("User {0} is adding / updating a product model state ({1}).", User.Identity.Name, objectAsString);
            return AddEdit<ProductModelState>(objectAsString);
        }

        /// <summary>
        /// Deletes the product model state .
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult DeleteProductModelState(Guid id)
        {
            _logger.LogInfo("User {0} is deleting a product model state with Id: {1}.", User.Identity.Name, id);
            return Delete<ProductModelState>(id);
        }

        /// <summary>
        /// Return model states as a json string.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetProductModelStatesForRemarksJson()
        {
            _logger.LogInfo("User {0} requests a list of product model states.", User.Identity.Name);
            return GetByType<ProductModelState>();
        }

        /*
        /// <summary>
        /// Gets the states as string list.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetStatesForComponentsJson()
        {
            _logger.LogInfo("User {0} requests an autocomplete element list of product model states.", User.Identity.Name);
            var results = GTSDataRepository.GetAll<ProductModelState>().Select(x => new { Id = x.Id, Name = x.Name }).ToList();
            return Json(results, JsonRequestBehavior.AllowGet);
        }*/

        #endregion Model States

        public ActionResult PrintRemarks(Guid id)
        {
            _logger.LogInfo("User {0} is attempting to open the page to print remarks for product with id {1}.", User.Identity.Name, id);
            return View();
        }

        public ActionResult PrintRemarksJSON(Guid id)
        {
            _logger.LogInfo("User {0} is attempting to retrieve (for print) remarks for product with id {1}.", User.Identity.Name, id);
            try
            {
                var product = GTSDataRepository.GetById<ProductAssembly>(id);

                var result = new
                {
                    Name = product.ProductModel.Name,
                    ProductSerial = product.ProductSerial,
                    Remarks = product.RemarkSymptoms
                        .Where(x => x.IsArchived == false && x.Resolved == false)
                        .Select(x => new
                        {
                            Title = x.RemarkSymptomType.Name,
                            Description = x.Description,
                            User = x.EditedBy.Contains("@") ? x.EditedBy.Split('@')[0] : x.EditedBy,
                            CreationDate = x.CreationDate.ToString("yyyy-MM-dd")
                        }).ToList()
                };

                return SerializeForWeb(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public ActionResult PrintSetupHelp()
        {
            return View();
        }

        /// <summary>
        /// Uploads an image.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UploadImages()
        {
            //  Get all files from Request object
            HttpFileCollectionBase files = Request.Files;

            if (files.Count <= 0)
                return new HttpStatusCodeResult(200, "No files selected to upload.");

            _logger.LogInfo("User {0} is attempting to upload files to server.", User.Identity.Name);
            try
            {
                var images = new List<RemarkImage>();
                for (var i = 0; i < files.Count; i++)
                {
                    HttpPostedFileBase file = files[i];
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
                    imagePath = Path.Combine(Server.MapPath("~/Content/Images/Remarks"), imagePath);

                    _logger.LogInfo("User {0} attempts to upload an image with a name that already exists to server, renaming file.", User.Identity.Name);
                    FileInfo fileInfo = new FileInfo(imagePath);
                    var fileName = string.Format("{0}_1{1}", Path.GetFileNameWithoutExtension(fileInfo.Name), fileInfo.Extension);
                    images.Add(new RemarkImage
                    {
                        ImagePath = fileName,
                        Id = Guid.NewGuid()
                    });

                    imagePath = Path.Combine(Server.MapPath("~/Content/Images/Remarks"), fileName);

                    //return Json(new SimpleActionResult(false, "File " + file.FileName + " already exists. Please rename it or select another."));

                    file.SaveAs(imagePath);
                }

                _logger.LogInfo("user {0} has uploaded {1} images", User.Identity.Name, images.Count);
                return Json(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Add / edit routine.
        /// </summary>
        /// <param name="objectAsString">The object as string.</param>
        /// <returns></returns>
        private ActionResult AddEdit<T>(string objectAsString) where T : DomainObject
        {
            try
            {
                var type = typeof(T);

                var formCollection = new JavaScriptSerializer().Deserialize<dynamic>(objectAsString);

                // determine ID and try to retrieve existing item
                var id = formCollection["Id"] == Guid.Empty.ToString() || formCollection["Id"] == string.Empty ? Guid.NewGuid() : Guid.Parse(formCollection["Id"]);

                // init enity
                var entity = GTSDataRepository.GetById<T>(id);
                var newEntity = false;
                if (entity == null)
                {
                    entity = (T)Activator.CreateInstance(typeof(T));
                    entity.Id = Guid.NewGuid();
                    newEntity = true;
                }
                entity.Name = formCollection["Name"];

                entity.Description = ((Dictionary<string, object>)formCollection).Keys.Contains("Description") ? formCollection["Description"] : string.Empty;

                if (type == typeof(ProductModelState))
                    entity.PreventBatchMode = ((Dictionary<string, object>)formCollection).Keys.Contains("PreventBatchMode") ? formCollection["PreventBatchMode"] : false;

                var propertyInfo = typeof(T).GetProperty("EditedBy");
                if (propertyInfo != null)
                    propertyInfo.SetValue(entity, User.Identity.Name);

                var existingEntitiesWithSameName = GTSDataRepository.GetListByQuery<T>("FROM " + type.Name + " type WHERE type.IsArchived = false AND type.Name = :name AND type.Id != :id",
                    new Dictionary<string, object> { { "name", entity.Name }, { "id", entity.Id } })
                    .ToList();

                if (existingEntitiesWithSameName.Count > 0)
                    throw new InvalidOperationException("You cannot create a type or state with the same name as another.");

                if (newEntity)
                    GTSDataRepository.Create<T>(entity);
                else
                    GTSDataRepository.Update<T>(entity);

                return new HttpStatusCodeResult(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Deletes an instance
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private ActionResult Delete<T>(Guid id) where T : ArchivableDomainObject
        {
            var type = typeof(T);
            try
            {
                GTSDataRepository.ArchiveById<T>(id, User.Identity.Name);

                return new HttpStatusCodeResult(200, type + " logically deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves an instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private ActionResult GetByType<T>() where T : DomainObject
        {
            var type = typeof(T);
            var result = GTSDataRepository.GetListByQuery<T>(string.Format("FROM {0} pms WHERE pms.IsArchived = false ORDER BY pms.Name", type))
                .Select(x =>
                    new
                    {
                        Name = x.GetType().GetProperty("Name", typeof(string)) != null ? x.GetType().GetProperty("Name", typeof(string)).GetValue(x) : "",
                        Description = x.GetType().GetProperty("Description", typeof(string)) != null ? x.GetType().GetProperty("Description", typeof(string)).GetValue(x) : "",
                        Id = x.Id,
                        PreventBatchMode = x.GetType().GetProperty("PreventBatchMode", typeof(bool)) != null ? x.GetType().GetProperty("PreventBatchMode", typeof(bool)).GetValue(x) : false,
                    }
                ).ToList();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
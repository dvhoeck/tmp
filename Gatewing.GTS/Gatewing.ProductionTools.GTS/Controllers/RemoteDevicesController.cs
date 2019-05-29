using Delair.ProductionTools.Crypto;
using Gatewing.ProductionTools.BLL;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

/// <summary>
///
/// </summary>
namespace Gatewing.ProductionTools.GTS.Controllers
{
    public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
            base.OnActionExecuting(filterContext);
        }
    }

    [AllowAnonymous]
    [AllowCrossSiteJson]
    public class RemoteDevicesController : BaseController
    {
        [Authorize]
        public ActionResult GetDevices()
        {
            _logger.LogDebug("User attempts to request a list of remote devices");

            try
            {
                var devices = GTSDataRepository.GetAll<RemoteDevice>();

                return SerializeForWeb(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult OpenDevice()
        {
            _logger.LogDebug("User attempts to open a remote device");
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        [AllowCrossSiteJson]
        public ActionResult SerialExists(string serial, string modelNameSearchString)
        {
            _logger.LogInfo("A remote device attempts to check if a serial {0} exist for a model with {1} in its name.", serial, modelNameSearchString);

            try
            {
                // try to retrieve an assembly for the serial parameter
                var assemblySearchCriteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>();
                assemblySearchCriteria = assemblySearchCriteria.Add(Expression.Eq("ProductSerial", serial));
                var hits = assemblySearchCriteria.List();

                foreach (ProductAssembly hit in hits)
                {
                    // check for search string in model name
                    if (hit.GetType().ToString().ToLower().Contains(modelNameSearchString.ToLower()))
                    {
                        if (hit.RemarkSymptoms.Where(symptom => symptom.Resolved == false && symptom.IsArchived == false).Count() > 0)
                            return new HttpStatusCodeResult(500, "Wing is in remark");

                        return new HttpStatusCodeResult(200, "Serial found");
                    }
                }

                return new HttpStatusCodeResult(500, "Serial not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [AllowCrossSiteJson]
        public ActionResult UpdateAssembly(string objectAsString)
        {
            var postData = (Dictionary<string, object>)new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(objectAsString);

            var crypto = new CryptoHandler();

            var result = crypto.DecryptJsonDataString(postData["data"].ToString(), postData["key"].ToString());

            return UpdateAssemblyParsed(result);
        }

        [AllowAnonymous]
        public ActionResult UpdateDevice(Guid key, string ip)
        {
            _logger.LogDebug("Device attempts to update its address {0} for device key {1}.", ip, key);
            try
            {
                if (string.IsNullOrEmpty(ip))
                    throw new ArgumentNullException("You must provide a valid IP address to this update routine.");

                var deviceCriteria = UnitOfWorkSession.CreateCriteria<RemoteDevice>();
                deviceCriteria.Add(Expression.Eq("DeviceKey", key));

                var devices = deviceCriteria.List<RemoteDevice>();

                if (devices.Count() == 1)
                {
                    devices[0].LastOnline = DateTime.Now;
                    devices[0].Address = ip;
                    GTSDataRepository.Update<RemoteDevice>(devices[0]);
                    _logger.LogInfo("Device address update for {0} is succesful: address {1}", devices[0].Name, devices[0].Address);
                }
                else
                    throw new InvalidOperationException("Device not found or devices found with duplicate keys.");

                return new HttpStatusCodeResult(200);
            }
            catch (Exception)
            {
                _logger.LogError("Device adress update for key: {0} and address {1} failed.");
                throw;
            }
        }

        /// <summary>
        /// Updates an assembly.
        /// </summary>
        /// <param name="objectAsString">The object as a json string.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// No product serial was found in incoming device data.
        /// or
        /// Could not find product with serial: " + postData["productSerial"]
        /// or
        /// Data is corrupted: the system found more than one product with serial " + postData["productSerial"]
        /// or
        /// Could not find component assemblies for product with serial: " + postData["productSerial"]
        /// </exception>
        private ActionResult UpdateAssemblyParsed(string objectAsString)
        {
            _logger.LogInfo("Incoming data from a remote device: {0}", objectAsString);

            try
            {
                var postData = (Dictionary<string, object>)new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(objectAsString);

                if (postData["productSerial"] == null || string.IsNullOrEmpty(postData["productSerial"].ToString()))
                    throw new InvalidOperationException("No product serial was found in incoming device data.");

                var keyWords = postData.Keys.ToList().Where(x => x.ToString() != "productSerial" && x.ToString() != "baseModelId" && x.ToString() != "deviceKey").ToArray<string>();

                var doCreateRemark = false;
                if (postData["createRemark"].ToString().ToLower() == "true")
                    doCreateRemark = true;

                // check if product exists
                var productAssemblyCriteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>();
                productAssemblyCriteria = productAssemblyCriteria.Add(Expression.Eq("ProductSerial", postData["productSerial"]));
                var productAssembly = productAssemblyCriteria.List<ProductAssembly>();
                if (productAssembly.Count() == 0)
                    throw new InvalidOperationException("Could not find product with serial: " + postData["productSerial"]);
                if (productAssembly.Count() > 1)
                    throw new InvalidOperationException("Data is corrupted: the system found more than one product with serial " + postData["productSerial"]);

                var componentAssemblyCriteria = UnitOfWorkSession.CreateCriteria<ComponentAssembly>();
                componentAssemblyCriteria = componentAssemblyCriteria.CreateAlias("ProductAssembly", "pa");
                componentAssemblyCriteria = componentAssemblyCriteria.Add(Expression.Eq("pa.ProductSerial", postData["productSerial"]));
                componentAssemblyCriteria = componentAssemblyCriteria.CreateAlias("ProductComponent", "pc");
                componentAssemblyCriteria = componentAssemblyCriteria.Add(Expression.In("pc.DeviceKeyword", keyWords));
                componentAssemblyCriteria = componentAssemblyCriteria.CreateAlias("pc.ProductModel", "pm");
                componentAssemblyCriteria = componentAssemblyCriteria.Add(Expression.Eq("pm.IsReleased", true));
                //componentAssemblyCriteria = componentAssemblyCriteria.Add(Expression.Eq("pm.IsArchived", false));
                if (postData.Keys.Contains<string>("baseModelId"))
                    componentAssemblyCriteria = componentAssemblyCriteria.Add(Expression.Eq("pm.BaseModelId", Guid.Parse(postData["baseModelId"].ToString())));

                var filteredComponentAssemblies = componentAssemblyCriteria.List<ComponentAssembly>();

                if (filteredComponentAssemblies.Count() == 0)
                    throw new InvalidOperationException("Could not find component assemblies for product with serial: " + postData["productSerial"]);

                // find matching component assemblies for any device keywords in the post data
                keyWords.ToList().ForEach(x =>
                {
                    filteredComponentAssemblies.Where(y => y.ProductComponent.DeviceKeyword == x).ToList().ForEach(z =>
                    {
                        // update component assembly
                        if (postData[x] != null)
                        {
                            z.Revision = postData[x].ToString();
                            GTSDataRepository.Update<ComponentAssembly>(z);
                        }
                    });
                });

                // create remark when needed
                if (doCreateRemark)
                {
                    var remarkMessage = string.Format("Product with serial {0} failed its automated tests. Reasons given: ", postData["productSerial"].ToString());

                    if (productAssembly[0].ProductModel.Name.ToLower().Contains("wing"))
                    {
                        if (postData["deviceApproval"] != null && postData["deviceApproval"].ToString() == "no")
                        {
                            remarkMessage += "Product found to be invalid by wing checker tool, ";
                            if (postData["errorMessage"] != null)
                                remarkMessage += postData["errorMessage"].ToString();
                        }
                        if (postData["minmax"] != null && postData["minmax"].ToString() == "no")
                            remarkMessage += "User did not approve min and / or max, ";
                    }
                    else if (productAssembly[0].ProductModel.Name.ToLower().Contains("ux11 motor"))
                    {
                        if (!(bool)postData["userApproval"])
                            remarkMessage += "User disapproves (" + postData["disapprovalReason"].ToString() + "), ";
                        if (!(bool)postData["systemApprovalVoltage"])
                            remarkMessage += "Measured voltage does not conform to expectations, ";
                        if (!(bool)postData["systemApprovalCurrent"])
                            remarkMessage += "Measured current does not conform to expectations, ";
                        if (!(bool)postData["systemApprovalRPM"])
                            remarkMessage += "Measured rpm does not conform to expectations, ";
                        if (!(bool)postData["systemApprovalDirection"])
                            remarkMessage += "Measured direction of spin does not conform to expectations, ";
                    }

                    var newRemark = new RemarkSymptom
                    {
                        Id = Guid.NewGuid(),
                        CreationDate = DateTime.Now,
                        ResolutionDate = DateTime.MaxValue,
                        Description = remarkMessage,
                        ProductAssembly = productAssembly[0],
                        EditedBy = postData["user"].ToString(),
                        RemarkSymptomType = GTSDataRepository.GetListByQuery<RemarkSymptomType>("FROM RemarkSymptomType WHERE Name = '" + postData["remarkSymptomType"].ToString() + "'").FirstOrDefault(),
                    };

                    GTSDataRepository.Create<RemarkSymptom>(newRemark);
                }

                return new HttpStatusCodeResult(200, "Data received");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);

                return new HttpStatusCodeResult(500, ex.Message);
            }
        }
    }
}
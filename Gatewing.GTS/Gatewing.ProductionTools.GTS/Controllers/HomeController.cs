using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.GTS.Models;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Gatewing.ProductionTools.GTS.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult About()
        {
            _logger.LogInfo("User {0} requests About page.", User != null ? User.Identity.Name : "");

            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            _logger.LogInfo("User {0} requests Contact page.", User != null ? User.Identity.Name : "");

            ViewBag.Message = "Your contact page.";

            return View();
        }

        /// <summary>
        /// Gets GTS stats.
        /// </summary>
        /// <returns></returns>
        public ActionResult GetGtsStats()
        {
            try
            {
                //var userList = new List<ViewUser>();

                var tmmm = this;

                //var tmo = GTSDataRepository;

                // count users
                var userCount = new ApplicationDbContext().Users.Count();

                // count released models
                var releaseModelCount = GTSDataRepository.ExecuteQuery("SELECT Count(Id) FROM ProductModel pm WHERE pm.IsArchived = 0 AND pm.IsReleased = 1", null, true);

                // count assemblies
                var assemblyCount = GTSDataRepository.ExecuteQuery("SELECT Count(pa.Id) FROM ProductAssembly pa INNER JOIN ProductModelState pms on pa.ProductModelState_id = pms.Id WHERE pms.Name = 'Delivered'", null, true);

                // count remarks
                var unresolvedRemarkCount = GTSDataRepository.ExecuteQuery("SELECT Count(Id) FROM RemarkSymptom rs WHERE rs.IsArchived = 0 AND rs.Resolved = 0", null, true);

                // count items in the list and aggegrate them
                var result = new
                {
                    modelCount = releaseModelCount,
                    assemblyCount = assemblyCount,
                    remarkCount = unresolvedRemarkCount,
                    userCount = userCount
                };

                return SerializeForWeb(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public ActionResult Index()
        {
            _logger.LogInfo("User {0} requests Home page.", User != null ? User.Identity.Name : "");
            return View();
        }

        public ActionResult Index2()
        {
            _logger.LogInfo("User {0} requests Home page.", User != null ? User.Identity.Name : "");
            return View();
        }

        public ActionResult Test()
        {
            /*
            // iterate over all assemblies with 100% completion and set them to delivered
            var criteria = UnitOfWorkSession.CreateCriteria<ProductAssembly>()
                .CreateAlias("ProductModelState", "pms")
                .CreateAlias("ProductModel", "pm")
                .Add(Expression.Not(Expression.Eq("pms.Name", "Delivered")))
                .Add(Expression.Eq("pm.ModelType", ModelType.Standard));

            var results = criteria.List<ProductAssembly>();

            results = results.Where(x => x.Progress == 100 && x.RemarkSymptoms.Where(y => y.IsArchived == false || y.Resolved == false).Count() == 0).ToList();

            var simpleDTOs = results.Select(x => new
            {
                ProductSerial = x.ProductSerial,
                ModelName = x.ProductModel.Name,
                State = x.ProductModelState.Name,
                Progress = x.Progress
            });

            return SerializeForWeb(simpleDTOs);
            */

            return View();
        }
    }
}
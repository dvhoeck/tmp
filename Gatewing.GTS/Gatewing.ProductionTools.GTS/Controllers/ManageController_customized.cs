using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using Gatewing.ProductionTools.GTS.Models;
using System.Collections.Generic;
using System.Data.Entity;

namespace Gatewing.ProductionTools.GTS.Controllers
{
    public partial class ManageController
    {
        [HttpGet]
        [Authorize]
        public ActionResult UserRolesAsync()
        {
            var users = new ApplicationDbContext().Users.ToList();
            var userList = new List<ViewUser>();
            foreach (var user in users)
            {
                userList.Add(new ViewUser { Name = user.UserName, Id = user.Id, IsAdmin = UserManager.IsInRole(user.Id, "Administrators") });
            }

            ViewBag.Users = userList;
            return Json(userList);
        }

        /// <summary>
        /// Removes the user
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult RemoveUserJson(Guid id)
        {
            try
            {
                var context = new ApplicationDbContext();
                var user = context.Users.Where(x => x.Id == id.ToString()).FirstOrDefault();
                context.Entry(user).State = EntityState.Deleted;
                context.SaveChanges();

                return new HttpStatusCodeResult(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        [HttpGet]
        [Authorize]
        public ActionResult GetUsersJson()
        {
            var users = new ApplicationDbContext().Users;
            var userList = new List<ViewUser>();
            foreach (var user in users)
            {
                userList.Add(new ViewUser { Name = user.UserName, Id = user.Id, IsAdmin = UserManager.IsInRole(user.Id, "Administrators") });
            }

            return Json(userList.OrderBy(user => user.Name), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [Authorize]
        public ActionResult AddUserToAdminRole(Guid Id)
        {
            UserManager.AddToRole(Id.ToString(), "Administrators");
            return RedirectToAction("UserRoles");
        }

        public ActionResult RemoveUserFromAdminRole(Guid Id)
        {
            UserManager.RemoveFromRole(Id.ToString(), "Administrators");
            return RedirectToAction("index");
        }

        public ActionResult Delete(Guid Id)
        {
            UserManager.Delete(UserManager.FindById(Id.ToString()));
            return RedirectToAction("index");
        }

        public ActionResult BarcodeConfiguration()
        {
            return View();
        }
    }
}
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Gatewing.ProductionTools.GTS.Models;
using System.Web.Script.Serialization;
using Gatewing.ProductionTools.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Gatewing.ProductionTools.DAL;
using Microsoft.AspNet.Identity.EntityFramework;
using Gatewing.ProductionTools.BLL;
using System.Net.Mail;
using System.Web.Security;
using NHibernate.Linq;

namespace Gatewing.ProductionTools.GTS.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private Logger _logger = new Logger("Account Controller");

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult ForgotPasswordLogin(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /*
        [HttpPost]
        [AllowAnonymous]
        public ActionResult AngularLogin(FormCollection formCollection)
        {
            //var user = new JavaScriptSerializer().Deserialize<JsonUser>(formCollection);

            return Login(formCollection["username"], formCollection["password"]);
        }*/

        [HttpPost]
        [AllowAnonymous]
        public ActionResult AngularLogin(string username, string password)
        {
            //var user = new JavaScriptSerializer().Deserialize<JsonUser>(formCollection);

            _logger.LogInfo("A login attempt was made with login: " + username);

            return Login(username, password);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            _logger.LogDebug("Attempting sign in.");
            var result = SignInManager.PasswordSignIn(email, password, false, false);
            _logger.LogDebug("Sign in complete, result: " + result);
            var feedback = -9;
            switch (result)
            {
                case SignInStatus.Success:
                    feedback = 1;
                    break;

                case SignInStatus.LockedOut:
                    feedback = 0;
                    break;

                case SignInStatus.RequiresVerification:
                    feedback = 2;
                    break;

                case SignInStatus.Failure:
                default:
                    feedback = -1;
                    break;
            }

            return Json(feedback, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login(string ReturnUrl)
        {
            return RedirectToAction("Index", "Home");
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            _logger.LogDebug("Attempting model state check.");
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            _logger.LogDebug("Attempting sign in.");
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            _logger.LogDebug("Sign in complete, result: " + result);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });

                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult RegisterJson(string objectAsString)
        {
            var jsonUser = JsonConvert.DeserializeObject<dynamic>(objectAsString);
            var user = new ApplicationUser { UserName = jsonUser.Email, Email = jsonUser.Email };
            //var user = new ApplicationUser { UserName = "Els De Reu", Email = "els.dereu@delair-tech.com" };
            var result = UserManager.CreateAsync(user, jsonUser.password.ToString());
            //var result = UserManager.CreateAsync(user, "pppppp");
            if (result.Result.Succeeded)
            {
                SignInManager.SignIn(user, isPersistent: false, rememberBrowser: false);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                return new HttpStatusCodeResult(200, "User created and signed in.");
            }
            AddErrors(result.Result);

            var errors = string.Empty;
            ((string[])result.Result.Errors).ToList().ForEach(x => errors += x);

            // If we got this far, something failed, redisplay form
            return new HttpStatusCodeResult(500, errors);
        }

        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult InitAuth()
        {
            // first we create "Administrators" role
            ApplicationDbContext context = new ApplicationDbContext();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            if (!roleManager.RoleExists("Administrators"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Administrators";
                roleManager.Create(role);
            }

            var user = new ApplicationUser { UserName = "david.vanhoecke@gmail.com", Email = "david.vanhoecke@gmail.com" };
            var result = UserManager.CreateAsync(user, "pppppp");
            if (result.Result.Succeeded)
            {
                UserManager.AddToRole(user.Id.ToString(), "Administrators");

                SignInManager.SignIn(user, isPersistent: false, rememberBrowser: false);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                return new HttpStatusCodeResult(200, "User created and signed in.");
            }
            AddErrors(result.Result);

            var errors = string.Empty;
            ((string[])result.Result.Errors).ToList().ForEach(x => errors += x);

            // If we got this far, something failed, redisplay form
            return new HttpStatusCodeResult(500, errors);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// Starts the password reset process by sending a mail with instructions.
        /// </summary>
        /// <param name="emailAddress">The email address.</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ForgotPasswordJson(string emailAddress)
        {
            try
            {
                //var user = UserManager.FindByNameAsync(emailAddress);
                var user = UserManager.FindByEmail(emailAddress);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return new HttpStatusCodeResult(200);
                }

                //var existingHelperForEmailIds = GTSDataRepository.GetListByQuery<PasswordHelper>("FROM PasswordHelper ph WHERE ph.EmailAddress = :email", new Dictionary<string, object> { { "email", emailAddress } })

                var passwordHelper = new PasswordHelper { Id = Guid.NewGuid(), EmailAddress = emailAddress, RequestDate = DateTime.Now, Token = Guid.NewGuid() };

                GTSDataRepository.Create(passwordHelper);

                var callBackUrl = HttpContext.Request.Url.Authority;

                var resetUrl = string.Format("http://{0}/Account/ResetPasswordFromEmail/{1}", callBackUrl, passwordHelper.Token);

                var body = string.Format(
                    "Hello," +
                    "<br />" +
                    "<br />" +
                    "This mail was sent to enable you to reset your password and will expire after 24 hours. Click <a href='{0}'>here</a> to reset your password.<br />" +
                    "If you did not request this mail you can simply ignore it." +
                    "<br />" +
                    "<br />" +
                    "Kind regards,<br />" +
                    "GTS", resetUrl);

                var subject = "GTS Password Change Request";

                var from = "david.vanhoecke@delair-tech.com";

                var to = emailAddress;

                MailHandler.Send(to, from, subject, body, _logger, true, true);

                return new HttpStatusCodeResult(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Resets a password and triggered from an email.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ResetPasswordFromEmail(Guid id)
        {
            _logger.LogInfo("A password reset request was made for token: " + id);

            ViewBag.AllowChange = false;

            try
            {
                var existingToken = GTSDataRepository.GetListByQuery<PasswordHelper>("FROM PasswordHelper ph WHERE ph.IsArchived = false AND ph.Token = :token", new Dictionary<string, object> { { "token", id } }).FirstOrDefault();

                if (existingToken != null && DateTime.Now.Subtract(existingToken.RequestDate) < new TimeSpan(24, 0, 0))
                    ViewBag.AllowChange = true;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Resets the password after a request from a logged in user
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ResetPasswordFromLoggedIn()
        {
            _logger.LogInfo("A password reset request was made for user: " + User.Identity.Name);

            ViewBag.AllowChange = true;

            try
            {
                return View("ResetPasswordFromEmail");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Resets the password from email.
        /// </summary>
        /// <param name="newPassword">The new password.</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPasswordJson(string newPassword, Guid token)
        {
            try
            {
                var helper = GTSDataRepository.GetListByQuery<PasswordHelper>("FROM PasswordHelper ph WHERE ph.IsArchived = false AND ph.Token = :token", new Dictionary<string, object> { { "token", token } }).FirstOrDefault();

                if (helper == null && token != Guid.Empty)
                    throw new InvalidOperationException("Could not find password reset token");

                var users = UserManager.Users;

                var emailAddress = string.Empty;
                if (helper == null)
                    emailAddress = User.Identity.Name;
                else
                    emailAddress = helper.EmailAddress;

                var usr = users.Where(x => x.Email == emailAddress).FirstOrDefault();

                var validPass = await UserManager.PasswordValidator.ValidateAsync(newPassword);
                if (validPass.Succeeded)
                {
                    var user = UserManager.FindByName(usr.Email);
                    user.PasswordHash = UserManager.PasswordHasher.HashPassword(newPassword);
                    var res = UserManager.Update(user);
                    if (res.Succeeded)
                    {
                        GTSDataRepository.ExecuteUpdateQuery("Update PasswordHelper SET IsArchived = true, ArchivedBy = :name, ArchivalDate = :dateNow where EmailAddress = :email", new Dictionary<string, object> {
                            { "name", usr.Email }, { "dateNow", DateTime.Now }, {"email", usr.Email } });

                        AuthenticationManager.SignOut();

                        return new HttpStatusCodeResult(200, "Password change successful.");
                    }
                    else
                    {
                        var errorMsg = "One or more errors occured during the password change attempt: ";
                        res.Errors.ForEach(x => errorMsg += (x + ". "));
                        throw new InvalidOperationException(errorMsg);
                    }
                }
                else
                    throw new InvalidOperationException("The password you've provided is not considered strong enough, please choose another.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);

                case SignInStatus.LockedOut:
                    return View("Lockout");

                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });

                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        #endregion Helpers
    }

    internal class JsonUser
    {
        public string username { get; set; }
        public string password { get; set; }
    }
}
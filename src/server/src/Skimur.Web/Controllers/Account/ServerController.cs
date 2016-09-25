using System;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Skimur.Web.Models;
using Microsoft.Extensions.DependencyInjection;
using Skimur.App;
using Skimur.Web.Models.Api;
using Microsoft.AspNetCore.Http;

namespace Skimur.Web.Controllers.Account
{
    public class ServerController : BaseController
    {
        UserManager<User> _userManager;
        SignInManager<User> _signInManager;
        string _externalCookieScheme;

        public ServerController(UserManager<User> userManager,
            SignInManager<User> signInManager,
            IOptions<IdentityCookieOptions> identityCookieOptions)
            : base(userManager,
                 signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _externalCookieScheme = identityCookieOptions.Value.ExternalCookieAuthenticationScheme;
        }

        [Route("register")]
        public async Task<IActionResult> Register()
        {
            return View("js-{auto}", await BuildState());
        }

        [Route("login")]
        public async Task<IActionResult> Login()
        {
            return View("js-{auto}", await BuildState());
        }

        [Route("forgotpassword")]
        public async Task<IActionResult> ForgotPassword()
        {
            return View("js-{auto}", await BuildState());
        }

        [Route("resetpassword", Name="resetpassword")]
        public async Task<IActionResult> ResetPassword()
        {
            return View("js-{auto}", await BuildState());
        }

        [Route("confirmemail", Name="confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code, string newEmail /*for email changed*/)
        {
            ViewBag.change = !string.IsNullOrEmpty(newEmail);

            var state = await BuildState();

            if (userId == null || code == null)
            {
                ViewBag.success = false;
                return View("js-{auto}", state);
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                ViewBag.success = false;
                return View("js-{auto}", state);
            }

            IdentityResult result;
            if (!string.IsNullOrEmpty(newEmail))
            {
                result = await _userManager.ChangeEmailAsync(user, newEmail, code);
            }
            else
            {
                result = await _userManager.ConfirmEmailAsync(user, code);
            }
            
            ViewBag.success = result.Succeeded;

            return View("js-{auto}", state);
        }
        
        [Route("externallogincallback")]
        public async Task<IActionResult> ExternalLoginCallback(bool autoLogin = true)
        {
            var callbackTemplate = new Func<object, string>(x =>
            {
                var serializerSettings = HttpContext
                   .RequestServices
                   .GetRequiredService<IOptions<MvcJsonOptions>>()
                   .Value
                   .SerializerSettings;
                var serialized = JsonConvert.SerializeObject(x, serializerSettings);
                return
                $@"<html lang=""en-us"">
                    <head>
                        <script type=""text/javascript"">
                            opener.postMessage({serialized}, location.origin);
                        </script>
                    </head>
                    <body></body>
                </html>";
            });

            dynamic data = new ExpandoObject();
            data.externalAuthenticated = false;
            data.loginProvider = null;
            data.user = null;
            data.requiresTwoFactor = false;
            data.lockedOut = false;
            data.proposedEmail = "";
            data.proposedUserName = "";
            data.success = false;

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            // unable to authenticate with an external login
            {
                ModelState.AddModelError(string.Empty, "Unable to authenticate against the server.");
                data.errors = GetModelState();
                return Content(callbackTemplate(data), "text/html");
            }

            if (string.IsNullOrEmpty(info.ProviderDisplayName))
            {
                info.ProviderDisplayName =
                    _signInManager.GetExternalAuthenticationSchemes()
                        .SingleOrDefault(x => x.AuthenticationScheme.Equals(info.LoginProvider))?
                        .DisplayName;
                if (string.IsNullOrEmpty(info.ProviderDisplayName))
                {
                    info.ProviderDisplayName = info.LoginProvider;
                }
            }

            data.loginProvider = new
            {
                scheme = info.LoginProvider,
                displayName = info.ProviderDisplayName
            };

            data.externalAuthenticated = true;

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var userName = info.Principal.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrEmpty(userName))
                userName = userName.Replace(" ", "_");

            data.proposedEmail = email;
            data.proposedUserName = userName;

            // sign in the user with this external login provider if the user already has a login.
            if (autoLogin)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

                if(user == null)
                {
                    ModelState.AddModelError(string.Empty, "No user registered with this login");
                    data.errors = GetModelState();
                    return Content(callbackTemplate(data), "text/html");
                }
                
                var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);

                if (result.Succeeded)
                {
                    data.user = ApiUser.From(user);
                    data.errors = GetModelState();
                    data.success = true;
                } else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");

                    if (result.RequiresTwoFactor)
                    {
                        data.requiresTwoFactor = true;
                        data.userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
                    }

                    if (result.IsLockedOut)
                        data.lockedOut = true;

                    data.errors = GetModelState();
                }
            } else
            {
                // all we wanted was to externally authenticate, not log in, and we succeeded!
                data.errors = GetModelState();
                data.success = true;
            }

            return Content(callbackTemplate(data), "text/html");
        }

        [Route("externalloginredirect")]
        public async Task<IActionResult> ExternalLoginRedirect(string provider, bool autoLogin = true, bool didRefresh = false)
        {
            // when we first visit this redirect page, we need delete any previous authentication tokens.
            // See https://github.com/aspnet/Security/issues/299
            if (!didRefresh)
            {
                await HttpContext.Authentication.SignOutAsync(_externalCookieScheme);
                var queryString = new QueryString(Request.QueryString.ToString());
                return Redirect(Request.Path + queryString.Add("didRefresh", "true"));
            }

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, "/externallogincallback?autoLogin=" + autoLogin);
            return new ChallengeResult(provider, properties);
        }
    }
}

using Identity.IdentityServer.Models.Auth;
using Identity.IdentityServer.Models.Entity;
using Identity.IdentityServer.ViewModels;
using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Identity.IdentityServer.Controllers
{
    [Route("Auth")]
    public class AuthController : Controller
    {
        #region Private Fields
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _env;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IClientStore _clientStore;
        private readonly IEventService _events;
        private readonly IIdentityServerInteractionService _interactionService; 
        #endregion

        #region CTOR
        public AuthController(UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment env,

            IAuthenticationSchemeProvider schemeProvider,
            IClientStore clientStore,
            IEventService events,

            IIdentityServerInteractionService interactionService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
            _schemeProvider = schemeProvider;
            _clientStore = clientStore;
            _events = events;
            _interactionService = interactionService;
        } 
        #endregion


        [Route("Login/{returnUrl?}")]
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {

            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "~/";
            }

            if (User.Identity.IsAuthenticated)
            {
                //return RedirectToAction("Index", "Home");
                return Redirect(returnUrl);
            }

            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl });
            }

            if (_env.IsDevelopment())
            {
                vm.Password = "123qwe";
                vm.Username = "Muxa";
            }

            return View(vm);
        }

        [Route("[action]")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "User Not Found");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
                return Redirect(model.ReturnUrl);

            ModelState.AddModelError("", "Неверный логил или пароль");
            return View(model);
        }

        [Route("[action]")]
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            await _signInManager.SignOutAsync();
            var logoutRequest = await _interactionService.GetLogoutContextAsync(logoutId);

            if(!string.IsNullOrEmpty(logoutRequest.PostLogoutRedirectUri))
            {
                return Redirect(logoutRequest.PostLogoutRedirectUri);
            }

            return RedirectToAction("Index","Home");
        }

        [Route("[action]")]
        [HttpGet]
        public IActionResult RegisterUser(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "~/";
            }

            RegisterViewModel model = new()
            {
                Birthdate = System.DateTime.Now,
                ReturnUrl = returnUrl,
            };
            return View(model);
        }

        [Route("[action]")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RegisterUser(RegisterViewModel model)
        {
            if (model is null)
            {
                ModelState.AddModelError("","Model Errorr");
                return View();
            }

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user != null)
            {
                ModelState.AddModelError("", $"User Name {model.UserName} is taken");
            }

            if (!model.Pass.Equals(model.RetrPass))
            {
                ModelState.AddModelError("", "The password and confirmation password do not match.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user = new()
            {
                UserName = model.UserName,
                Gender = model.Gender,
                Birthdate = model.Birthdate,
            };

            var result = await _userManager.CreateAsync(user, model.Pass);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", string.Join("\n",result.Errors.Select(d => d.Description)));
                return View(model);
            }
            user = await _userManager.FindByNameAsync(model.UserName);
            user.LockoutEnabled = false;
            _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "User")).GetAwaiter().GetResult();

            return View();
        }

        [Route("[action]/{returnUrl?}")]
        [HttpGet]
        public IActionResult AccessDenied(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "~/";
            }

            return View(model: returnUrl);
        }

        #region Private
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interactionService.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Username = context?.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }
        #endregion
    }
}

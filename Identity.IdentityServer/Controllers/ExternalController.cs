using Identity.IdentityServer.Infrastructure;
using Identity.IdentityServer.Models.Entity;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace HRInPocket.IdentityServer.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    public class ExternalController : Controller
    {
        private readonly UserManager<User> _UserManager;
        private readonly SignInManager<User> _SignInManager;
        private readonly IIdentityServerInteractionService _Interaction;
        private readonly IClientStore _ClientStore;
        private readonly IEventService _Events;

        public ExternalController(
            UserManager<User> UserManager,
            SignInManager<User> SignInManager,
            IIdentityServerInteractionService Interaction,
            IClientStore ClientStore,
            IEventService Events)
        {
            _UserManager = UserManager;
            _SignInManager = SignInManager;
            _Interaction = Interaction;
            _ClientStore = ClientStore;
            _Events = Events;
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public IActionResult Challenge(string scheme, string ReturnUrl)
        {
            if (string.IsNullOrEmpty(ReturnUrl)) ReturnUrl = "~/";

            // validate returnUrl - either it is a valid OIDC URL or back to a local page
            if (!Url.IsLocalUrl(ReturnUrl) && !_Interaction.IsValidReturnUrl(ReturnUrl))
            {
                // user might have clicked on a malicious link - should be logged
                throw new Exception("invalid return URL");
            }

            var callback = Url.Action("LoginCallback");
            // start challenge and roundtrip the return URL and scheme 
            var props = new AuthenticationProperties
            {
                RedirectUri = callback,
                Items =
                    {
                        { "returnUrl", ReturnUrl },
                        { "scheme", scheme },
                    }
            };

            return Challenge(props, scheme);

        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> LoginCallback()
        {
            // read external identity from the temporary cookie
            var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error");
            }

            // lookup our user and external provider info
            var (user, provider, provider_user_id, claims) = await FindUserFromExternalProviderAsync(result);
            if (user == null)
            {
                // this might be where you might initiate a custom workflow for user registration
                // in this sample we don't show how that would be done, as our sample implementation
                // simply auto-provisions new external user
                user = await AutoProvisionUserAsync(provider, provider_user_id, claims);
            }

            // this allows us to collect any additonal claims or properties
            // for the specific prtotocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            var additional_local_claims = new List<Claim>();
            var local_sign_in_props = new AuthenticationProperties();
            ProcessLoginCallbackForOidc(result, additional_local_claims, local_sign_in_props);
            ProcessLoginCallbackForWsFed(result, additional_local_claims, local_sign_in_props);
            ProcessLoginCallbackForSaml2p(result, additional_local_claims, local_sign_in_props);

            // issue authentication cookie for user
            // we must issue the cookie maually, and can't use the SignInManager because
            // it doesn't expose an API to issue additional claims from the login workflow
            var principal = await _SignInManager.CreateUserPrincipalAsync(user);
            additional_local_claims.AddRange(principal.Claims);
            var name = principal.FindFirst(JwtClaimTypes.Name)?.Value ?? user.Id.ToString();

            var isuser = new IdentityServerUser(user.Id.ToString())
            {
                DisplayName = name,
                IdentityProvider = provider,
                AdditionalClaims = additional_local_claims
            };

            await HttpContext.SignInAsync(isuser, local_sign_in_props);

            // delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // validate return URL and redirect back to authorization endpoint or a local page
            var return_url = result.Properties.Items["returnUrl"];
            if (_Interaction.IsValidReturnUrl(return_url) || Url.IsLocalUrl(return_url))
            {
                return Redirect(return_url);
            }

            return Redirect("~/");
        }



        private async Task<(User user, string provider, string providerUserId, IEnumerable<Claim> claims)>
            FindUserFromExternalProviderAsync(AuthenticateResult result)
        {
            var external_user = result.Principal;

            // try to determine the unique id of the external user (issued by the provider)
            // the most common claim type for that are the sub claim and the NameIdentifier
            // depending on the external provider, some other claim type might be used
            var user_id_claim = external_user.FindFirst(JwtClaimTypes.Subject) ??
                              external_user.FindFirst(ClaimTypes.NameIdentifier) ??
                              throw new Exception("Unknown userid");

            // remove the user id claim so we don't include it as an extra claim if/when we provision the user
            var claims = external_user.Claims.ToList();
            claims.Remove(user_id_claim);

            var provider = result.Properties.Items["scheme"];
            var provider_user_id = user_id_claim.Value;

            // find external user
            var user = await _UserManager.FindByLoginAsync(provider, provider_user_id);

            return (user, provider, provider_user_id, claims);
        }

        private async Task<User> AutoProvisionUserAsync(string provider, string ProviderUserId, IEnumerable<Claim> claims)
        {
            // create a list of claims that we want to transfer into our store
            var filtered = new List<Claim>();

            // user's display name
            var name = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name)?.Value ??
                claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
            if (name != null)
            {
                filtered.Add(new Claim(JwtClaimTypes.Name, name));
            }
            else
            {
                var first = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value ??
                    claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName)?.Value;
                var last = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value ??
                    claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname)?.Value;
                if (first != null && last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
                }
                else if (first != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first));
                }
                else if (last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, last));
                }
            }

            // email
            var email = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email)?.Value ??
               claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            if (email != null)
            {
                filtered.Add(new Claim(JwtClaimTypes.Email, email));
            }

            var birthDate = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.BirthDate)?.Value ??
                claims.FirstOrDefault(x => x.Type == ClaimTypes.DateOfBirth)?.Value ?? DateTime.Now.ToString("D");

            var giveName = claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName)?.Value;
            var familyName = claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname)?.Value;

            var user = new User
            {
                UserName = Guid.NewGuid().ToString(),
                GivenName = giveName,
                FamilyName = familyName,
                Email = email,
                Birthdate = DateTime.Parse(birthDate),
            };
            var identity_result = await _UserManager.CreateAsync(user);

            if (!identity_result.Succeeded) throw new Exception(identity_result.Errors.First().Description);

            user.LockoutEnabled = false;
            _UserManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "User")).GetAwaiter().GetResult();
            if (filtered.Count > 0)
            {
                identity_result = await _UserManager.AddClaimsAsync(user, filtered);
                if (!identity_result.Succeeded) throw new Exception(identity_result.Errors.First().Description);
            }

            identity_result = await _UserManager.AddLoginAsync(user, new UserLoginInfo(provider, ProviderUserId, provider));
            if (!identity_result.Succeeded) throw new Exception(identity_result.Errors.First().Description);

            return user;
        }


        private static void ProcessLoginCallbackForOidc(AuthenticateResult ExternalResult,
            ICollection<Claim> LocalClaims, AuthenticationProperties LocalSignInProps)
        {
            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = ExternalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                LocalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            var id_token = ExternalResult.Properties.GetTokenValue("id_token");
            if (id_token != null)
            {
                LocalSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = id_token } });
            }
        }

        private static void ProcessLoginCallbackForWsFed(AuthenticateResult ExternalResult,
            ICollection<Claim> LocalClaims, AuthenticationProperties LocalSignInProps)
        {
        }

        private static void ProcessLoginCallbackForSaml2p(AuthenticateResult ExternalResult,
            ICollection<Claim> LocalClaims, AuthenticationProperties LocalSignInProps)
        {
        }
    }
}

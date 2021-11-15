using Identity.IdentityServer.Infrastructure;
using Identity.IdentityServer.Models.Entity;
using Identity.IdentityServer.ViewModels;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Identity.IdentityServer.Controllers
{
    [Route("[controller]/[action]")]
    public class ExternalOldController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;

        public ExternalOldController(UserManager<User> userManager,
            SignInManager<User> signInManager,
            IIdentityServerInteractionService interaction,
            IEventService events)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _interaction = interaction;
            _events = events;
        }

        [HttpGet]
        public IActionResult Challenge(string scheme, string returnUrl)
        {
            if (!_interaction.IsValidReturnUrl(returnUrl) && !Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var callback = Url.Action("LoginCallback");

            var prop = new AuthenticationProperties
            {
                RedirectUri = callback,
                Items =
                {
                    {"scheme", scheme},
                    {"returnUrl", returnUrl},
                }
            };

            return Challenge(prop, scheme);
        }

        [HttpGet]
        public async Task<IActionResult> LoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync(GlobalConstant.ExternalSignSheme);

            var tokens = result.Properties.GetTokens();

            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error");
            }

            #region tmp
            //var externalUser = result.Principal;
            //if (externalUser == null)
            //{
            //    throw new Exception("External authentication error");
            //}

            //var claims = externalUser.Claims.ToList();

            //var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject);
            //if (userIdClaim == null)
            //{
            //    userIdClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            //}
            //if (userIdClaim == null)
            //{
            //    throw new Exception("Unknown userid");
            //}

            //var externalUserId = userIdClaim.Value;
            //var externalProvider = userIdClaim.Issuer;

            //var userName = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

            //User user = null;

            //user = await _userManager.FindByLoginAsync(externalProvider, externalProvider); 
            #endregion

            var (user, externalProvider, providerUserId, claims) = await FindUserFromExternalProviderAsync(result);

            if (user == null)
            {
                user = await AutoProvisionUserAsync(externalProvider, providerUserId, claims);
            }

            //if(user == null)
            //{
            //    user = new User()
            //    {
            //        UserName = userName,
            //        GivenName = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
            //        FamilyName = claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname)?.Value,
            //    };
            //    await _userManager.CreateAsync(user);
            //    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "User"));
            //    var claimResult = await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Name, user.UserName));

            //    if (claimResult.Succeeded)
            //    {
            //        var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(externalProvider, externalProvider, externalProvider));
            //    }
                
            //}
            
            var additionalLocalClaims = new List<Claim>();
            var localSignInProps = new AuthenticationProperties();
            ProcessLoginCallback(result, additionalLocalClaims, localSignInProps);

            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            additionalLocalClaims.AddRange(principal.Claims);
            

            var name = principal.FindFirst(JwtClaimTypes.Name)?.Value ?? user.Id.ToString();
            var issuser = new IdentityServerUser(user.Id)
            {
                DisplayName = name,
                IdentityProvider = externalProvider,
                AdditionalClaims = additionalLocalClaims,
            };

           
            await HttpContext.SignInAsync(issuser, localSignInProps);
            await HttpContext.SignOutAsync(GlobalConstant.ExternalSignSheme);

            

            var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";
            if (_interaction.IsValidReturnUrl(returnUrl) || Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect(returnUrl);
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
            var user = await _userManager.FindByLoginAsync(provider, provider_user_id);

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

            var user = new User
            {
                UserName = Guid.NewGuid().ToString(),
            };
            var identity_result = await _userManager.CreateAsync(user);
            if (!identity_result.Succeeded) throw new Exception(identity_result.Errors.First().Description);

            if (filtered.Count > 0)
            {
                identity_result = await _userManager.AddClaimsAsync(user, filtered);
                if (!identity_result.Succeeded) throw new Exception(identity_result.Errors.First().Description);
            }

            identity_result = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, ProviderUserId, provider));
            if (!identity_result.Succeeded) throw new Exception(identity_result.Errors.First().Description);

            return user;
        }

        private void ProcessLoginCallback(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            var idToken = externalResult.Properties.GetTokenValue("id_token");
            if (idToken != null)
            {
                localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
            }

           
        }
    }
}

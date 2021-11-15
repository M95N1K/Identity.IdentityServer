using Identity.IdentityServer.Models.Entity;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Identity.IdentityServer.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<User> _userManager;

        public ProfileService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
                          
            var user = _userManager.GetUserAsync(context.Subject).GetAwaiter().GetResult();

            var claims = new List<Claim>
            {  
            };
            var scop = context.Client.AllowedScopes;

            var role = context.Subject.Claims.FirstOrDefault(r => r.Type == ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(role))
                role = "User";

            if (!string.IsNullOrEmpty(scop.FirstOrDefault(s => s == "profile")))
            {
                claims.AddRange(ProfileClaims(user));
            }

            if (!string.IsNullOrEmpty(scop.FirstOrDefault(s => s == "OrdersApi")))
            {
                claims.Add(new Claim(ClaimTypes.Role,role));
            }

            if (!string.IsNullOrEmpty(scop.FirstOrDefault(s => s == "role")))
            {
                claims.Add(new Claim(ClaimTypes.Role,role));
            }


            context.IssuedClaims.AddRange(claims);

            return Task.CompletedTask;
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            var user = _userManager.GetUserAsync(context.Subject).GetAwaiter().GetResult();

            context.IsActive = (user != null) && !user.LockoutEnabled; 

            return Task.CompletedTask;
        }

        private List<Claim> ProfileClaims(User user)
        {
            List<Claim> result = new();

            result.Add(new Claim(ClaimTypes.DateOfBirth, user.Birthdate.ToString("dd/MM/yyyy")));
            result.Add(new Claim(JwtClaimTypes.Name, user.UserName));

            if (!string.IsNullOrEmpty(user.Nickname))
            {
                result.Add(new Claim(JwtClaimTypes.NickName, user.Nickname));
            }
            if (!string.IsNullOrEmpty(user.GivenName))
            {
                result.Add(new Claim(JwtClaimTypes.GivenName, user.GivenName));
            }
            if (!string.IsNullOrEmpty(user.FamilyName))
            {
                result.Add(new Claim(JwtClaimTypes.FamilyName, user.FamilyName));
            }
            if (!string.IsNullOrEmpty(user.Gender))
            {
                result.Add(new Claim(JwtClaimTypes.Gender, user.Gender));
            }


            return result;
        }
    }
}

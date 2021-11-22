using Identity.IdentityServer.ViewModel;
using Identity.IdentityServer.Models.Entity;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using System;
using System.Security.Claims;
using System.Linq;

namespace Identity.IdentityServer.Infrastructure
{
    public static class Extension
    {
        public static UserInfoViewModel ToUserInfo(this User user)
        {
            var json = JsonSerializer.Serialize(user);

            var result = JsonSerializer.Deserialize<UserInfoViewModel>(json);

            return result;
        }

        public static UserInfoViewModel ApplyData(this UserInfoViewModel viewModel, UserManager<User> userManager)
        {
            var user = userManager.FindByIdAsync(viewModel.Id).GetAwaiter().GetResult();
            if(user == null)
            {
                throw new ArgumentException($"Пользователь с ID : {viewModel.Id} не найден!!!");
            }

            user.UserName = viewModel.UserName;
            user.LockoutEnabled = viewModel.LockoutEnabled;
            user.LockoutEnd = viewModel.LockoutEnd;
            user.TwoFactorEnabled = viewModel.TwoFactorEnabled;
            user.PhoneNumberConfirmed = viewModel.PhoneNumberConfirmed;
            user.PhoneNumber = viewModel.PhoneNumber;
            user.EmailConfirmed = viewModel.EmailConfirmed;
            user.Email = viewModel.Email;
            user.FamilyName = viewModel.FamilyName;
            user.GivenName = viewModel.GivenName;
            user.Nickname = viewModel.Nickname;
            user.Gender = viewModel.Gender;
            user.Picture = viewModel.Picture;
            user.Birthdate = viewModel.Birthdate;

            userManager.UpdateAsync(user).GetAwaiter().GetResult();

            var role = userManager.GetClaimsAsync(user)
                .GetAwaiter().GetResult().FirstOrDefault(x => x.Type == ClaimTypes.Role).Value;

            if (role != viewModel.Role)
            {
                userManager.RemoveClaimAsync(user, new Claim(ClaimTypes.Role, role)).GetAwaiter().GetResult();
                userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, viewModel.Role)).GetAwaiter().GetResult();
            }

            return viewModel;
        }

        
    }
}

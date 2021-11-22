using Identity.IdentityServer.Infrastructure;
using Identity.IdentityServer.Models.Entity;
using Identity.IdentityServer.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Identity.IdentityServer.Controllers
{
    [Authorize("Administrator")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            List<UserInfoViewModel> usersInfo = new List<UserInfoViewModel>();
            var allUser = _userManager.Users.ToList();
            foreach (var item in allUser)
            {
                var uInfo = item.ToUserInfo();
                uInfo.Role = (await _userManager.GetClaimsAsync(item)).FirstOrDefault(x => x.Type == ClaimTypes.Role).Value;
                usersInfo.Add(uInfo);
            }
            return View(usersInfo);
        }

        [Route("user/{id}")]
        [HttpGet]
        public async Task<IActionResult> UserInfo(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new System.ArgumentException($"\"{nameof(id)}\" не может быть неопределенным или пустым.", nameof(id));
            }

            var user = await _userManager.FindByIdAsync(id);

            if(user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(null);
            }

            var info = user.ToUserInfo();
            info.Role = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(x => x.Type == ClaimTypes.Role).Value;

            return View(info);
        }

        [Route("[action]/{id}")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new System.ArgumentException($"\"{nameof(id)}\" не может быть неопределенным или пустым.", nameof(id));
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(new UserInfoViewModel());
            }

            var info = user.ToUserInfo();
            info.Role = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(x => x.Type == ClaimTypes.Role).Value;

            return View(info);
        }

        [Route("[action]/{id}")]
        [HttpPost]
        public async Task<IActionResult> Edit(string id, UserInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
            {
                ModelState.AddModelError("", "Ошибка Id пользователя");
                return View(new UserInfoViewModel());
            }

            model.ApplyData(_userManager);
            //await _userManager.UpdateAsync(user);

            return View("UserInfo",model);
        }
    }
}

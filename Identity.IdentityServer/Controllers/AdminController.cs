using Identity.IdentityServer.Infrastructure;
using Identity.IdentityServer.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
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
            List<UserInfo> usersInfo = new List<UserInfo>();
            var allUser = _userManager.Users.ToList();
            foreach (var item in allUser)
            {
                var uInfo = item.ToUserInfo();
                uInfo.Role = (await _userManager.GetClaimsAsync(item)).FirstOrDefault(x => x.Type == ClaimTypes.Role).Value;
                usersInfo.Add(uInfo);
            }
            return View(usersInfo);
        }
    }
}

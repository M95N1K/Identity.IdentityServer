using Identity.IdentityServer.ViewModel;
using System.Collections.Generic;

namespace Identity.IdentityServer.Infrastructure
{
    public static class CreateMenu
    {
        public static IEnumerable<MenuViewModel> Menu =>
            new List<MenuViewModel>
            {
                new MenuViewModel
                {
                    Controller = "Home",
                    Action = "Index",
                    DisplayName = "[Home]"
                },
                new MenuViewModel
                {
                    Controller = "Home",
                    Action = "Privacy",
                    DisplayName = "[Privacy]"
                },
                new MenuViewModel
                {
                    Controller = "Auth",
                    Action = "Login",
                    DisplayName = "[Login]"
                },
                new MenuViewModel
                {
                    Controller = "Auth",
                    Action = "RegisterUser",
                    DisplayName = "[Registration]"
                },
            };


    }
}

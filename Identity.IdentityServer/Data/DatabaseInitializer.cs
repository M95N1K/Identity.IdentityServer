using Identity.IdentityServer.Models.Entity;
using IdentityModel;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;

namespace Identity.IdentityServer.Data
{
    public static class DatabaseInitializer
    {
        public static void EntityDbInit(IServiceProvider serviceProvider, bool useDB = false)
        {
            var db = serviceProvider.GetService<EntityDbContext>().Database;

            if (useDB && db.GetPendingMigrations().Any())
            {
                db.Migrate();
            }

            var userManager = serviceProvider.GetService<UserManager<User>>();

            var count = userManager.Users.ToList().Count;
            if (count > 0)
                return;

            var user = new User
            {
                UserName = "Muxa",
                Email = "user@mail.net",
            };

            var result = userManager.CreateAsync(user, "123qwe").GetAwaiter().GetResult();

            user = userManager.FindByNameAsync("Muxa").GetAwaiter().GetResult();
            user.LockoutEnabled = false;

            if (result.Succeeded)
            {
                userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Administrator")).GetAwaiter().GetResult();
            }
            
        }

        public static void IdentityServerDbInit(IServiceProvider serviceProvider, bool useDB = false)
        {
            var persistedGrantDb = serviceProvider.GetRequiredService<PersistedGrantDbContext>().Database;
            if (useDB && persistedGrantDb.GetPendingMigrations().Any())
            {
                persistedGrantDb.Migrate();
            }

            var context = serviceProvider.GetRequiredService<ConfigurationDbContext>();
            if (useDB && context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate(); 
            }

            if (!context.Clients.Any())
            {
                foreach (var client in Config.GetClients())
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in Config.GetIdentityREsources())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiScopes.Any())
            {
                foreach (var resource in Config.GetApiScopes())
                {
                    context.ApiScopes.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }
        }
    }
}

using Identity.IdentityServer.Data;
using Identity.IdentityServer.Infrastructure;
using Identity.IdentityServer.Infrastructure.Services;
using Identity.IdentityServer.Models.Entity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Identity.IdentityServer
{
    public class Startup
    {
        private bool _useDB;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _useDB = Convert.ToBoolean(Configuration.GetSection("AppSetings:UseDB").Value); ;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            #region EntityFramework and MSIdentity
            services.AddDbContext<EntityDbContext>(config =>
               {
                   if (_useDB)
                       config.UseSqlServer(Configuration.GetConnectionString(nameof(EntityDbContext)));
                   else
                       config.UseInMemoryDatabase("MAMORY");
               })
               .AddIdentity<User, IdentityRole>(config =>
                   {
                       config.Password.RequireDigit = false;
                       config.Password.RequireLowercase = false;
                       config.Password.RequireNonAlphanumeric = false;
                       config.Password.RequireUppercase = false;
                       config.Password.RequiredLength = 6;

                       config.Lockout.AllowedForNewUsers = true;

                   })

               .AddEntityFrameworkStores<EntityDbContext>();
            #endregion

            #region IdentityServer4
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            services.AddIdentityServer(config =>
            {
                config.UserInteraction.LoginUrl = "/auth/login";
                config.UserInteraction.LogoutUrl = "/auth/logout";
            })
                .AddAspNetIdentity<User>()
                .AddConfigurationStore(options =>
                {
                    if (_useDB)
                    {
                        options.ConfigureDbContext = b =>
                        b.UseSqlServer(Configuration.GetConnectionString(nameof(EntityDbContext)),
                        sql => sql.MigrationsAssembly(migrationsAssembly));
                    }
                    else
                    {
                        options.ConfigureDbContext = b =>
                            b.UseInMemoryDatabase("MAMORY");
                    }
                })
                .AddOperationalStore(options =>
                {
                    if (_useDB)
                    {
                        options.ConfigureDbContext = b =>
                                    b.UseSqlServer(Configuration.GetConnectionString(nameof(EntityDbContext)),
                                        sql => sql.MigrationsAssembly(migrationsAssembly));
                    }
                    else
                    {
                        options.ConfigureDbContext = b =>
                            b.UseInMemoryDatabase("MAMORY");
                    }
                })
                .AddProfileService<ProfileService>()

                .AddDeveloperSigningCredential();
            #endregion

            #region External Authentication
            services.AddAuthentication()
                    .AddGoogle(conf =>
                    {
                        conf.SignInScheme = GlobalConstant.ExternalSignSheme;

                        conf.ClientId = Configuration["Autorize:Google:ClientID"];
                        conf.ClientSecret = Configuration["Autorize:Google:ClientSecret"];

                    }); 
            #endregion

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseIdentityServer();

            app.UseRouting();

            //app.UseCookiePolicy();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Privacy}/{id?}");
            });
        }

    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Identity.IdentityServer.Data;
using Microsoft.Extensions.Configuration;
using System;

namespace Identity.IdentityServer
{
    
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

            var useDB = Convert.ToBoolean(config.GetSection("AppSetings:UseDB").Value);

            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                DatabaseInitializer.EntityDbInit(scope.ServiceProvider, useDB);
                DatabaseInitializer.IdentityServerDbInit(scope.ServiceProvider, useDB);
            }
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

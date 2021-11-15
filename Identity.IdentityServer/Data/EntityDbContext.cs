using Identity.IdentityServer.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.IdentityServer.Data
{
    public class EntityDbContext : IdentityDbContext<User,IdentityRole,string>
    {
        public EntityDbContext(DbContextOptions<EntityDbContext> options)
            :base(options)
        {
            
        }
    }
}

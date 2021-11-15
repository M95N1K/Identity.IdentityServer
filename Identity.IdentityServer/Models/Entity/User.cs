using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.IdentityServer.Models.Entity
{
    public class User: IdentityUser
    {
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public string Nickname { get; set; }
        public string Gender { get; set; }
        public string Picture { get; set; }
        public DateTime Birthdate { get; set; }

    }
}
